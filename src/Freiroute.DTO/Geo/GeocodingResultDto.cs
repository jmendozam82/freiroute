using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Geo;

/// <summary>
/// Resultado de geocodificación de una dirección (ADR-014).
/// Devuelto por IGeocodingService — usado al registrar ubicaciones.
/// </summary>
[SwaggerSchema(Description = "Resultado de geocodificación de una dirección")]
public class GeocodingResultDto
{
    [SwaggerSchema(Description = "Latitud geográfica (-90 a 90)")]
    public double Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud geográfica (-180 a 180)")]
    public double Longitud { get; set; }

    [SwaggerSchema(Description = "Dirección normalizada por el proveedor")]
    public string DireccionNormalizada { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nivel de confianza 0.0-1.0")]
    public double Confianza { get; set; }

    [SwaggerSchema(Description = "Proveedor usado: nominatim, google, here")]
    public string Proveedor { get; set; } = "nominatim";
}