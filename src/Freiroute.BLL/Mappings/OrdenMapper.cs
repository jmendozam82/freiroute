using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Orders;

namespace Freiroute.BLL.Mappings;

/// <summary>
/// Mapeo entidad → DTO para listados de órdenes.
/// Centraliza la transformación para que los servicios de Sprint 5
/// (PO/SO, prioridades, SLA, re-entregas) no dupliquen lógica.
/// EstadoBadgeClass se deja vacío — lo resuelve el frontend desde la
/// FSM (ADR-019) con fr-badge-* (Design System Freiroute).
/// Vive en BLL (no en Utility) por ADR-016: Utility no referencia DTO/Entity.
/// </summary>
public static class OrdenMapper
{
    public static OrdenListDto ToListDto(Orden o) => new()
    {
        Id = o.Id,
        NumeroOrden = o.NumeroOrden,
        ClienteNombre = o.ClienteNombre ?? string.Empty,
        OrigenNombre = o.OrigenNombre ?? string.Empty,
        DestinoNombre = o.DestinoNombre ?? string.Empty,
        Estado = o.Estado,
        EstadoLabel = OrdenEstado.GetLabel(o.Estado),
        ModoTransporte = o.ModoTransporte,
        Prioridad = o.Prioridad,
        PrioridadLabel = OrdenPrioridad.GetLabel(o.Prioridad),
        FechaPickupSolicitada = o.FechaPickupSolicitada,
        FechaEntregaRequerida = o.FechaEntregaRequerida,
        PesoKg = o.PesoKg,
        OrigenCreacion = o.OrigenCreacion,
        ReferenciaCliente = o.ReferenciaCliente,
        NumeroPo = o.NumeroPo,
        SlaStatus = SlaCalculator.Calcular(
            o.FechaEntregaRequerida?.ToDateTime(TimeOnly.MinValue), o.Estado),
        FechaCreacion = o.FechaCreacion
    };
}