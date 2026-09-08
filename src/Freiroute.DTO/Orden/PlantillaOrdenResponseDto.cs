using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Respuesta de una plantilla de orden (HU-027).
/// </summary>
[SwaggerSchema(Description = "Respuesta de una plantilla de orden")]
public class PlantillaOrdenResponseDto
{
    [SwaggerSchema(Description = "ID único de la plantilla")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre descriptivo de la plantilla")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción de la plantilla")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Snapshot JSON de los campos de la orden")]
    public string DatosOrden { get; set; } = "{}";

    [SwaggerSchema(Description = "true si tiene recurrencia activa")]
    public bool EsRecurrente { get; set; }

    [SwaggerSchema(Description = "Frecuencia de recurrencia")]
    public string? FrecuenciaRecurrencia { get; set; }

    [SwaggerSchema(Description = "Etiqueta legible de la frecuencia")]
    public string? FrecuenciaRecurrenciaLabel { get; set; }

    [SwaggerSchema(Description = "Fecha de la próxima ejecución automática")]
    public DateOnly? ProximaEjecucion { get; set; }

    [SwaggerSchema(Description = "Nombre del usuario que creó la plantilla")]
    public string? CreadoPorNombre { get; set; }

    [SwaggerSchema(Description = "Si la plantilla está activa")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}
