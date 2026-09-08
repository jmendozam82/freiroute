# ADR-020: Numeración Automática de Órdenes de Transporte

## Estado
Aceptado

## Fecha
2026 — Sprint 4

## Decisores
@PM · @Arquitecto

## Módulo afectado
EP-04 Order Management — HU-021, HU-024

---

## Contexto

Cada orden de transporte necesita un número legible, único dentro del tenant
y trazable en documentos físicos (carta de porte), correos electrónicos al
cliente y comunicaciones con el carrier. El operador no debe digitarlo; el
sistema lo genera automáticamente.

El onboarding wizard (ADR-010) ya recolecta el campo `prefijo_orden` en la
configuración inicial del tenant (ej: `"ORD"`, `"FRT"`, `"GT"`). La tabla
`empresas` almacena este valor. Si el tenant no lo configuró, el sistema
aplica un fallback seguro.

### Requisitos del número de orden

- **Único por tenant** — no necesita ser globalmente único
- **Legible** — formato imprimible en documentos físicos
- **Inmutable** — una vez asignado no cambia aunque la orden se edite
- **Generado al confirmar** — el DRAFT no tiene número; este solo se asigna
  en la transición `DRAFT → CONFIRMED` (ADR-019)
- **Sin gaps visibles en secuencia confirmada** — los DRAFTs abandonados no
  consumen numeración

---

## Decisión

### Formato del número de orden

```
{PREFIJO}-{YYYY}-{NNNNN}

Ejemplos:
  ORD-2026-00001
  FRT-2026-00142
  GT-2026-00009
```

| Componente | Valor | Origen |
|---|---|---|
| `PREFIJO` | 2–10 chars | `configuracion.prefijo_orden`; fallback `"ORD"` |
| `YYYY` | Año de confirmación (4 dígitos) | `EXTRACT(YEAR FROM NOW())` |
| `NNNNN` | Consecutivo de 5 dígitos, reinicia cada año por tenant | tabla `contadores_orden` |

### Cuándo se genera

Exclusivamente al ejecutar la transición `DRAFT → CONFIRMED` en
`OrderService.CambiarEstadoAsync`. El número se asigna en la **misma
transacción de BD** que actualiza el estado. Si la transacción falla
el consecutivo no queda comprometido.

### Mecanismo: contador atómico por tenant-año

Se utiliza una tabla de contadores con `INSERT … ON CONFLICT DO UPDATE`
(upsert atómico) en lugar de una sequence de PostgreSQL, para permitir:

1. Reset automático del consecutivo cada año nuevo por tenant
2. Múltiples tenants con sus propios rangos independientes
3. Sin gaps en la numeración visible (los DRAFTs no consumen el contador)

#### Tabla `contadores_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_contadores_orden.sql
CREATE TABLE contadores_orden (
    empresa_id         UUID NOT NULL REFERENCES empresas(id),
    anio               SMALLINT NOT NULL,
    ultimo_consecutivo INT NOT NULL DEFAULT 0,
    PRIMARY KEY (empresa_id, anio)
);

COMMENT ON TABLE contadores_orden IS
    'Contador de consecutivo de órdenes por tenant y año.
     El upsert atómico garantiza unicidad sin race conditions.';
COMMENT ON COLUMN contadores_orden.ultimo_consecutivo IS
    'Último número asignado. Reinicia a 1 cada nuevo año por tenant.';
```

#### Función PostgreSQL `generar_numero_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_funcion_generar_numero_orden.sql
CREATE OR REPLACE FUNCTION generar_numero_orden(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql AS $$
DECLARE
    v_consecutivo INT;
    v_numero      TEXT;
BEGIN
    -- Atomic upsert: insert first occurrence or increment counter
    INSERT INTO contadores_orden (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = contadores_orden.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    -- Format: PREFIJO-YYYY-NNNNN
    v_numero := UPPER(TRIM(p_prefijo))
             || '-' || p_anio::TEXT
             || '-' || LPAD(v_consecutivo::TEXT, 5, '0');

    RETURN v_numero;
END;
$$;

COMMENT ON FUNCTION generar_numero_orden IS
    'Genera el número de orden legible para el tenant en el año indicado.
     Garantiza unicidad por tenant-año mediante upsert atómico.
     Llamada exclusivamente en la transición DRAFT→CONFIRMED.';
```

### Llamada desde C# (`OrderRepository`)

```csharp
// Freiroute.DAL/Repositories/OrderRepository.cs
public async Task<string> GenerarNumeroOrdenAsync(
    Guid empresaId, string prefijo, int anio)
{
    const string sql = """
        SELECT generar_numero_orden(
            @EmpresaId::uuid,
            @Prefijo,
            @Anio::smallint)
        """;

    return await _db.ExecuteScalarAsync<string>(
        sql, new { EmpresaId = empresaId, Prefijo = prefijo, Anio = anio });
}
```

### Flujo completo en `OrderService.CambiarEstadoAsync`

```csharp
// Solo aplica cuando estadoNuevo == OrdenEstado.Confirmed
if (dto.EstadoNuevo == OrdenEstado.Confirmed)
{
    var config = await _configuracionRepository.GetByEmpresaAsync(empresaId);
    var prefijo = string.IsNullOrWhiteSpace(config?.PrefijoOrden)
        ? "ORD"
        : config.PrefijoOrden;

    var anio = DateTime.UtcNow.Year;
    numeroOrden      = await _ordenRepository.GenerarNumeroOrdenAsync(
                           empresaId, prefijo, anio);
    fechaConfirmacion = DateTime.UtcNow;
}
```

---

## Índice único de soporte

```sql
-- Garantía adicional a nivel BD (respaldo del contador)
CREATE UNIQUE INDEX idx_ordenes_numero_empresa
    ON ordenes(empresa_id, numero_orden)
    WHERE numero_orden IS NOT NULL;
```

Este índice rechaza cualquier duplicado que pudiera surgir por
condición de carrera extrema, garantizando consistencia a nivel de BD
independientemente de la capa de aplicación.

---

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| UUID como número de orden | No legible en documentos físicos ni comunicaciones con clientes |
| `SERIAL` / `SEQUENCE` global de PostgreSQL | No reinicia por año ni se puede separar por tenant sin múltiples sequences |
| Número generado en C# (timestamp + random) | Race condition posible en alta concurrencia; no garantiza formato legible |
| Generar el número al crear el DRAFT | Crea brechas numéricas visibles (ORD-2026-00001, ORD-2026-00003…) por DRAFTs abandonados |
| Usar `consecutivo_orden` en tabla `empresas` | Bloqueo de fila en `empresas` en cada confirmación; cuellos de botella en volumen alto |

---

## Consecuencias

**Positivas:**
- Generación atómica — sin race conditions, sin duplicados, sin bloqueos globales
- Reset anual automático — el consecutivo vuelve a 1 cada año por tenant
- Legible en documentos físicos, correos y llamadas telefónicas
- Prefijo configurable desde el onboarding — cada tenant tiene su identidad
- El índice único en BD es el seguro de última línea contra duplicados

**Negativas / trade-offs:**
- La tabla `contadores_orden` no tiene RLS propio — se protege por FK a `empresas`
  y por el filtro `empresa_id` en la función. No contiene datos sensibles.
- Si el tenant nunca configuró `prefijo_orden`, todos sus números usan `"ORD"`.
  Esto es correcto — los tenants están aislados por `empresa_id`.
- El número de 5 dígitos soporta hasta 99.999 órdenes confirmadas por año
  por tenant. Si un tenant supera ese volumen, se extiende a 6 dígitos en
  una futura migración sin romper el formato.

---

## Módulos afectados

| Módulo | Impacto |
|---|---|
| EP-04 HU-021 Creación de orden (Sprint 4) | El campo `numero_orden` queda NULL en DRAFT |
| EP-04 HU-024 Flujo de estados (Sprint 4) | Generación del número en DRAFT → CONFIRMED |
| EP-04 HU-026 Split de órdenes (Sprint 4) | Las sub-órdenes generan su propio número al confirmarse |
| EP-06 Shipment Planning (Sprint 7) | Referencia al `numero_orden` en documentos del shipment |
| EP-09 Document Management (Sprint 11) | `numero_orden` como referencia en documentos adjuntos |
| EP-10 Freight Audit (Sprint 12) | `numero_orden` en facturas y conciliaciones |

---

*ADR-020 — Freiroute TMS*
*Versión: 1.0 | Sprint 4 | 2026*
