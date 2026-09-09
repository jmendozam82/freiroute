using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Builders;

/// <summary>
/// Builder para OrdenListDto (resumen paginado de órdenes — HU-021 CA-14).
/// Útil en tests de mappers de PO (HU-028), prioridades (HU-029) y SLA (HU-031).
/// </summary>
public class OrdenListItemBuilder
{
    private Guid _id = Guid.NewGuid();
    private string? _numeroOrden = "ORD-2026-00001";
    private string _clienteNombre = "Distribuidora ABC S.A.";
    private string _origenNombre = "Almacen Managua";
    private string _destinoNombre = "Bodega León";
    private string _estado = OrdenEstado.Confirmed;
    private string _prioridad = OrdenPrioridad.Normal;
    private DateOnly? _fechaEntregaRequerida = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
    private decimal _pesoKg = 1200m;
    private string? _numeroPo;
    private string _slaStatus = SlaStatus.Ok;

    public OrdenListItemBuilder ConId(Guid id) { _id = id; return this; }
    public OrdenListItemBuilder ConNumeroOrden(string? numero) { _numeroOrden = numero; return this; }
    public OrdenListItemBuilder ConEstado(string estado) { _estado = estado; return this; }
    public OrdenListItemBuilder ConPrioridad(string prioridad) { _prioridad = prioridad; return this; }
    public OrdenListItemBuilder ConFechaEntrega(DateOnly? fecha) { _fechaEntregaRequerida = fecha; return this; }
    public OrdenListItemBuilder ConNumeroPo(string? po) { _numeroPo = po; return this; }
    public OrdenListItemBuilder ConSlaStatus(string sla) { _slaStatus = sla; return this; }
    public OrdenListItemBuilder ConCliente(string nombre) { _clienteNombre = nombre; return this; }

    public OrdenListDto Build() => new()
    {
        Id = _id,
        NumeroOrden = _numeroOrden,
        ClienteNombre = _clienteNombre,
        OrigenNombre = _origenNombre,
        DestinoNombre = _destinoNombre,
        Estado = _estado,
        EstadoLabel = OrdenEstado.GetLabel(_estado),
        EstadoBadgeClass = "fr-badge-info",
        ModoTransporte = ModoTransporte.Terrestre,
        Prioridad = _prioridad,
        PrioridadLabel = OrdenPrioridad.GetLabel(_prioridad),
        FechaPickupSolicitada = null,
        FechaEntregaRequerida = _fechaEntregaRequerida,
        PesoKg = _pesoKg,
        OrigenCreacion = OrigenCreacion.Manual,
        ReferenciaCliente = null,
        NumeroPo = _numeroPo,
        SlaStatus = _slaStatus,
        FechaCreacion = DateTime.UtcNow
    };
}