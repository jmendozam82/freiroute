using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Ítem del historial de estados de un reclamo (HU-032 CA-07).
/// Enriquecido con el nombre del usuario que ejecutó la transición.
/// </summary>
[SwaggerSchema(Description = "Ítem del historial de estados de un reclamo")]
public class HistorialEstadoReclamoDto
{
    [SwaggerSchema(Description = "Estado anterior (NULL en inserción inicial)")]
    public string? EstadoAnterior { get; set; }

    [SwaggerSchema(Description = "Estado nuevo")]
    public string EstadoNuevo { get; set; } = null!;

    [SwaggerSchema(Description = "Motivo de la transición")]
    public string? Motivo { get; set; }

    [SwaggerSchema(Description = "Nombre del usuario que ejecutó la transición (calculado)")]
    public string? UsuarioNombre { get; set; }

    [SwaggerSchema(Description = "Fecha de la transición")]
    public DateTime FechaCreacion { get; set; }
}