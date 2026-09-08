using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Resumen de un shipment para respuestas de consolidación (HU-025).
/// </summary>
[SwaggerSchema(Description = "Resumen de un shipment con sus órdenes consolidadas")]
public class ShipmentResumenDto
{
    [SwaggerSchema(Description = "ID único del shipment")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Número del shipment")]
    public string? NumeroShipment { get; set; }

    [SwaggerSchema(Description = "Estado del shipment")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado")]
    public string EstadoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Cantidad de órdenes consolidadas")]
    public int CantidadOrdenes { get; set; }

    [SwaggerSchema(Description = "Peso total consolidado en kilogramos")]
    public decimal PesoTotalKg { get; set; }

    [SwaggerSchema(Description = "Volumen total consolidado en metros cúbicos")]
    public decimal VolumenTotalM3 { get; set; }

    [SwaggerSchema(Description = "Lista de IDs de las órdenes consolidadas")]
    public List<Guid> OrdenIds { get; set; } = [];

    [SwaggerSchema(Description = "Advertencias (ej: órdenes con distintos modos de transporte)")]
    public List<string> Advertencias { get; set; } = [];

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}
