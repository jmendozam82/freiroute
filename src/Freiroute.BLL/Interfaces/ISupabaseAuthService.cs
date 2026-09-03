namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Resultado de la verificación de credenciales contra Supabase Auth.
/// </summary>
public record SupabaseSignInResult(bool Success, Guid? SupabaseUserId = null);

/// <summary>
/// Contrato de integración con Supabase Auth (HU-003, HU-004, HU-007 CA-06).
/// Supabase Auth es el almacén de contraseñas del sistema (la tabla 'usuarios'
/// guarda el perfil de negocio y el vínculo supabase_user_id — no la contraseña).
/// En Sprint 1 se usa SupabaseAuthServiceStub (ver TODO — la llamada HTTP real
/// al REST de Supabase Auth va en Sprint 2).
/// </summary>
public interface ISupabaseAuthService
{
    /// <summary>
    /// Verifica email+contraseña contra Supabase Auth (grant_type=password).
    /// Devuelve Success=true con el supabase_user_id si las credenciales son válidas.
    /// </summary>
    Task<SupabaseSignInResult> SignInWithPasswordAsync(string email, string password);

    /// <summary>Crea un usuario en Supabase Auth y devuelve su supabase_user_id (invitación/activación).</summary>
    Task<Guid> SignUpAsync(string email, string password);

    /// <summary>Cambia la contraseña de un usuario en Supabase Auth (HU-007 CA-06).</summary>
    Task UpdatePasswordAsync(Guid supabaseUserId, string newPassword);
}