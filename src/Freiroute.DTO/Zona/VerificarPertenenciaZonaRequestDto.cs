using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Zona;

/// <summary>
/// Punto geográfico para verificar en qué zonas cae (HU-016 CA-05).
/// La BLL evalúa point-in-polygon para zonas tipo POLIGONO y
/// coincidencia de listas para los demás métodos de definición.
/// </summary>
[SwaggerSchema(Description = "Punto geográfico para verificar pertenencia a zonas")]
public class VerificarPertenenciaZonaRequestDto
{
    [SwaggerSchema(Description = "Latitud (-90 a 90)", Nullable = false)]
    public double Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud (-180 a 180)", Nullable = false)]
    public double Longitud { get; set; }
}