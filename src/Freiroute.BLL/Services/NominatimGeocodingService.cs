using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Geo;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Geocodificación y reverse geocoding vía Nominatim (OpenStreetMap)
/// (ADR-014). Política de uso de OSM: máximo 1 request/segundo y
/// User-Agent identificable. Fail-soft: nunca lanza — retorna null.
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Convierte una dirección a coordenadas. Retorna null si no se pudo
    /// geocodificar o el proveedor no responde (no lanza).
    /// </summary>
    public async Task<GeocodingResultDto?> GeocodeAsync(
        string direccion, string? ciudad = null, string? pais = null)
    {
        if (string.IsNullOrWhiteSpace(direccion))
        {
            return null;
        }

        try
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var q = string.Join(", ", new[] { direccion.Trim(), ciudad, pais }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));

                var url = $"/search?format=jsonv2&limit=1&q={Uri.EscapeDataString(q)}";
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Nominatim búsqueda falló con status {StatusCode} para '{Direccion}'",
                        response.StatusCode, direccion);
                    return null;
                }

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.ValueKind != JsonValueKind.Array ||
                    json.RootElement.GetArrayLength() == 0)
                {
                    return null;
                }

                var first = json.RootElement[0];
                if (!first.TryGetProperty("lat", out var latProp) ||
                    !first.TryGetProperty("lon", out var lonProp) ||
                    !double.TryParse(latProp.GetString(), out var lat) ||
                    !double.TryParse(lonProp.GetString(), out var lon))
                {
                    return null;
                }

                var displayName = first.TryGetProperty("display_name", out var displayProp)
                    ? displayProp.GetString()
                    : null;

                // importance (0-1) de Nominatim, si está disponible; fallback 0.5.
                var confianza = 0.5;
                if (first.TryGetProperty("importance", out var impProp) &&
                    impProp.TryGetDouble(out var imp))
                {
                    confianza = Math.Clamp(imp, 0.0, 1.0);
                }

                return new GeocodingResultDto
                {
                    Latitud = lat,
                    Longitud = lon,
                    DireccionNormalizada = displayName ?? direccion,
                    Confianza = confianza,
                    Proveedor = "nominatim"
                };
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or
                                   TaskCanceledException or
                                   JsonException or
                                   FormatException)
        {
            _logger.LogWarning(ex, "Geocodificación falló para '{Direccion}'", direccion);
            return null;
        }
    }

    /// <summary>
    /// Convierte coordenadas a una dirección legible. Retorna null
    /// si no se pudo resolver (no lanza).
    /// </summary>
    public async Task<string?> ReverseGeocodeAsync(double lat, double lng)
    {
        try
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var url = $"/reverse?format=jsonv2&lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Nominatim reverse falló con status {StatusCode} para ({Lat}, {Lng})",
                        response.StatusCode, lat, lng);
                    return null;
                }

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("display_name", out var displayProp))
                {
                    return displayProp.GetString();
                }

                return null;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or
                                   TaskCanceledException or
                                   JsonException)
        {
            _logger.LogWarning(ex, "Reverse geocoding falló para ({Lat}, {Lng})", lat, lng);
            return null;
        }
    }
}