using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Ubicacion;

/// <summary>
/// Datos para ajustar manualmente las coordenadas de una ubicación
/// (HU-015 CA-05) cuando la geocodificación automática es inexacta.
/// </summary>
[SwaggerSchema(Description = "Coordenadas de ajuste manual de una ubicación")]
public class ActualizarCoordenadasDto
{
    [SwaggerSchema(Description = "Latitud (-90 a 90)", Nullable = false)]
    public double Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud (-180 a 180)", Nullable = false)]
    public double Longitud { get; set; }
}