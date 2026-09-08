using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Filtros para la búsqueda paginada de órdenes (HU-021 CA-14).
/// Todos los campos son opcionales — solo se aplican los no nulos.
/// </summary>
[SwaggerSchema(Description = "Filtros para búsqueda de órdenes")]
public class OrdenFiltroDto
{
    [SwaggerSchema(Description = "Filtro de texto libre (número de orden, referencia del cliente)")]
    public string? Q { get; set; }

    [SwaggerSchema(Description = "Filtrar por ID del cliente")]
    public Guid? ClienteId { get; set; }

    [SwaggerSchema(Description = "Filtrar por estado de la orden (FSM)")]
    public string? Estado { get; set; }

    [SwaggerSchema(Description = "Filtrar por modo de transporte")]
    public string? ModoTransporte { get; set; }

    [SwaggerSchema(Description = "Filtrar por nivel de servicio")]
    public string? NivelServicio { get; set; }

    [SwaggerSchema(Description = "Filtrar por prioridad")]
    public string? Prioridad { get; set; }

    [SwaggerSchema(Description = "Filtrar por canal de creación")]
    public string? OrigenCreacion { get; set; }

    [SwaggerSchema(Description = "Filtrar por ID del shipment")]
    public Guid? ShipmentId { get; set; }

    [SwaggerSchema(Description = "Fecha de pickup desde (inclusive)")]
    public DateOnly? FechaPickupDesde { get; set; }

    [SwaggerSchema(Description = "Fecha de pickup hasta (inclusive)")]
    public DateOnly? FechaPickupHasta { get; set; }

    [SwaggerSchema(Description = "Fecha de entrega desde (inclusive)")]
    public DateOnly? FechaEntregaDesde { get; set; }

    [SwaggerSchema(Description = "Fecha de entrega hasta (inclusive)")]
    public DateOnly? FechaEntregaHasta { get; set; }

    [SwaggerSchema(Description = "Solo órdenes split")]
    public bool? EsSplit { get; set; }

    [SwaggerSchema(Description = "Número de página (default: 1)")]
    public int Page { get; set; } = 1;

    [SwaggerSchema(Description = "Tamaño de página (default: 20)")]
    public int PageSize { get; set; } = 20;
}
