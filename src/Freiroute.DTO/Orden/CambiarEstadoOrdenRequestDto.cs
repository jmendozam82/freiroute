using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para cambiar el estado de una orden de transporte (HU-024).
/// La transición se valida con OrderStateMachine (ADR-019).
/// La generación del numero_orden ocurre en DRAFT → CONFIRMED (ADR-020).
/// </summary>
[SwaggerSchema(Description = "Datos para cambiar el estado de una orden")]
public class CambiarEstadoOrdenRequestDto
{
    [SwaggerSchema(Description = "Estado destino deseado (debe ser válido según FSM)")]
    public string EstadoNuevo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Motivo del cambio de estado (requerido en cancelaciones)")]
    public string? Motivo { get; set; }

    [SwaggerSchema(Description = "ID del shipment para asignar (requerido en ASSIGNED)")]
    public Guid? ShipmentId { get; set; }
}
