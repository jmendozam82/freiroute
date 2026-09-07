using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Zona;

/// <summary>
/// Datos de salida de una zona de entrega (HU-016).
/// TotalUbicaciones se calcula en la BLL (conteo de ubicaciones asignadas).
/// </summary>
[SwaggerSchema(Description = "Respuesta de una zona de entrega")]
public class ZonaResponseDto
{
    [SwaggerSchema(Description = "ID único de la zona")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la zona")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string Codigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción de la zona")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Color en formato #RRGGBB para el mapa")]
    public string ColorHex { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Método de definición: POLIGONO, CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES")]
    public string TipoDefinicion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "GeoJSON Polygon o MultiPolygon del área de la zona")]
    public string? PoligonoGeoJson { get; set; }

    [SwaggerSchema(Description = "Lista de códigos postales incluidos")]
    public string[] CodigosPostales { get; set; } = [];

    [SwaggerSchema(Description = "Lista de ciudades incluidas")]
    public string[] Ciudades { get; set; } = [];

    [SwaggerSchema(Description = "Lista de departamentos incluidos")]
    public string[] Departamentos { get; set; } = [];

    [SwaggerSchema(Description = "Lista de países incluidos")]
    public string[] Paises { get; set; } = [];

    [SwaggerSchema(Description = "Número de ubicaciones asignadas a la zona (calculado)")]
    public int TotalUbicaciones { get; set; }

    [SwaggerSchema(Description = "Si la zona está activa (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}