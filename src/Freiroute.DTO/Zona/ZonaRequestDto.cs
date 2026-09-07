using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Zona;

/// <summary>
/// Datos de entrada para crear o actualizar una zona de entrega (HU-016).
/// La zona se define por al menos un método: polígono GeoJSON,
/// códigos postales, ciudades, departamentos o países.
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una zona de entrega")]
public class ZonaRequestDto
{
    [SwaggerSchema(Description = "Nombre de la zona", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa (ej: Z-MGA-N)")]
    public string Codigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción de la zona")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Color en formato #RRGGBB para el mapa")]
    public string ColorHex { get; set; } = "#1A73E8";

    [SwaggerSchema(Description = "Método de definición: POLIGONO, CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES")]
    public string TipoDefinicion { get; set; } = "POLIGONO";

    [SwaggerSchema(Description = "GeoJSON Polygon o MultiPolygon del área de la zona")]
    public string? PoligonoGeoJson { get; set; }

    [SwaggerSchema(Description = "Lista de códigos postales incluidos")]
    public string[] CodigosPostales { get; set; } = [];

    [SwaggerSchema(Description = "Lista de ciudades incluidas")]
    public string[] Ciudades { get; set; } = [];

    [SwaggerSchema(Description = "Lista de departamentos incluidos")]
    public string[] Departamentos { get; set; } = [];

    [SwaggerSchema(Description = "Lista de países incluidos")]
    public string[] Paises { get; set; } = ["Nicaragua"];
}