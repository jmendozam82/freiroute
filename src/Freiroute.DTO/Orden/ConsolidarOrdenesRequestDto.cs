using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para consolidar múltiples órdenes en un solo shipment (HU-025).
/// Solo órdenes en estado CONFIRMED pueden consolidarse. Se requieren
/// mínimo 2 órdenes.
/// </summary>
[SwaggerSchema(Description = "Datos para consolidar órdenes en un shipment")]
public class ConsolidarOrdenesRequestDto
{
    [SwaggerSchema(Description = "IDs de las órdenes a consolidar (mínimo 2, todas deben estar en CONFIRMED)")]
    public List<Guid> OrdenIds { get; set; } = [];

    [SwaggerSchema(Description = "ID del shipment existente. Si es null se crea uno nuevo en estado PLANNED")]
    public Guid? ShipmentId { get; set; }
}
