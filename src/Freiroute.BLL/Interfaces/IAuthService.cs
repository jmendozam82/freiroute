using Freiroute.DTO.Auth;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de autenticación (HU-003, HU-007). Delega en Supabase Auth para
/// verificar credenciales y genera el JWT interno Freiroute con los claims:
/// user_id, empresa_id, perfil_id, tipo_usuario, permisos[], nombre (ADR-007).
/// Access token: 8 h — Refresh token: 30 días.
/// </summary>
public interface IAuthService
{
    /// <summary>Inicia sesión con email y contraseña. Registra LOGIN o LOGIN_FAILED en auditoría.</summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>Renueva el access token con el refresh token.</summary>
    Task<LoginResponseDto> RefreshAsync(RefreshTokenRequestDto request);

    /// <summary>Cierra la sesión invalidando el refresh token. Registra LOGOUT en auditoría.</summary>
    Task LogoutAsync(string refreshToken);

    /// <summary>Solicita recuperación de contraseña. Respuesta genérica (HU-007 CA-03).</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request);

    /// <summary>Restablece la contraseña con el token de un solo uso (30 min). Invalida las sesiones activas.</summary>
    Task ResetPasswordAsync(ResetPasswordRequestDto request);
}