using System.Net.Http.Headers;
using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Cliente de Supabase Storage (HU-014, ADR-012). Implementa el contrato
/// <see cref="IStorageService"/> usando el REST de Supabase vía HttpClient:
///
///   - Subida:   POST /storage/v1/object/{bucket}/{path}  (Bearer SERVICE_ROLE_KEY)
///   - Firmado:  POST /storage/v1/object/sign/{bucket}/{path}?expiresIn=86400
///   - Borrado:  DELETE /storage/v1/object/{bucket}/{path}
///
/// Los buckets son PRIVADOS (AGENTS.md #32): se usa la SERVICE_ROLE_KEY para
/// subir/borrar y las descargas se hacen mediante signed URLs temporales (24 h),
/// nunca exponiendo la URL pública del objeto.
/// </summary>
public class SupabaseStorageService : IStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _serviceRoleKey;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = (configuration["Supabase:Url"] ?? "http://localhost:54321").TrimEnd('/');
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"] ?? string.Empty;
        _logger = logger;
    }

    /// <summary>
    /// Sube un archivo a un bucket y devuelve el path almacenado.
    /// El nombre se compone con un UUID para evitar colisiones (AGENTS.md #32/33).
    /// </summary>
    public async Task<string> UploadAsync(string bucket, string path, string fileName,
        Stream stream, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var objectPath = $"{path?.TrimEnd('/')}/{Guid.NewGuid():N}{extension}";
        var url = $"{_baseUrl}/storage/v1/object/{bucket}/{objectPath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StreamContent(stream)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.Add("x-upsert", "false");

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Supabase Storage subida fallida: {Status} — {Body}",
                response.StatusCode, body);
            throw new BusinessException("No se pudo subir el archivo al almacenamiento.");
        }

        return objectPath;
    }

    /// <summary>
    /// Genera una signed URL temporal para un objeto privado (AGENTS.md #32).
    /// </summary>
    public async Task<string?> GetSignedUrlAsync(string bucket, string path,
        int expiresInSeconds = 86400)
    {
        var url = $"{_baseUrl}/storage/v1/object/sign/{bucket}/{path}?expiresIn={expiresInSeconds}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("No se pudo firmar URL para {Path}: {Status} — {Body}",
                path, response.StatusCode, body);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var signedPath = doc.RootElement.GetProperty("signedURL").GetString();
            return string.IsNullOrWhiteSpace(signedPath)
                ? null
                : $"{_baseUrl}{signedPath}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Respuesta inesperada al firmar URL {Path}", path);
            return null;
        }
    }

    /// <summary>Elimina un objeto del bucket.</summary>
    public async Task<bool> DeleteAsync(string bucket, string path)
    {
        var url = $"{_baseUrl}/storage/v1/object/{bucket}/{path}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase Storage borrado fallido: {Status} — {Path}",
                response.StatusCode, path);
            return false;
        }

        return true;
    }
}
