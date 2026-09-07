using Freiroute.DTO.Geo;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato del servicio de geocodificación (ADR-014).
/// Implementado por NominatimGeocodingService (OpenStreetMap).
/// La interfaz permite cambiar de proveedor (Google, HERE, MapBox)
/// sin tocar la BLL ni las vistas. Vive en BLL, no en DAL: es un
/// servicio de integración externa, no de persistencia.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Convierte una dirección a coordenadas (lat/lng) usando el proveedor
    /// configurado. Retorna null si no se pudo geocodificar (no lanza).
    /// </summary>
    Task<GeocodingResultDto?> GeocodeAsync(
        string direccion,
        string? ciudad = null,
        string? pais = null);

    /// <summary>
    /// Convierte coordenadas a una dirección legible (reverse geocoding).
    /// Retorna null si no se pudo resolver.
    /// </summary>
    Task<string?> ReverseGeocodeAsync(double lat, double lng);
}