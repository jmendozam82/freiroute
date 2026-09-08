using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Registro de una transición de estado en el historial de una orden (HU-024).
/// Consultable via GET /api/ordenes/{id}/historial.
/// </summary>
[SwaggerSchema(Description = "Registro de una transición de estado de una orden")]
public class HistorialEstadoOrdenDto
{
    [SwaggerSchema(Description = "ID único del registro de historial")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "ID de la orden")]
    public Guid OrdenId { get; set; }

    [SwaggerSchema(Description = "Estado anterior (NULL en inserción inicial)")]
    public string? EstadoAnterior { get; set; }

    [SwaggerSchema(Description = "Etiqueta legible del estado anterior")]
    public string? EstadoAnteriorLabel { get; set; }

    [SwaggerSchema(Description = "Estado nuevo")]
    public string EstadoNuevo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado nuevo")]
    public string EstadoNuevoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Motivo del cambio (opcional)")]
    public string? Motivo { get; set; }

    [SwaggerSchema(Description = "Nombre del usuario que ejecutó la transición")]
    public string? UsuarioNombre { get; set; }

    [SwaggerSchema(Description = "Fecha y hora de la transición")]
    public DateTime FechaCreacion { get; set; }
}
