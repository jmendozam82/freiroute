using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Implementación real de la integración con Supabase Auth vía REST (G-06).
/// Usa el ServiceRoleKey para el cambio de contraseña con token de admin
/// (PATCH /auth/v1/admin/users/{id}) y el endpoint público de token para el
/// login (POST /auth/v1/token). El DI la registra cuando Supabase:UseRealAuth=true;
/// por defecto se usa SupabaseAuthServiceStub (dev/CI).
/// </summary>
public class SupabaseAuthServiceReal : ISupabaseAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseAuthServiceReal> _logger;

    private string SupabaseUrl => _configuration["Supabase:Url"]
        ?? throw new InvalidOperationException("Falta la configuración 'Supabase:Url'.");

    private string AnonKey => _configuration["Supabase:AnonKey"]
        ?? throw new InvalidOperationException("Falta la configuración 'Supabase:AnonKey'.");

    private string ServiceRoleKey => _configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Falta la configuración 'Supabase:ServiceRoleKey'.");

    public SupabaseAuthServiceReal(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SupabaseAuthServiceReal> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// POST {Url}/auth/v1/token?grant_type=password — grant_type password con
    /// email+contraseña. 200 OK → el usuario existe y las credenciales son válidas.
    /// 400 invalid_grant → credenciales inválidas.
    /// </summary>
    public async Task<SupabaseSignInResult> SignInWithPasswordAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{SupabaseUrl}/auth/v1/token?grant_type=password");

        request.Headers.Add("apikey", AnonKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { email, password }),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Supabase Auth → login fallido {Status} para {Email}",
                    (int)response.StatusCode, email);
                return new SupabaseSignInResult(false);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var userId = doc.RootElement.TryGetProperty("id", out var idProp)
                ? Guid.Parse(idProp.GetString()!)
                : Guid.Empty;

            return new SupabaseSignInResult(true, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase Auth → error HTTP en login para {Email}", email);
            return new SupabaseSignInResult(false);
        }
    }

    /// <summary>POST {Url}/auth/v1/signup — crea el usuario en Supabase Auth y devuelve su UUID.</summary>
    public async Task<Guid> SignUpAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseUrl}/auth/v1/signup");
        request.Headers.Add("apikey", AnonKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { email, password }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return Guid.Parse(doc.RootElement.GetProperty("id").GetString()!);
    }

    /// <summary>
    /// PUT {Url}/auth/v1/user — cambio de contraseña con el token del propio usuario
    /// (flujo "conozco mi contraseña" / sesión del usuario).
    /// </summary>
    public async Task UpdatePasswordAsync(Guid supabaseUserId, string newPassword)
    {
        // El token del usuario no está disponible en este servicio (lo tiene AuthService
        // en la sesión). Para no romper el contrato, reutilizamos el flujo admin
        // (CambiarPasswordAsync) que funciona con el ServiceRoleKey.
        var ok = await CambiarPasswordAsync(supabaseUserId, newPassword);
        if (!ok)
        {
            throw new InvalidOperationException(
                "Supabase Auth no pudo actualizar la contraseña (flujo admin).");
        }
    }

    /// <summary>
    /// PATCH {Url}/auth/v1/admin/users/{id} — cambio de contraseña con token de admin
    /// (G-06-D). Headers: Authorization Bearer ServiceRoleKey + apikey ServiceRoleKey.
    /// 200 OK → contraseña actualizada; cualquier otro status → false.
    /// </summary>
    public async Task<bool> CambiarPasswordAsync(Guid supabaseUserId, string nuevaPassword)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{SupabaseUrl}/auth/v1/admin/users/{supabaseUserId}");

        request.Headers.Add("apikey", ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {ServiceRoleKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { password = nuevaPassword }),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Supabase Auth → cambio de password ADMIN fallido {Status} para {UserId}",
                    (int)response.StatusCode, supabaseUserId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase Auth → error HTTP en cambio de password ADMIN para {UserId}", supabaseUserId);
            return false;
        }
    }

    /// <summary>DTO interno de la respuesta del endpoint admin/users (G-06).</summary>
    private sealed class SupabaseAdminUserDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}