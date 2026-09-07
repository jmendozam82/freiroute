# ADR-015: Estrategia de Cálculo de Tarifas de Flete

## Estado
Aceptado

## Fecha
2026 — Sprint 3

## Contexto
El catálogo de tarifas base (HU-020) es uno de los módulos más críticos
del TMS. Las tarifas determinan el costo de cada embarque y alimentan:
- El cálculo automático de costos al planificar embarques (Sprint 7)
- La auditoría de facturas de carriers (Sprint 12)
- El dashboard financiero del tenant

Los TMS de referencia (Oracle TMS, SAP TM, MercuryGate) soportan
múltiples modelos de tarificación simultáneos. La decisión de cuál
implementar en Sprint 3 impacta la complejidad de la BD y la BLL.

## Decisión
Sprint 3 implementa un modelo de tarifas **por zona con recargos**.
Es el modelo más usado por transportistas latinoamericanos y cubre
el 80% de los casos de uso del MVP.

### Modelo de tarifa

```
Tarifa base = f(zona_origen, zona_destino, modo, tipo_servicio)
Costo flete = Tarifa base
            × unidad (por kg, por m³, por km, fijo por viaje)
            + Σ recargos aplicables
```

### Tipos de tarifa soportados

| Tipo | Descripción | Ejemplo |
|---|---|---|
| `POR_KG` | Precio por kilogramo | $0.05/kg |
| `POR_M3` | Precio por metro cúbico | $2.50/m³ |
| `POR_KM` | Precio por kilómetro recorrido | $1.20/km |
| `FIJO_VIAJE` | Precio fijo por viaje completo | $450.00/viaje |
| `POR_UNIDAD` | Precio por unidad (pallet, caja) | $8.00/pallet |

### Recargos configurables

| Código | Nombre | Cálculo |
|---|---|---|
| `COMBUSTIBLE` | Fuel surcharge | % sobre tarifa base |
| `PEAJE` | Peajes y casetas | Monto fijo por ruta |
| `SEGURO` | Seguro de carga | % sobre valor declarado |
| `MANIPULACION` | Carga/descarga | Monto fijo |
| `URGENCIA` | Servicio express | % sobre tarifa base |
| `REFRIGERACION` | Cadena de frío | Monto fijo por día |
| `SOBREDIMENSION` | Carga sobredimensionada | % sobre tarifa base |

### Vigencia de tarifas
Las tarifas tienen fecha de inicio y fin. Al calcular el costo de un
embarque se aplica la tarifa vigente en la fecha de pickup planificada.
Historial completo de cambios de tarifa preservado (nunca se elimina).

## Implementación

### Estructura de tablas
```sql
-- Tabla principal de tarifas
tarifas_base (id, empresa_id, carrier_id?, zona_origen_id, zona_destino_id,
              modo_transporte, tipo_servicio, tipo_tarifa,
              precio_unitario, moneda, fecha_vigencia_desde,
              fecha_vigencia_hasta, activo, ...)

-- Recargos por tarifa
recargos_tarifa (id, empresa_id, tarifa_id, codigo_recargo,
                 nombre, tipo_calculo, valor, aplica_sobre,
                 activo, ...)
```

### Motor de cálculo (Sprint 7)
```csharp
// Freiroute.BLL/Interfaces/ICalculoFleteService.cs
// Implementado en Sprint 7 — aquí solo se define la interfaz
public interface ICalculoFleteService
{
    Task<CalculoFleteResultDto> CalcularAsync(
        Guid zonaOrigenId,
        Guid zonaDestinoId,
        string modoTransporte,
        string tipoServicio,
        decimal pesoKg,
        decimal volumenM3,
        decimal distanciaKm,
        decimal valorDeclarado,
        DateTime fechaPickup,
        Guid empresaId,
        Guid? carrierId = null);
}
```

## Alternativas Consideradas

1. **Tarifas por corredor (punto a punto)** — Más preciso pero
   requiere registrar cientos de pares origen-destino. Descartada
   para el MVP — se agrega en v1.5 junto con EP-10.

2. **Tarifas por distancia pura** — Simple pero inexacto para
   operaciones donde la distancia no determina el costo (ej: cruces
   de frontera). Descartada — se soporta como tipo `POR_KM`.

3. **Tarifas por tabla de pesos (weight break)** — Usado en courier
   y LTL. Requiere tabla de rangos. Descartada para MVP — se agrega
   en Sprint 13 (EP-10 Freight Audit).

## Consecuencias

**Positivas:**
- Modelo familiar para transportistas latinoamericanos
- Flexible: combinando tipo_tarifa + recargos cubre la mayoría
  de contratos reales
- Vigencia temporal permite gestionar aumentos sin perder historial

**Negativas / Trade-offs:**
- No soporta tablas de peso (weight break) en MVP
- El cálculo requiere conocer zonas de origen y destino —
  las ubicaciones deben estar zonificadas (HU-016)

## Módulos Afectados
- HU-020: Catálogo de tarifas base (Sprint 3)
- EP-05: Carrier Management — tarifas por carrier (Sprint 6)
- EP-06: Shipment Planning — cálculo al asignar carrier (Sprint 7)
- EP-10: Freight Audit — auditoría de facturas vs. tarifas (Sprint 12)
