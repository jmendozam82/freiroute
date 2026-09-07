using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Ubicacion;

/// <summary>
/// Payload mínimo para el mapa (Leaflet + CartoDB, ADR-014).
/// Sin datos sensibles de contacto.
/// </summary>
[SwaggerSchema(Description = "Payload mínimo de ubicación para el mapa (sin datos sensibles)")]
public class UbicacionMapaDto
{
    [SwaggerSchema(Description = "ID único de la ubicación")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la ubicación")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de la ubicación (define el marker)")]
    public string Tipo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Latitud (-90 a 90)")]
    public double? Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud (-180 a 180)")]
    public double? Longitud { get; set; }
}