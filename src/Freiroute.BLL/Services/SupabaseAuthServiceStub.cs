using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Stub de Supabase Auth para Sprint 1.
///
/// TODO (Sprint 2 — @BackendDev): reemplazar por llamadas HTTP reales al REST
/// de Supabase Auth:
///   - POST {Supabase:Url}/auth/v1/token?grant_type=password   (SignInWithPasswordAsync)
///   - POST {Supabase:Url}/auth/v1/signup                       (SignUpAsync)
///   - PUT  {Supabase:Url}/auth/v1/user                          (UpdatePasswordAsync)
/// con headers apikey/Authorization (AnonKey / ServiceRoleKey).
///
/// Comportamiento temporal del stub:
///   - La contraseña "wrong-password" SIEMPRE falla (para demo del flujo
///     LOGIN_FAILED + bloqueo de cuenta HU-003 CA-04/06).
///   - Cualquier otra contraseña autentica (dev mode — NUNCA usar en prod real).
///   - SignUpAsync devuelve un UUID determinístico estable por email.
/// </summary>
public class SupabaseAuthServiceStub : ISupabaseAuthService
{
    private readonly ILogger<SupabaseAuthServiceStub> _logger;

    // Contraseña centinela para poder ejercitar el flujo de fallo en demo/CI.
    private const string WrongPassword = "wrong-password";

    public SupabaseAuthServiceStub(ILogger<SupabaseAuthServiceStub> logger)
    {
        _logger = logger;
    }

    /// <summary>Verifica credenciales (stub). TODO: llamada real a Supabase Auth en Sprint 2.</summary>
    public Task<SupabaseSignInResult> SignInWithPasswordAsync(string email, string password)
    {
        if (string.Equals(password, WrongPassword, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "SUPABASE AUTH STUB → credenciales inválidas para {Email}", email);
            return Task.FromResult(new SupabaseSignInResult(false));
        }

        _logger.LogWarning(
            "SUPABASE AUTH STUB → login simulado aceptado para {Email} (TODO: llamada real en Sprint 2)",
            email);

        var fakeUserId = DeterministicGuid(email);
        return Task.FromResult(new SupabaseSignInResult(true, fakeUserId));
    }

    /// <summary>Crea un usuario simulado y devuelve un supabase_user_id determinístico.</summary>
    public Task<Guid> SignUpAsync(string email, string password)
    {
        _logger.LogWarning(
            "SUPABASE AUTH STUB → signup simulado para {Email} (TODO: llamada real en Sprint 2)",
            email);

        return Task.FromResult(DeterministicGuid(email));
    }

    /// <summary>No-op simulado. TODO: PUT /auth/v1/user en Sprint 2.</summary>
    public Task UpdatePasswordAsync(Guid supabaseUserId, string newPassword)
    {
        _logger.LogWarning(
            "SUPABASE AUTH STUB → cambio de password simulado para SupabaseUserId {SupabaseUserId} (TODO: llamada real en Sprint 2)",
            supabaseUserId);

        return Task.CompletedTask;
    }

    private static Guid DeterministicGuid(string email)
    {
        return new Guid(MD5Hash(email));
    }

    private static byte[] MD5Hash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        return md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }
}