using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para crear o actualizar una plantilla de orden (HU-027).
/// El datos_orden es un snapshot JSON de los campos de OrdenRequestDto.
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una plantilla de orden")]
public class PlantillaOrdenRequestDto
{
    [SwaggerSchema(Description = "Nombre descriptivo de la plantilla", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción de la plantilla")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Snapshot JSON de los campos de OrdenRequestDto")]
    public string DatosOrden { get; set; } = "{}";
}
