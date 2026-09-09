using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Respuesta del registro de un rechazo de entrega (HU-030).
/// </summary>
[SwaggerSchema(Description = "Respuesta de un rechazo de entrega registrado")]
public class RechazoEntregaResponseDto
{
    [SwaggerSchema(Description = "ID del registro de rechazo")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "ID de la orden rechazada")]
    public Guid OrdenId { get; set; }

    [SwaggerSchema(Description = "Motivo del rechazo (TipoRechazo)")]
    public string Motivo { get; set; } = null!;

    [SwaggerSchema(Description = "Detalle libre del rechazo")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Nombre del usuario que registró el rechazo (calculado)")]
    public string? UsuarioNombre { get; set; }

    [SwaggerSchema(Description = "Fecha de registro del rechazo")]
    public DateTime FechaCreacion { get; set; }
}