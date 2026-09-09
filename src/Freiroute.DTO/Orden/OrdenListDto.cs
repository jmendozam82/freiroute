using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Resumen de una orden para listados paginados (HU-021 CA-14).
/// Contiene solo los campos necesarios para la tabla de órdenes.
/// Paginado con PagedResult (20 registros/página — RNF-01.4).
/// </summary>
[SwaggerSchema(Description = "Resumen de una orden para listados paginados")]
public class OrdenListDto
{
    [SwaggerSchema(Description = "ID único de la orden")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Número legible de la orden (NULL en DRAFT)")]
    public string? NumeroOrden { get; set; }

    [SwaggerSchema(Description = "Nombre del cliente (shipper)")]
    public string ClienteNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre de la ubicación de origen")]
    public string OrigenNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre de la ubicación de destino")]
    public string DestinoNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Estado actual de la orden")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado")]
    public string EstadoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "CSS class del badge de estado")]
    public string EstadoBadgeClass { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Modo de transporte")]
    public string ModoTransporte { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Prioridad de la orden")]
    public string Prioridad { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible de la prioridad")]
    public string PrioridadLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Fecha de recogida solicitada")]
    public DateOnly? FechaPickupSolicitada { get; set; }

    [SwaggerSchema(Description = "Fecha de entrega requerida")]
    public DateOnly? FechaEntregaRequerida { get; set; }

    [SwaggerSchema(Description = "Peso total en kilogramos")]
    public decimal PesoKg { get; set; }

    [SwaggerSchema(Description = "Canal de creación: MANUAL, CSV, API, RECURRENTE")]
    public string OrigenCreacion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Referencia del cliente")]
    public string? ReferenciaCliente { get; set; }

    [SwaggerSchema(Description = "Número de Purchase Order del cliente (HU-028)")]
    public string? NumeroPo { get; set; }

    [SwaggerSchema(Description = "Estado calculado de SLA (HU-031): OK | EN_RIESGO | CRITICO | VENCIDO")]
    public string SlaStatus { get; set; } = Freiroute.Utility.Constants.SlaStatus.Ok;

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}
