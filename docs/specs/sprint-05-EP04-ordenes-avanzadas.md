# Spec: Sprint 5 — EP-04 Órdenes Avanzadas + Fixes Sprint 4

**Sprint:** 05
**Épica:** EP-04 — Order Management (continuación)
**Historias:** HU-028 · HU-029 · HU-030 · HU-031 · HU-032
**Story Points:** 55 pts (HUs nuevas) + fixes de deuda técnica (sin pts de backlog)
**Objetivo del Sprint:** Cerrar los gaps críticos G-14/G-15 del Sprint 4 y entregar
las capacidades avanzadas de órdenes: integración PO/SO, priorización dinámica,
rechazos y re-entregas, SLA management y gestión de reclamos.
**ADRs aplicables:** ADR-019 · ADR-020 · ADR-013 · ADR-003 · ADR-005 · ADR-008

---

## Dependencias

```
Sprint 1 → Auth, tenants, perfiles, permisos, auditoría                    ✅
Sprint 2 → Configuración del tenant (prefijo_orden, configuracion)          ✅
Sprint 3 → clientes (sla_dias_entrega, sla_ventana_*), ubicaciones,
           tarifas_base, tipos_mercancia, unidades_medida                   ✅
Sprint 4 → ordenes (FSM, historial_estados_orden, numeración automática)    ✅

Deuda técnica prioritaria a resolver ANTES de HUs nuevas:
  G-14 🔴 → @BackendDev: BCrypt.Verify en ApiKeyTenantRepository
  G-15 🔴 → @BackendDev: JsonSerializer.Serialize en auditoría de órdenes
  G-16 🟠 → @BackendDev: snapshot real en datosOrden de plantilla
  G-10 🟠 → @BackendDev: origenCreacion=RECURRENTE en órdenes de plantilla
  G-17 🟡 → @BackendDev: historial completo (DRAFT, OrdenId, usuario, fechaConfirmacion)
  G-18 🟡 → @BackendDev: JOINs nombres en listado de órdenes
  G-09 🟡 → @BackendDev: guardar-como-plantilla → 201 CreatedAtAction
  G-11 🟡 → @QA: test integración FrApi.patch abonos
  G-12 🟡 → @QA: tests OrdenesController cobertura ≥60%
  G-08 🟡 → @BackendDev: AuditoriaActivityResponseDto con NombreUsuario/EmailUsuario
```

---

## Orden de Implementación por Capa

```
Fase 1 → @Arquitecto
         Constantes: TipoRechazo, EstadoReclamo, TipoReclamo, SlaStatus,
                     OrigenCreacion (ampliar), AccionAuditoria (ampliar)
         Entities: ReglaPrioridad, RechazoEntrega, Reclamo, HistorialEstadoReclamo
         DTOs: request/response para HU-028→HU-032
         Interfaces DAL + BLL
         Extends: OrdenResponseDto (campos PO/SO, sla_status)
                  OrdenListDto (numero_po)

Fase 2 → @IngenieroDatos
         ALTER TABLE ordenes → numero_po, numero_so
         CREATE TABLE rechazos_entrega
         CREATE TABLE reclamos + historial_estados_reclamo
         CREATE TABLE reglas_prioridad
         Índices + RLS en tablas nuevas
         Repositorios Dapper: IReglaPrioridadRepository, IRechazoEntregaRepository,
                              IReclamoRepository
         Registro en DI

Fase 3 → @BackendDev
         FIXES PRIMERO (G-14, G-15, G-16, G-10, G-17, G-18, G-09, G-08)
         BLL: OrdenPoService (HU-028), PrioridadOrdenService (HU-029),
              RechazoEntregaService (HU-030), SlaService (HU-031),
              ReclamoService (HU-032)
         Job: PrioridadOrdenesJob (HU-029) + SlaMonitorJob (HU-031)
         Controllers: endpoints nuevos en OrdenesController +
                      ReclamosController (nuevo)
         Registro en DI

Fase 4 → @QA
         Fix G-11 + Fix G-12
         Tests BLL ≥80% (acumulado)
         Tests API ≥60% (acumulado)
         QA Report: docs/specs/sprint-05-qa-report.md

Fase 5 → @FrontendDev
         Vistas: Órdenes (extend Index con columnas PO + SLA badge),
                 Rechazo/ReEntrega modal, SLA dashboard widget,
                 Reclamos (Index, Create, Detalle)
```

---

## Tablas de Base de Datos del Sprint 5

### Diagrama de relaciones nuevas

```
ordenes (Sprint 4)
  │── numero_po VARCHAR(100)    ← HU-028 ALTER TABLE
  │── numero_so VARCHAR(100)    ← HU-028 ALTER TABLE
  │
  ├── rechazos_entrega          ← HU-030
  │     └── orden_id FK
  │
  └── reclamos                  ← HU-032
        │── orden_id FK
        └── historial_estados_reclamo
              └── reclamo_id FK

empresas (Sprint 1)
  └── reglas_prioridad          ← HU-029
        └── empresa_id FK
```

---

### ALTER TABLE ordenes (HU-028)

```sql
-- Migration: supabase migration new sprint5_po_so_ordenes
ALTER TABLE ordenes
    ADD COLUMN IF NOT EXISTS numero_po VARCHAR(100),
    ADD COLUMN IF NOT EXISTS numero_so VARCHAR(100);

COMMENT ON COLUMN ordenes.numero_po IS
    'Número de Purchase Order del cliente vinculada a esta orden de transporte.';
COMMENT ON COLUMN ordenes.numero_so IS
    'Número de Sales Order del cliente vinculada a esta orden de transporte.';

CREATE INDEX IF NOT EXISTS idx_ordenes_numero_po
    ON ordenes(empresa_id, numero_po)
    WHERE numero_po IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_ordenes_numero_so
    ON ordenes(empresa_id, numero_so)
    WHERE numero_so IS NOT NULL;
```

---

### CREATE TABLE rechazos_entrega (HU-030)

```sql
-- Migration: supabase migration new sprint5_rechazos_entrega
CREATE TABLE IF NOT EXISTS rechazos_entrega (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id       UUID        NOT NULL REFERENCES empresas(id),
    orden_id         UUID        NOT NULL REFERENCES ordenes(id),
    motivo           VARCHAR(40) NOT NULL,
        -- CLIENTE_AUSENTE | DIRECCION_INCORRECTA | MERCANCIA_DANADA
        -- RECHAZO_CLIENTE | OTRO
    descripcion      TEXT,
    usuario_id       UUID        REFERENCES usuarios(id),
    fecha_creacion   TIMESTAMPTZ NOT NULL DEFAULT now(),
    activo           BOOLEAN     NOT NULL DEFAULT true
);

COMMENT ON TABLE rechazos_entrega IS
    'Registra rechazos de entrega de órdenes en campo. Relación N:1 con ordenes.';

-- RLS
ALTER TABLE rechazos_entrega ENABLE ROW LEVEL SECURITY;

CREATE POLICY rechazos_entrega_tenant_isolation ON rechazos_entrega
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

-- Índices
CREATE INDEX IF NOT EXISTS idx_rechazos_entrega_orden
    ON rechazos_entrega(empresa_id, orden_id);
```

---

### CREATE TABLE reglas_prioridad (HU-029)

```sql
-- Migration: supabase migration new sprint5_reglas_prioridad
CREATE TABLE IF NOT EXISTS reglas_prioridad (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID        NOT NULL REFERENCES empresas(id),
    nombre          VARCHAR(100) NOT NULL,
    condicion       VARCHAR(50)  NOT NULL,
        -- ENTREGA_PROXIMA | CLIENTE_VIP | SIN_AVANCE
    horas_umbral    INTEGER      NOT NULL DEFAULT 24,
    nivel_destino   VARCHAR(20)  NOT NULL DEFAULT 'ALTO',
        -- CRITICO | ALTO
    activo          BOOLEAN      NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE reglas_prioridad IS
    'Reglas configurables por tenant para elevación automática de prioridad de órdenes.';

ALTER TABLE reglas_prioridad ENABLE ROW LEVEL SECURITY;

CREATE POLICY reglas_prioridad_tenant_isolation ON reglas_prioridad
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

CREATE UNIQUE INDEX IF NOT EXISTS idx_reglas_prioridad_nombre
    ON reglas_prioridad(empresa_id, nombre)
    WHERE activo = true;
```

---

### CREATE TABLE reclamos (HU-032)

```sql
-- Migration: supabase migration new sprint5_reclamos
CREATE TABLE IF NOT EXISTS reclamos (
    id                    UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id            UUID         NOT NULL REFERENCES empresas(id),
    orden_id              UUID         NOT NULL REFERENCES ordenes(id),
    numero_reclamo        VARCHAR(30),
        -- Formato: REC-{EMPRESA_PREFIX}-{AÑO}-{SECUENCIA}
    tipo                  VARCHAR(20)  NOT NULL,
        -- DANO | PERDIDA | RETRASO | OTRO
    descripcion           TEXT         NOT NULL,
    monto_reclamado       NUMERIC(15,2),
    estado                VARCHAR(20)  NOT NULL DEFAULT 'ABIERTO',
        -- ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO
    referencias_evidencia TEXT[],
        -- URLs a Supabase Storage (implementación completa: Sprint 11)
    activo                BOOLEAN      NOT NULL DEFAULT true,
    fecha_creacion        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fecha_modificacion    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    creado_por            UUID         REFERENCES usuarios(id),
    modificado_por        UUID         REFERENCES usuarios(id)
);

COMMENT ON TABLE reclamos IS
    'Reclamos formales de clientes por incidencias de entrega (daño, pérdida, retraso).';

ALTER TABLE reclamos ENABLE ROW LEVEL SECURITY;

CREATE POLICY reclamos_tenant_isolation ON reclamos
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

CREATE INDEX IF NOT EXISTS idx_reclamos_orden
    ON reclamos(empresa_id, orden_id);

CREATE INDEX IF NOT EXISTS idx_reclamos_estado
    ON reclamos(empresa_id, estado)
    WHERE activo = true;

CREATE INDEX IF NOT EXISTS idx_reclamos_numero
    ON reclamos(empresa_id, numero_reclamo)
    WHERE numero_reclamo IS NOT NULL;
```

---

### CREATE TABLE historial_estados_reclamo (HU-032)

```sql
CREATE TABLE IF NOT EXISTS historial_estados_reclamo (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID        NOT NULL REFERENCES empresas(id),
    reclamo_id      UUID        NOT NULL REFERENCES reclamos(id),
    estado_anterior VARCHAR(20),
    estado_nuevo    VARCHAR(20) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID        REFERENCES usuarios(id),
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE historial_estados_reclamo ENABLE ROW LEVEL SECURITY;

CREATE POLICY historial_reclamo_tenant_isolation ON historial_estados_reclamo
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

CREATE INDEX IF NOT EXISTS idx_historial_reclamo_reclamo
    ON historial_estados_reclamo(empresa_id, reclamo_id);
```

---

## HU-028 · Gestión de órdenes de compra vinculadas (PO Integration)

**Como** operador, **quiero** vincular órdenes de transporte a órdenes de compra o venta
del cliente, **para** trazabilidad end-to-end entre sistemas ERP/WMS y el TMS.

**Estimación:** 5 pts | **Prioridad:** Media | **Módulo de permiso:** `ordenes`

### Criterios de aceptación

| ID | Criterio | Verificado por |
|----|----------|----------------|
| CA-01 | `numero_po VARCHAR(100)` en `ordenes` (opcional) además del `referencia_cliente` existente | Migration + smoke test |
| CA-02 | `numero_so VARCHAR(100)` en `ordenes`, independiente del PO | Migration + smoke test |
| CA-03 | `GET /api/ordenes?po=PO-20260501` — búsqueda exacta por número de PO | Test integración |
| CA-04 | Una PO puede vincularse a múltiples órdenes (relación 1:N — campo nullable en orden) | Test BLL |
| CA-05 | `GET /api/ordenes/por-po/{numero}` → lista todas las órdenes con esa referencia PO (mismo tenant) | Test integración |
| CA-06 | `numero_po` y `numero_so` son editables vía `PATCH /api/ordenes/{id}` (campos opcionales) | Test integración |
| CA-07 | `numero_po` aparece en `OrdenListDto` y en el filtro de búsqueda del listado paginado | Test BLL |
| CA-08 | `numero_po` y `numero_so` aparecen en `OrdenResponseDto` (detalle) | Test BLL |
| CA-09 | Auditoría registra `VINCULAR_PO` cuando se asigna o cambia el número de PO | Test BLL |

### Nuevos endpoints

```
GET  /api/ordenes?po={numero}           → listado paginado filtrado por numero_po
GET  /api/ordenes/por-po/{numero}       → alias semántico — retorna 200 + lista
PATCH /api/ordenes/{id}                 → ya existe — ampliar para aceptar NumeroPo/NumerySo
```

### Cambios en entidades y DTOs existentes

```csharp
// Ampliar OrdenRequestDto:
public string? NumeroPo  { get; set; }   // max 100 chars
public string? NumeroSo  { get; set; }   // max 100 chars

// Ampliar OrdenResponseDto:
public string? NumeroPo  { get; set; }
public string? NumeroSo  { get; set; }

// Ampliar OrdenListDto:
public string? NumeroPo  { get; set; }

// Ampliar OrdenFiltroDto (query params del listado):
public string? Po        { get; set; }   // filtro por numero_po exact match
```

### SQL del repositorio (GetAllAsync — agregar filtro po)

```sql
-- Agregar al WHERE de GetAllAsync:
AND (@Po IS NULL OR numero_po = @Po)
```

```sql
-- GetPorPoAsync:
SELECT o.*, c.nombre AS cliente_nombre,
       u_orig.nombre AS origen_nombre,
       u_dest.nombre AS destino_nombre
FROM   ordenes o
JOIN   clientes    c      ON c.id = o.cliente_id AND c.empresa_id = o.empresa_id
JOIN   ubicaciones u_orig ON u_orig.id = o.origen_id AND u_orig.empresa_id = o.empresa_id
JOIN   ubicaciones u_dest ON u_dest.id = o.destino_id AND u_dest.empresa_id = o.empresa_id
WHERE  o.empresa_id = @EmpresaId
AND    o.numero_po  = @NumeroPo
AND    o.activo     = true
ORDER BY o.fecha_creacion DESC;
```

---

## HU-029 · Priorización dinámica de órdenes

**Como** dispatcher, **quiero** que las órdenes de alta prioridad se destaquen
automáticamente y se puedan reordenar, **para** planificar primero los envíos más urgentes.

**Estimación:** 5 pts | **Prioridad:** Media | **Módulo de permiso:** `ordenes`

### Criterios de aceptación

| ID | Criterio | Verificado por |
|----|----------|----------------|
| CA-01 | Campo `prioridad` ya existe (CRITICO/ALTO/NORMAL/BAJO) — la HU extiende el comportamiento automático | Sprint 4 ✅ |
| CA-02 | Job diario eleva a ALTO si `fecha_entrega_requerida <= now() + 1 día` y estado = CONFIRMED | Test BLL job |
| CA-03 | Job eleva a ALTO si cliente es VIP (`tipo_cliente='VIP'`) y orden en DRAFT > 2h sin avance | Test BLL job |
| CA-04 | `GET /api/ordenes/criticas` → órdenes CRITICO o ALTO con > 4h sin transición de estado | Test integración |
| CA-05 | Alerta en dashboard: contador "X órdenes críticas sin asignar" (CRITICO sin shipment > 2h) | Test BLL |
| CA-06 | Vista Index de órdenes: columna Prioridad ordenable (CRITICO arriba por defecto) | Smoke test Fase 5 |
| CA-07 | Badge de prioridad CRITICO con animación CSS `pulse` (clase `fr-badge-critico--pulse`) | Smoke test Fase 5 |
| CA-08 | Auditoría registra `AUTO_PRIORIDAD` con prioridad anterior y nueva cuando el sistema eleva | Test BLL |
| CA-09 | `PATCH /api/ordenes/{id}/prioridad` → cambio manual por dispatcher con auditoría `CAMBIO_PRIORIDAD` | Test integración |

### Nuevos endpoints

```
GET   /api/ordenes/criticas             → 200 + lista de órdenes CRITICO/ALTO sin avanzar > 4h
PATCH /api/ordenes/{id}/prioridad       → 200 + OrdenResponseDto actualizado
      Body: { "prioridad": "CRITICO", "motivo": "Cliente solicitó urgencia" }
```

### Nuevo DTOs

```csharp
// PrioridadRequestDto
public string Prioridad { get; set; } = null!;   // validar: CRITICO|ALTO|NORMAL|BAJO
public string? Motivo   { get; set; }
```

### PrioridadOrdenesJob — Background Job (patrón ADR-013)

```csharp
// Freiroute.API/BackgroundJobs/PrioridadOrdenesJob.cs
// IHostedService con PeriodicTimer — intervalo 6 horas
// Lógica:
//   1. Cargar reglas_prioridad activas del tenant (o usar defaults embebidos)
//   2. Regla ENTREGA_PROXIMA: ordenes en CONFIRMED con fecha_entrega <= now()+24h → ALTO
//   3. Regla CLIENTE_VIP: ordenes en DRAFT > 2h con cliente VIP → ALTO
//   4. Para cada elevación: UPDATE ordenes SET prioridad=@Nuevo WHERE...
//      + RegistrarAsync auditoría con accion='AUTO_PRIORIDAD'
//      + Insertar historial_estados_orden (estado no cambia, se documenta el cambio de prioridad)
```

### SQL — GetCriticasAsync

```sql
SELECT o.*, c.nombre AS cliente_nombre,
       u_orig.nombre AS origen_nombre,
       u_dest.nombre AS destino_nombre,
       EXTRACT(EPOCH FROM (now() - (
           SELECT MAX(h.fecha_creacion)
           FROM historial_estados_orden h
           WHERE h.orden_id = o.id
       )))/3600 AS horas_sin_avance
FROM   ordenes o
JOIN   clientes    c      ON c.id = o.cliente_id AND c.empresa_id = o.empresa_id
JOIN   ubicaciones u_orig ON u_orig.id = o.origen_id AND u_orig.empresa_id = o.empresa_id
JOIN   ubicaciones u_dest ON u_dest.id = o.destino_id AND u_dest.empresa_id = o.empresa_id
WHERE  o.empresa_id = @EmpresaId
AND    o.prioridad  IN ('CRITICO', 'ALTO')
AND    o.activo     = true
AND    o.estado NOT IN ('DELIVERED', 'CLOSED', 'CANCELLED')
AND    (
    SELECT MAX(h.fecha_creacion)
    FROM   historial_estados_orden h
    WHERE  h.orden_id = o.id
) < now() - INTERVAL '4 hours'
ORDER BY
    CASE o.prioridad WHEN 'CRITICO' THEN 0 ELSE 1 END,
    o.fecha_entrega_requerida ASC NULLS LAST;
```

---

## HU-030 · Gestión de rechazos y re-entregas

**Como** operador, **quiero** registrar rechazos de entrega y gestionar re-entregas,
**para** resolver incidencias de entrega fallida de manera formal y trazable.

**Estimación:** 5 pts | **Prioridad:** Media | **Módulo de permiso:** `ordenes`

### Criterios de aceptación

| ID | Criterio | Verificado por |
|----|----------|----------------|
| CA-01 | `POST /api/ordenes/{id}/rechazo` — registra rechazo con motivo obligatorio (ver CA-02) | Test integración |
| CA-02 | Motivos válidos: `CLIENTE_AUSENTE | DIRECCION_INCORRECTA | MERCANCIA_DANADA | RECHAZO_CLIENTE | OTRO` | Validator |
| CA-03 | El rechazo mueve la orden a `FAILED_DELIVERY` vía FSM (ADR-019 — transición desde IN_TRANSIT) | Test BLL FSM |
| CA-04 | `POST /api/ordenes/{id}/re-entrega` — crea nueva orden de re-entrega vinculada a la original | Test integración |
| CA-05 | La re-entrega hereda: cliente_id, origen_id, destino_id, tipo_mercancia_id, unidad_medida_id de la original | Test BLL |
| CA-06 | La re-entrega tiene `orden_origen_id` = `{id}` (mismo campo que split — ADR-020) | Test BLL |
| CA-07 | La re-entrega inicia en estado `CONFIRMED` (no DRAFT — requiere atención inmediata) | Test BLL |
| CA-08 | Cargo de re-entrega: leer `factor_reentrega` de `configuracion` del tenant (default 1.5). `monto_flete` = tarifa original × factor | Test BLL |
| CA-09 | Notificación al cliente (stub email) cuando se registra el rechazo | Test BLL |
| CA-10 | `GET /api/ordenes/{id}/re-entregas` → historial de re-entregas de esa orden (campo `orden_origen_id`) | Test integración |
| CA-11 | Auditoría registra `RECHAZO_ENTREGA` y `CREAR_REENTREGA` con detalles JSON | Test BLL |

### Nuevos endpoints

```
POST /api/ordenes/{id}/rechazo          → 201 + RechazoEntregaResponseDto
POST /api/ordenes/{id}/re-entrega       → 201 + OrdenResponseDto (nueva orden)
GET  /api/ordenes/{id}/re-entregas      → 200 + List<OrdenListDto>
```

### Nuevos DTOs

```csharp
// RechazoEntregaRequestDto
public string Motivo       { get; set; } = null!;   // enum TipoRechazo
public string? Descripcion { get; set; }

// RechazoEntregaResponseDto
public Guid   Id           { get; set; }
public Guid   OrdenId      { get; set; }
public string Motivo       { get; set; } = null!;
public string? Descripcion { get; set; }
public string UsuarioNombre { get; set; } = null!;
public DateTime FechaCreacion { get; set; }

// ReEntregaRequestDto (opcional — solo si se quiere sobreescribir algo)
public string? Instrucciones { get; set; }   // instrucciones especiales de re-entrega
```

### Constantes

```csharp
// Freiroute.Utility/Constants/TipoRechazo.cs
public static class TipoRechazo
{
    public const string ClienteAusente       = "CLIENTE_AUSENTE";
    public const string DireccionIncorrecta  = "DIRECCION_INCORRECTA";
    public const string MercanciaDanada      = "MERCANCIA_DANADA";
    public const string RechazoCliente       = "RECHAZO_CLIENTE";
    public const string Otro                 = "OTRO";

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        ClienteAusente, DireccionIncorrecta, MercanciaDanada,
        RechazoCliente, Otro
    };
}
```

### SQL InsertRechazoAsync

```sql
INSERT INTO rechazos_entrega
       (id, empresa_id, orden_id, motivo, descripcion, usuario_id)
VALUES (@Id, @EmpresaId, @OrdenId, @Motivo, @Descripcion, @UsuarioId)
RETURNING id;
```

### Regla de negocio: factor_reentrega

```csharp
// ConfiguracionRepository — agregar campo o leer desde JSONB config
// Si no existe, usar 1.5 por defecto
// El campo a agregar en configuracion:
//   factor_reentrega NUMERIC(4,2) NOT NULL DEFAULT 1.5
// Alternativa sin migración: constante en Utility hasta Sprint 6
public const decimal FactorReentregaDefault = 1.5m;
```

> **✅ Decisión de arquitectura (Fase 1 — @Arquitecto, verificado contra migraciones):**
> El campo `factor_reentrega` **NO existe** en la BD. Además, la tabla `configuracion`
> NO existe como tabla física — la configuración operativa del tenant se persiste en
> **`empresas`** (ver `IConfiguracionRepository`: *"NO tiene tabla propia — lee y
> actualiza un subset de campos de la tabla 'empresas'"*).
> **Decisión:** se define la constante `ConfiguracionDefaults.FactorReentregaDefault = 1.5m`
> en `Freiroute.Utility/Constants/ConfiguracionDefaults.cs` como default en código.
> @IngenieroDatos debe agregar en la migración Sprint 5:
> ```sql
> ALTER TABLE empresas
>     ADD COLUMN IF NOT EXISTS factor_reentrega NUMERIC(4,2) NOT NULL DEFAULT 1.5;
> ```
> La BLL (HU-030 CA-08) leerá el valor de BD si está configurado y usará el default
> de Utility como fallback.

---

## HU-031 · SLA Management por cliente

**Como** gerente de operaciones, **quiero** definir SLAs de entrega por cliente y
monitorear su cumplimiento en tiempo real, **para** gestionar compromisos contractuales
y detectar riesgos antes del vencimiento.

**Estimación:** 8 pts | **Prioridad:** Media | **Módulo de permiso:** `ordenes` (lectura) + `analytics` (reportes)

### Criterios de aceptación

| ID | Criterio | Verificado por |
|----|----------|----------------|
| CA-01 | `sla_dias_entrega`, `sla_ventana_inicio`, `sla_ventana_fin` ya en `Cliente` (Sprint 3) — extender con monitoreo | Sprint 3 ✅ |
| CA-02 | `GET /api/ordenes/sla-en-riesgo` → órdenes donde `fecha_entrega_requerida - now() < 24h` y estado < `IN_TRANSIT` | Test integración |
| CA-03 | `GET /api/clientes/{id}/sla-cumplimiento` → % de órdenes `DELIVERED` a tiempo en últimos 30 días | Test integración |
| CA-04 | Campo calculado `sla_status` en `OrdenResponseDto`: `EN_RIESGO | CRITICO | OK | VENCIDO` | Test BLL |
| CA-05 | `sla_status = VENCIDO` si `fecha_entrega_requerida < now()` y estado NOT IN (`DELIVERED`,`CLOSED`,`CANCELLED`) | Test BLL |
| CA-06 | `sla_status = CRITICO` si entrega vence en < 2h y estado < `IN_TRANSIT` | Test BLL |
| CA-07 | `GET /api/reportes/sla` → reporte de cumplimiento por cliente (body: `{ "desde": "...", "hasta": "..." }`) | Test integración |
| CA-08 | Job diario `SlaMonitorJob` envía alerta (stub email) a rol ADMIN cuando cliente VIP tiene SLA < 90% en el mes | Test BLL |
| CA-09 | `sla_status` se incluye en `OrdenListDto` para mostrar badge en la vista Index | Test BLL |

### Nuevos endpoints

```
GET /api/ordenes/sla-en-riesgo                → 200 + List<OrdenListDto>
GET /api/clientes/{id}/sla-cumplimiento       → 200 + SlaClienteResponseDto
GET /api/reportes/sla                         → 200 + List<SlaReporteItemDto>
    Query: ?desde=2026-01-01&hasta=2026-09-08
```

### Nuevos DTOs

```csharp
// SlaClienteResponseDto
public Guid    ClienteId          { get; set; }
public string  ClienteNombre      { get; set; } = null!;
public int     TotalOrdenes       { get; set; }
public int     OrdenesATiempo     { get; set; }
public int     OrdenesTardias     { get; set; }
public decimal PorcentajeCumplimiento { get; set; }   // 0-100
public int     SlasDias           { get; set; }       // SLA configurado en días

// SlaReporteItemDto
public Guid    ClienteId          { get; set; }
public string  ClienteNombre      { get; set; } = null!;
public string  TipoCliente        { get; set; } = null!;
public decimal PorcentajeCumplimiento { get; set; }
public int     TotalOrdenes       { get; set; }
public int     OrdenesATiempo     { get; set; }
public int     OrdenesTardias     { get; set; }
```

### Constantes

```csharp
// Freiroute.Utility/Constants/SlaStatus.cs
public static class SlaStatus
{
    public const string Ok        = "OK";
    public const string EnRiesgo  = "EN_RIESGO";
    public const string Critico   = "CRITICO";
    public const string Vencido   = "VENCIDO";
}
```

### Lógica calculada SlaStatus en BLL

```csharp
// SlaService.CalcularSlaStatus — método estático / helper
public static string Calcular(DateTime? fechaEntregaRequerida, string estadoOrden)
{
    var estadosFinales = new[] { "DELIVERED", "CLOSED", "CANCELLED" };
    if (estadosFinales.Contains(estadoOrden)) return SlaStatus.Ok;
    if (fechaEntregaRequerida is null)        return SlaStatus.Ok;

    var ahora = DateTime.UtcNow;
    var margen = fechaEntregaRequerida.Value - ahora;

    if (margen < TimeSpan.Zero)                 return SlaStatus.Vencido;
    if (margen < TimeSpan.FromHours(2))         return SlaStatus.Critico;
    if (margen < TimeSpan.FromHours(24))        return SlaStatus.EnRiesgo;
    return SlaStatus.Ok;
}
```

### SQL — SlaEnRiesgoAsync

```sql
SELECT o.*, c.nombre AS cliente_nombre,
       u_orig.nombre AS origen_nombre,
       u_dest.nombre AS destino_nombre
FROM   ordenes o
JOIN   clientes    c      ON c.id = o.cliente_id AND c.empresa_id = o.empresa_id
JOIN   ubicaciones u_orig ON u_orig.id = o.origen_id AND u_orig.empresa_id = o.empresa_id
JOIN   ubicaciones u_dest ON u_dest.id = o.destino_id AND u_dest.empresa_id = o.empresa_id
WHERE  o.empresa_id = @EmpresaId
AND    o.activo     = true
AND    o.estado NOT IN ('DELIVERED', 'CLOSED', 'CANCELLED')
AND    o.fecha_entrega_requerida IS NOT NULL
AND    o.fecha_entrega_requerida <= now() + INTERVAL '24 hours'
ORDER BY o.fecha_entrega_requerida ASC;
```

### SQL — SlaClienteAsync (cumplimiento 30 días)

```sql
SELECT
    COUNT(*)                                       AS total_ordenes,
    SUM(CASE
        WHEN fecha_entrega_real <= fecha_entrega_requerida THEN 1
        ELSE 0
    END)                                           AS ordenes_a_tiempo
FROM ordenes
WHERE empresa_id = @EmpresaId
AND   cliente_id = @ClienteId
AND   estado     = 'DELIVERED'
AND   activo     = true
AND   fecha_entrega_real IS NOT NULL
AND   fecha_creacion >= now() - INTERVAL '30 days';
```

> **✅ Decisión de arquitectura (Fase 1 — @Arquitecto, verificado contra migraciones):**
> La columna `fecha_entrega_real` **NO existe** en `ordenes` (migración
> `20260907000005_tabla_ordenes.sql`). @IngenieroDatos debe agregarla como parte
> de la migración Sprint 5:
> ```sql
> ALTER TABLE ordenes
>     ADD COLUMN IF NOT EXISTS fecha_entrega_real TIMESTAMPTZ;
> COMMENT ON COLUMN ordenes.fecha_entrega_real IS
>     'Fecha/hora real de entrega — alimenta el cálculo de cumplimiento SLA (HU-031 CA-03).';
> ```
> La BLL la actualiza en la transición FSM a DELIVERED (Fase 3).

### SlaMonitorJob — Background Job (ADR-013)

```
// PeriodicTimer — intervalo 24 horas (hora configurable: 08:00 local)
// Lógica:
//   1. Para cada empresa activa
//   2. Para cada cliente VIP
//   3. Calcular cumplimiento SLA último mes
//   4. Si % < 90: enviar notificación stub a usuarios con rol ADMIN
//   5. Log estructurado Serilog con métricas
```

---

## HU-032 · Gestión de reclamos (Claims Management)

**Como** cliente o administrador, **quiero** registrar y gestionar reclamos por
incidencias de entrega, **para** obtener resolución formal y compensación cuando corresponda.

**Estimación:** 8 pts | **Prioridad:** Media | **Módulo de permiso:** `ordenes`

### Criterios de aceptación

| ID | Criterio | Verificado por |
|----|----------|----------------|
| CA-01 | `POST /api/reclamos` — crear reclamo con `orden_id`, `tipo`, `descripcion`, `monto_reclamado` | Test integración → 201 |
| CA-02 | El reclamo se vincula a `orden_id` existente del mismo tenant (FK + validación BLL) | Test BLL + integración |
| CA-03 | FSM de estados: `ABIERTO → EN_REVISION → APROBADO/RECHAZADO → CERRADO` | Test BLL |
| CA-04 | `PATCH /api/reclamos/{id}/estado` → transición de estado con motivo obligatorio + historial | Test integración |
| CA-05 | Campo `referencias_evidencia TEXT[]` acepta lista de URLs; validación: URLs bien formadas | Test BLL |
| CA-06 | `GET /api/reclamos` → listado paginado (filtros: estado, tipo, fecha_desde, fecha_hasta) | Test integración |
| CA-07 | `GET /api/reclamos/{id}` → detalle con `historial_estados_reclamo` incluido | Test integración |
| CA-08 | Notificación stub al cliente en cada cambio de estado del reclamo | Test BLL |
| CA-09 | `GET /api/reportes/reclamos` → reporte paginado por periodo, tipo y resolución | Test integración |
| CA-10 | `GET /api/clientes/{id}/reclamos` → reclamos del cliente del tenant | Test integración |
| CA-11 | Auditoría registra `CREAR_RECLAMO` y `CAMBIO_ESTADO_RECLAMO` con detalles JSON | Test BLL |
| CA-12 | Solo usuarios con rol que tenga `ordenes:update` pueden mover a `APROBADO` o `RECHAZADO` | Test integración 403 |

### Nuevo Controller: ReclamosController

```
POST   /api/reclamos                    → 201 + ReclamoResponseDto
GET    /api/reclamos                    → 200 + PagedResult<ReclamoListDto>
GET    /api/reclamos/{id}               → 200 + ReclamoResponseDto (con historial)
PATCH  /api/reclamos/{id}/estado        → 200 + ReclamoResponseDto
GET    /api/reportes/reclamos           → 200 + List<ReclamoReporteItemDto>
GET    /api/clientes/{id}/reclamos      → 200 (en ClientesController ya existente)
```

### Nuevos DTOs

```csharp
// ReclamoRequestDto
public Guid    OrdenId            { get; set; }
public string  Tipo               { get; set; } = null!;   // TipoReclamo constant
public string  Descripcion        { get; set; } = null!;
public decimal? MontoReclamado    { get; set; }
public List<string>? ReferenciasEvidencia { get; set; }

// ReclamoEstadoRequestDto
public string  EstadoNuevo        { get; set; } = null!;
public string  Motivo             { get; set; } = null!;

// ReclamoListDto
public Guid    Id                 { get; set; }
public string  NumeroReclamo      { get; set; } = null!;
public string  Tipo               { get; set; } = null!;
public string  Estado             { get; set; } = null!;
public decimal? MontoReclamado    { get; set; }
public string  OrdenNumero        { get; set; } = null!;   // JOIN con ordenes
public string  ClienteNombre      { get; set; } = null!;   // JOIN con clientes
public DateTime FechaCreacion     { get; set; }

// ReclamoResponseDto (detalle completo)
public Guid    Id                 { get; set; }
public string  NumeroReclamo      { get; set; } = null!;
public Guid    OrdenId            { get; set; }
public string  OrdenNumero        { get; set; } = null!;
public string  Tipo               { get; set; } = null!;
public string  Descripcion        { get; set; } = null!;
public string  Estado             { get; set; } = null!;
public decimal? MontoReclamado    { get; set; }
public List<string>? ReferenciasEvidencia { get; set; }
public DateTime FechaCreacion     { get; set; }
public DateTime FechaModificacion { get; set; }
public List<HistorialEstadoReclamoDto> Historial { get; set; } = new();

// HistorialEstadoReclamoDto
public string  EstadoAnterior     { get; set; } = null!;
public string  EstadoNuevo        { get; set; } = null!;
public string? Motivo             { get; set; }
public string  UsuarioNombre      { get; set; } = null!;
public DateTime FechaCreacion     { get; set; }

// ReclamoReporteItemDto
public string  Tipo               { get; set; } = null!;
public string  Estado             { get; set; } = null!;
public int     Total              { get; set; }
public decimal MontoTotal         { get; set; }
public decimal MontoPromedio      { get; set; }
```

### Constantes

```csharp
// Freiroute.Utility/Constants/TipoReclamo.cs
public static class TipoReclamo
{
    public const string Dano    = "DANO";
    public const string Perdida = "PERDIDA";
    public const string Retraso = "RETRASO";
    public const string Otro    = "OTRO";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Dano, Perdida, Retraso, Otro };
}

// Freiroute.Utility/Constants/EstadoReclamo.cs
public static class EstadoReclamo
{
    public const string Abierto    = "ABIERTO";
    public const string EnRevision = "EN_REVISION";
    public const string Aprobado   = "APROBADO";
    public const string Rechazado  = "RECHAZADO";
    public const string Cerrado    = "CERRADO";

    // Transiciones válidas (FSM simple)
    public static readonly Dictionary<string, string[]> Transiciones = new()
    {
        [Abierto]    = new[] { EnRevision },
        [EnRevision] = new[] { Aprobado, Rechazado },
        [Aprobado]   = new[] { Cerrado },
        [Rechazado]  = new[] { Cerrado },
        [Cerrado]    = Array.Empty<string>()
    };
}
```

### Numeración de reclamos

```csharp
// Formato: REC-{PREFIX}-{AÑO}-{SECUENCIA:0000}
// Ejemplo: REC-FRT-2026-0001
// Usar el mismo contador de contadores_orden con tipo 'RECLAMO'
// O crear secuencia propia — decisión @Arquitecto Fase 1
// Recomendación: reutilizar contadores_orden con un nuevo 'tipo_contador'
```

### SQL — GetReclamosAsync (listado paginado)

```sql
SELECT r.*,
       o.numero_orden AS orden_numero,
       c.nombre       AS cliente_nombre,
       u.nombre       AS creado_por_nombre
FROM   reclamos r
JOIN   ordenes  o ON o.id = r.orden_id AND o.empresa_id = r.empresa_id
JOIN   clientes c ON c.id = o.cliente_id AND c.empresa_id = r.empresa_id
LEFT JOIN usuarios u ON u.id = r.creado_por
WHERE  r.empresa_id = @EmpresaId
AND    r.activo     = true
AND    (@Estado     IS NULL OR r.estado = @Estado)
AND    (@Tipo       IS NULL OR r.tipo   = @Tipo)
AND    (@Desde      IS NULL OR r.fecha_creacion >= @Desde)
AND    (@Hasta      IS NULL OR r.fecha_creacion <= @Hasta)
ORDER BY r.fecha_creacion DESC
LIMIT @PageSize OFFSET @Offset;
```

---

## Fixes de Deuda Técnica Sprint 4 — Instrucciones para @BackendDev

> Los fixes se implementan en Fase 3, ANTES de las HUs nuevas.
> Cada fix tiene su test requerido — el @QA valida en Fase 4.

### Fix G-14 · BCrypt.Verify en ApiKey 🔴 PRIORITARIO

```csharp
// Archivo: Freiroute.DAL/Repositories/ApiKeyTenantRepository.cs
// Método: GetByClaveHashAsync

// ANTES (incorrecto — BCrypt no es reversible):
// WHERE clave_hash = @ClaveHash  ← siempre falla

// DESPUÉS:
public async Task<ApiKeyTenant?> GetByClaveHashAsync(string rawKey)
{
    const string sql = @"
        SELECT * FROM api_keys_tenant
        WHERE activo = true
          AND empresa_id = @EmpresaId";
    // Si hay muchas keys, filtrar por empresa primero para reducir el set
    var keys = await _db.QueryAsync<ApiKeyTenant>(sql, new { EmpresaId = empresaId });
    return keys.FirstOrDefault(k => BCrypt.Net.BCrypt.Verify(rawKey, k.ClaveHash));
}
// Nota: el empresaId viene del tenant middleware — pasar como parámetro al método
```

**Test requerido (integración sin mock de repo):**
1. `POST /api/integracion/api-keys` → obtener rawKey de la respuesta
2. Llamar `ValidarApiKeyAsync(rawKey)` → esperar `true` + empresaId correcto
3. Llamar con rawKey+"X" → esperar `false`

### Fix G-15 · Auditoría JSON en servicios de órdenes 🔴 PRIORITARIO

```csharp
// Aplicar en: OrdenService, OrdenImportService, OrdenApiExternaService, PlantillaOrdenService
// Agregar using: System.Text.Json

// ANTES:
await _auditoriaService.RegistrarAsync(
    "ordenes", "CREATE", empresaId, usuarioId,
    detalles: new { ordenId = id, referencia = dto.ReferenciaCliente });

// DESPUÉS:
await _auditoriaService.RegistrarAsync(
    "ordenes", "CREATE", empresaId, usuarioId,
    detalles: JsonSerializer.Serialize(
        new { ordenId = id, referencia = dto.ReferenciaCliente }));
```

**Verificación post-fix:**
```bash
GET /api/auditoria?modulo=ordenes
# Esperado: total > 0, campo "detalles" con JSON legible (no null, no error)
```

### Fix G-16 · Snapshot real en datosOrden de plantilla 🟠

```csharp
// Archivo: Freiroute.BLL/Services/PlantillaOrdenService.cs
// Método: GuardarComoPlantillaAsync

// Cargar la orden completa antes de serializar:
var orden = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
    ?? throw new NotFoundException("Orden", ordenId);

var snapshot = new OrdenRequestDto
{
    ClienteId       = orden.ClienteId,
    OrigenId        = orden.OrigenId,
    DestinoId       = orden.DestinoId,
    TipoMercanciaId = orden.TipoMercanciaId,
    UnidadMedidaId  = orden.UnidadMedidaId,
    TipoEmbalajeId  = orden.TipoEmbalajeId,
    Cantidad        = orden.Cantidad,
    PesoKg          = orden.PesoKg,
    VolumenM3       = orden.VolumenM3,
    ModoTransporte  = orden.ModoTransporte,
    NivelServicio   = orden.NivelServicio,
    Prioridad       = orden.Prioridad,
    Instrucciones   = orden.Instrucciones
};
plantilla.DatosOrden = JsonSerializer.Serialize(snapshot);
```

### Fix G-10 · OrigenCreacion=RECURRENTE 🟠

```csharp
// Archivo: Freiroute.BLL/Services/OrdenService.cs
public async Task<OrdenResponseDto> CreateAsync(
    OrdenRequestDto dto,
    Guid empresaId,
    Guid usuarioId,
    string origenCreacion = OrigenCreacion.Manual)   // ← nuevo parámetro con default
{
    var orden = new Orden
    {
        // ...
        OrigenCreacion = origenCreacion,   // ← usar el parámetro
    };
}

// Archivo: Freiroute.BLL/Services/PlantillaOrdenService.cs
// En el método que genera órdenes desde plantilla/recurrencia:
await _ordenService.CreateAsync(
    dto, empresaId, usuarioId,
    origenCreacion: OrigenCreacion.Recurrente);   // ← pasar RECURRENTE
```

### Fix G-17 · Historial completo 🟡

**4 sub-fixes en OrdenService + OrdenRepository:**

```
1. Registro en DRAFT: verificar que InsertHistorialAsync se llama en CreateAsync
   con estadoAnterior=null, estadoNuevo="DRAFT", ordenId=resultado del RETURNING id

2. OrdenId=Guid.Empty: asegurarse de pasar el ID retornado por la BD al historial,
   no Guid.Empty del objeto antes del INSERT

3. usuarioNombre=null: agregar al SELECT de GetHistorialAsync:
   LEFT JOIN usuarios u ON u.id = h.usuario_id
   Y en el SELECT: u.nombre AS usuario_nombre

4. fechaConfirmacion=null: verificar COALESCE(@FechaConfirmacion, fecha_confirmacion)
   en UpdateEstadoAsync — el parámetro debe llevar valor cuando corresponde
```

### Fix G-18 · JOINs nombres en listado 🟡

```sql
-- Agregar a GetAllAsync en OrdenRepository:
JOIN clientes    c      ON c.id      = o.cliente_id  AND c.empresa_id = o.empresa_id
JOIN ubicaciones u_orig ON u_orig.id = o.origen_id   AND u_orig.empresa_id = o.empresa_id
JOIN ubicaciones u_dest ON u_dest.id = o.destino_id  AND u_dest.empresa_id = o.empresa_id
-- En el SELECT agregar:
-- c.nombre AS cliente_nombre
-- u_orig.nombre AS origen_nombre
-- u_dest.nombre AS destino_nombre
```

### Fix G-09 · guardar-como-plantilla → 201 🟡

```csharp
// Archivo: Freiroute.API/Controllers/OrdenesController.cs
// Método: GuardarComoPlantilla

// ANTES:
return Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result));

// DESPUÉS:
return CreatedAtAction(
    nameof(PlantillasOrdenController.GetById),
    "PlantillasOrden",
    new { id = result.Id },
    ApiResponse<PlantillaOrdenResponseDto>.Ok(result));
```

### Fix G-08 · AuditoriaActivityResponseDto con NombreUsuario 🟡

```sql
-- AuditoriaRepository.GetAllAsync — agregar JOIN:
LEFT JOIN usuarios u ON u.id = a.usuario_id AND u.empresa_id = a.empresa_id

-- En SELECT agregar:
-- u.nombre AS nombre_usuario
-- u.email  AS email_usuario
```

```csharp
// AuditoriaActivityResponseDto — agregar campos:
public string? NombreUsuario { get; set; }
public string? EmailUsuario  { get; set; }
```

---

## Checklist DoD Sprint 5

### @BackendDev
- [ ] Fix G-14: BCrypt.Verify implementado — API key válida retorna 200 en smoke test
- [ ] Fix G-15: Auditoría de órdenes registra total > 0 — `GET /api/auditoria?modulo=ordenes`
- [ ] Fix G-16: `DatosOrden` de plantilla contiene snapshot real (no `"{}"`)
- [ ] Fix G-10: `OrigenCreacion` = `RECURRENTE` en órdenes generadas por job
- [ ] Fix G-17: Historial: DRAFT registrado, OrdenId real, usuarioNombre, fechaConfirmacion
- [ ] Fix G-18: Listado de órdenes muestra nombres reales (no UUIDs)
- [ ] Fix G-09: `POST guardar-como-plantilla` retorna 201
- [ ] Fix G-08: Auditoría muestra NombreUsuario y EmailUsuario
- [ ] HU-028: Endpoints PO/SO implementados y respondiendo correctamente
- [ ] HU-029: PrioridadOrdenesJob registrado + `GET /api/ordenes/criticas` operativo
- [ ] HU-030: Rechazo registra FAILED_DELIVERY en FSM + re-entrega crea orden nueva
- [ ] HU-031: `GET /api/ordenes/sla-en-riesgo` + SlaMonitorJob registrado
- [ ] HU-032: ReclamosController con CRUD completo + FSM de estados

### @QA
- [ ] Fix G-11: test integración `PATCH /api/ordenes/{id}/abonos` en verde
- [ ] Fix G-12: `OrdenesController` cobertura ≥ 60%
- [ ] Tests BLL ≥ 80% (acumulado)
- [ ] Tests API ≥ 60% (acumulado)
- [ ] QA Report en `docs/specs/sprint-05-qa-report.md`

### @FrontendDev
- [ ] Vista Index de órdenes: columna PO + badge SLA + badge Prioridad con `pulse`
- [ ] Modal de rechazo (POST /api/ordenes/{id}/rechazo) desde vista Detalle
- [ ] Vista Reclamos: Index (listado paginado) + Create + Detalle con historial
- [ ] Widget SLA en dashboard: contador "órdenes en riesgo"

### @PM (cierre)
- [ ] `dotnet build` 0 errores / 0 warnings
- [ ] `dotnet test` todos los tests en verde (≥ 1000 estimado)
- [ ] Smoke test Sprint 5 ejecutado (S0–S20 o equivalente)
- [ ] `deuda-tecnica.md` actualizado: G-08 → G-18 cerrados
- [ ] `contexto-pm-sprint5-cierre.md` generado para handoff Sprint 6

---

*Spec: Sprint 5 — EP-04 Órdenes Avanzadas + Fixes Sprint 4*
*Freiroute TMS | Versión: 1.0 | Fecha: 2026-09-08 | Autor: @PM*
