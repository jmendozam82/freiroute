namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de salida del login/refresh. El JWT de acceso es válido por 8 horas
/// y el refresh token por 30 días (expiresIn en segundos = 28800).
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 28800;
    public UsuarioTokenDto Usuario { get; set; } = new();
}

/// <summary>
/// Información del usuario autenticado embebida en la respuesta de login.
/// Nunca expone datos sensibles (supabase_user_id, intentos_fallidos, etc.).
/// </summary>
public class UsuarioTokenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TipoUsuario { get; set; } = string.Empty;
    public string EmpresaNombre { get; set; } = string.Empty;

    /// <summary>Permisos en formato "modulo:accion", ej: ["embarques:read", "embarques:create"].</summary>
    public List<string> Permisos { get; set; } = [];
}