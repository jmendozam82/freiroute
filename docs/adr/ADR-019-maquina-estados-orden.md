# ADR-019: Máquina de Estados de la Orden de Transporte

## Estado
Aceptado

## Fecha
2026 — Sprint 4

## Decisores
@PM · @Arquitecto

## Módulo afectado
EP-04 Order Management

---

## Contexto

El módulo de órdenes es el corazón operacional del TMS. Una orden nace
como solicitud del cliente y cierra como factura pagada. Durante su ciclo
de vida pasa por múltiples actores (operador, dispatcher, conductor, cliente)
y puede bifurcarse por excepciones (cancelación, retención, entrega fallida,
división en splits).

Sin una máquina de estados formal y auditada el sistema no puede garantizar
trazabilidad, control de acceso por rol ni prevención de transiciones
incoherentes. Los sistemas de referencia (Oracle TMS, SAP TM, MercuryGate)
modelan la orden como una FSM (Finite State Machine) con transiciones
explícitas y guards de autorización.

La tabla `ordenes` persiste el estado como string para facilitar la lectura
en SQL y en reportes sin necesidad de joins adicionales (convención del proyecto
según ADR-016).

---

## Decisión

Se implementa una **máquina de estados finitos (FSM)** para las órdenes de
transporte. La FSM vive en `Freiroute.Utility/Orders/OrderStateMachine.cs`
como clase estática pura, sin dependencias de infraestructura, y es la
**única fuente de verdad** para las transiciones válidas del sistema.

---

## Estados del ciclo de vida principal

```
DRAFT → CONFIRMED → ASSIGNED → PICKUP_SCHEDULED
     → IN_TRANSIT → DELIVERED → INVOICED → CLOSED
```

## Estados de excepción

```
→ CANCELLED        (desde: DRAFT, CONFIRMED, ASSIGNED)
→ ON_HOLD          (desde: CONFIRMED, ASSIGNED, PICKUP_SCHEDULED, IN_TRANSIT)
→ FAILED_DELIVERY  (desde: IN_TRANSIT)
→ PARTIALLY_SPLIT  (desde: CONFIRMED, ASSIGNED — cuando se divide la orden)
```

## Retornos desde excepción

```
ON_HOLD         → CONFIRMED    (cuando se resuelve la retención)
FAILED_DELIVERY → IN_TRANSIT   (re-entrega programada)
```

---

## Tabla completa de transiciones

| Estado origen      | Evento                  | Estado destino    | Rol autorizado                       |
|--------------------|-------------------------|-------------------|--------------------------------------|
| DRAFT              | Confirmar orden         | CONFIRMED         | OPERADOR, ADMIN                      |
| DRAFT              | Cancelar                | CANCELLED         | OPERADOR, ADMIN                      |
| CONFIRMED          | Asignar a shipment      | ASSIGNED          | DISPATCHER, ADMIN                    |
| CONFIRMED          | Poner en espera         | ON_HOLD           | OPERADOR, DISPATCHER, ADMIN          |
| CONFIRMED          | Dividir (split)         | PARTIALLY_SPLIT   | DISPATCHER, ADMIN                    |
| CONFIRMED          | Cancelar                | CANCELLED         | OPERADOR, ADMIN                      |
| ASSIGNED           | Programar pickup        | PICKUP_SCHEDULED  | DISPATCHER, ADMIN                    |
| ASSIGNED           | Poner en espera         | ON_HOLD           | DISPATCHER, ADMIN                    |
| ASSIGNED           | Cancelar                | CANCELLED         | ADMIN                                |
| PICKUP_SCHEDULED   | Iniciar tránsito        | IN_TRANSIT        | DISPATCHER, CONDUCTOR, ADMIN         |
| PICKUP_SCHEDULED   | Poner en espera         | ON_HOLD           | DISPATCHER, ADMIN                    |
| IN_TRANSIT         | Confirmar entrega       | DELIVERED         | CONDUCTOR, DISPATCHER, ADMIN         |
| IN_TRANSIT         | Entrega fallida         | FAILED_DELIVERY   | CONDUCTOR, DISPATCHER, ADMIN         |
| IN_TRANSIT         | Poner en espera         | ON_HOLD           | DISPATCHER, ADMIN                    |
| DELIVERED          | Generar factura         | INVOICED          | ADMIN, módulo facturación (Sprint 12)|
| INVOICED           | Cerrar orden            | CLOSED            | ADMIN, módulo facturación (Sprint 12)|
| ON_HOLD            | Reactivar               | CONFIRMED         | DISPATCHER, ADMIN                    |
| FAILED_DELIVERY    | Re-programar entrega    | IN_TRANSIT        | DISPATCHER, ADMIN                    |
| PARTIALLY_SPLIT    | —                       | —                 | Las sub-órdenes siguen su propio ciclo FSM |
| CLOSED             | —                       | —                 | Estado terminal — inmutable          |
| CANCELLED          | —                       | —                 | Estado terminal — inmutable          |

---

## Implementación C#

### Constantes de dominio

```csharp
// Freiroute.Utility/Constants/OrdenConstants.cs
public static class OrdenEstado
{
    public const string Draft           = "DRAFT";
    public const string Confirmed       = "CONFIRMED";
    public const string Assigned        = "ASSIGNED";
    public const string PickupScheduled = "PICKUP_SCHEDULED";
    public const string InTransit       = "IN_TRANSIT";
    public const string Delivered       = "DELIVERED";
    public const string Invoiced        = "INVOICED";
    public const string Closed          = "CLOSED";
    public const string Cancelled       = "CANCELLED";
    public const string OnHold          = "ON_HOLD";
    public const string FailedDelivery  = "FAILED_DELIVERY";
    public const string PartiallySplit  = "PARTIALLY_SPLIT";

    public static readonly IReadOnlySet<string> Terminales =
        new HashSet<string>(StringComparer.Ordinal) { Closed, Cancelled };

    public static readonly IReadOnlySet<string> Editables =
        new HashSet<string>(StringComparer.Ordinal) { Draft, Confirmed };

    public static readonly IReadOnlySet<string> Desactivables =
        new HashSet<string>(StringComparer.Ordinal) { Draft };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Draft]           = "Borrador",
            [Confirmed]       = "Confirmada",
            [Assigned]        = "Asignada",
            [PickupScheduled] = "Recogida programada",
            [InTransit]       = "En tránsito",
            [Delivered]       = "Entregada",
            [Invoiced]        = "Facturada",
            [Closed]          = "Cerrada",
            [Cancelled]       = "Cancelada",
            [OnHold]          = "En espera",
            [FailedDelivery]  = "Entrega fallida",
            [PartiallySplit]  = "Dividida parcialmente",
        };

    public static string GetLabel(string estado) =>
        Labels.TryGetValue(estado, out var label) ? label : estado;
}
```

### Máquina de estados

```csharp
// Freiroute.Utility/Orders/OrderStateMachine.cs
public static class OrderStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        [OrdenEstado.Draft]           = [OrdenEstado.Confirmed, OrdenEstado.Cancelled],
        [OrdenEstado.Confirmed]       = [OrdenEstado.Assigned, OrdenEstado.OnHold,
                                         OrdenEstado.PartiallySplit, OrdenEstado.Cancelled],
        [OrdenEstado.Assigned]        = [OrdenEstado.PickupScheduled, OrdenEstado.OnHold,
                                         OrdenEstado.Cancelled],
        [OrdenEstado.PickupScheduled] = [OrdenEstado.InTransit, OrdenEstado.OnHold],
        [OrdenEstado.InTransit]       = [OrdenEstado.Delivered, OrdenEstado.FailedDelivery,
                                         OrdenEstado.OnHold],
        [OrdenEstado.Delivered]       = [OrdenEstado.Invoiced],
        [OrdenEstado.Invoiced]        = [OrdenEstado.Closed],
        [OrdenEstado.OnHold]          = [OrdenEstado.Confirmed],
        [OrdenEstado.FailedDelivery]  = [OrdenEstado.InTransit],
        [OrdenEstado.PartiallySplit]  = [],
        [OrdenEstado.Closed]          = [],
        [OrdenEstado.Cancelled]       = [],
    };

    public static bool CanTransition(string from, string to) =>
        ValidTransitions.TryGetValue(from, out var set) && set.Contains(to);

    public static IReadOnlySet<string> GetNextStates(string from) =>
        ValidTransitions.TryGetValue(from, out var set)
            ? set
            : (IReadOnlySet<string>)new HashSet<string>();

    /// <summary>
    /// Throws BusinessException(422) if the transition from → to is not allowed.
    /// </summary>
    public static void AssertTransition(string from, string to)
    {
        if (!CanTransition(from, to))
            throw new BusinessException(
                $"Transición de estado inválida: " +
                $"{OrdenEstado.GetLabel(from)} → {OrdenEstado.GetLabel(to)}",
                "ORDEN_TRANSICION_INVALIDA");
    }
}
```

### Tabla de auditoría de transiciones

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_historial_estados_orden.sql
CREATE TABLE historial_estados_orden (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id),
    orden_id        UUID NOT NULL REFERENCES ordenes(id),
    estado_anterior VARCHAR(30),
    estado_nuevo    VARCHAR(30) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID REFERENCES usuarios(id),
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE historial_estados_orden IS
    'Auditoría inmutable de todas las transiciones de estado de una orden.
     Cada cambio queda registrado con usuario, timestamp y motivo.';

CREATE INDEX idx_historial_orden_empresa ON historial_estados_orden(empresa_id);
CREATE INDEX idx_historial_orden_orden_id ON historial_estados_orden(orden_id);

ALTER TABLE historial_estados_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_historial_orden" ON historial_estados_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_historial_estados_orden_fecha_mod
    BEFORE UPDATE ON historial_estados_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

## Color semántico por estado (Design System Freiroute)

| Estado           | Variable / Hex           | Badge class        |
|------------------|--------------------------|--------------------|
| DRAFT            | `--fr-text-secondary`    | `badge-fr-neutral` |
| CONFIRMED        | `--fr-action-blue`       | `badge-fr-info`    |
| ASSIGNED         | `#0891B2`                | `badge-fr-info`    |
| PICKUP_SCHEDULED | `--fr-action-blue`       | `badge-fr-info`    |
| IN_TRANSIT       | `--fr-warning`           | `badge-fr-warning` |
| DELIVERED        | `--fr-success`           | `badge-fr-success` |
| INVOICED         | `--fr-success`           | `badge-fr-success` |
| CLOSED           | `--fr-text-secondary`    | `badge-fr-neutral` |
| CANCELLED        | `#374151`                | `badge-fr-neutral` |
| ON_HOLD          | `--fr-warning`           | `badge-fr-warning` |
| FAILED_DELIVERY  | `--fr-danger`            | `badge-fr-danger`  |
| PARTIALLY_SPLIT  | `#0891B2`                | `badge-fr-info`    |

---

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| Enum C# simple sin FSM | No previene transiciones inválidas en tiempo de ejecución |
| Workflow engine externo (Elsa, Stateless.NET) | Dependencia externa innecesaria para MVP; complejidad operativa sin beneficio proporcional |
| Estado en JSONB | No indexable, dificulta filtros y reportes SQL |
| FSM en el Controller API | Viola la regla de capas — la lógica de negocio pertenece a BLL/Utility |

---

## Consecuencias

**Positivas:**
- Trazabilidad completa — cada cambio de estado queda en `historial_estados_orden` con usuario y timestamp
- Imposible saltar estados inválidos — `AssertTransition` lanza `BusinessException(422)` antes de cualquier escritura en BD
- Testeable de forma aislada — `OrderStateMachine` es una clase estática pura sin dependencias externas
- Extensible — agregar estados futuros (ej: `CUSTOMS_HOLD`) no rompe la FSM ni requiere migraciones complejas
- `GetNextStates` permite que la UI muestre solo los botones de acción disponibles según el estado actual

**Negativas / trade-offs:**
- `INVOICED` y `CLOSED` dependen del módulo de facturación (Sprint 12); hasta entonces esas transiciones quedan implementadas pero sin UI en el tenant
- `PARTIALLY_SPLIT` requiere que la lógica de HU-026 cree sub-órdenes con su propio ciclo FSM independiente
- Los guards de rol (quién puede hacer cada transición) viven en `OrderService`, no en la FSM — la FSM solo valida la transición lógica

---

## Módulos afectados

| Módulo | Impacto |
|---|---|
| EP-04 Order Management (Sprint 4) | Implementación principal |
| EP-06 Shipment Planning (Sprint 7) | Transición CONFIRMED → ASSIGNED al consolidar en shipment |
| EP-08 Track & Trace (Sprint 10) | Transición IN_TRANSIT → DELIVERED via GPS/POD |
| EP-09 Document Management (Sprint 11) | Documentos adjuntos por estado |
| EP-10 Freight Audit & Payment (Sprint 12) | Transiciones DELIVERED → INVOICED → CLOSED |
| EP-19 Notificaciones (Sprint 25) | Trigger de notificación al cliente en cambios clave |

---

*ADR-019 — Freiroute TMS*
*Versión: 1.0 | Sprint 4 | 2026*
