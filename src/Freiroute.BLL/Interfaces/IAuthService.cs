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

    /// <summary>
    /// Prepara el alta de 2FA TOTP (HU-005 CA-01): genera el secret, el QR y 8
    /// códigos de recuperación. Persiste un registro pendiente (TotpHabilitado=false)
    /// con el secret cifrado AES-256 para poder verificar el primer código al activar.
    /// </summary>
    Task<Setup2faResponseDto> Setup2faAsync(Guid usuarioId, Guid empresaId);

    /// <summary>
    /// Activa el 2FA tras verificar el primer código TOTP contra el secret pendiente
    /// (HU-005 CA-01). Marca TotpHabilitado=true y persiste los hashes de los códigos
    /// de recuperación (solo se muestran una vez durante el setup).
    /// </summary>
    Task<bool> Activar2faAsync(Activar2faRequestDto dto, Guid usuarioId, Guid empresaId);

    /// <summary>
    /// Desactiva el 2FA de un usuario (HU-005 CA-06). Requiere la verificación del
    /// código actual (TOTP o email) antes de desactivar por seguridad.
    /// </summary>
    Task<bool> Desactivar2faAsync(Guid usuarioId, Guid empresaId, string codigoActual);

    /// <summary>
    /// Segundo paso del login con 2FA (HU-005): valida el temp token de corta vida
    /// emitido en el paso 1 y verifica el código TOTP (o un código de recuperación de
    /// un solo uso). Si es válido, emite el access + refresh token completo.
    /// </summary>
    Task<LoginResponseDto> Verificar2faAsync(Verificar2faRequestDto request);

    /// <summary>
    /// Obtiene los códigos de recuperación de 2FA. SIEMPRE lanza BusinessException
    /// porque los hashes en BD no son reversibles. Los códigos solo se muestran una
    /// vez durante el setup de 2FA.
    /// </summary>
    Task GetRecoveryCodesAsync(Guid usuarioId, Guid empresaId);

    /// <summary>
    /// Regenera los 8 códigos de recuperación de 2FA (HU-005 CA-04).
    /// Retorna los códigos en claro — solo se muestran una vez.
    /// </summary>
    Task<List<string>> RegenerarRecoveryCodesAsync(Guid usuarioId, Guid empresaId);

    /// <summary>
    /// Login con OAuth (HU-004): resuelve el usuario por el token devuelto por
    /// Supabase Auth y emite el JWT interno. Implementación base en este sprint.
    /// </summary>
    Task<LoginResponseDto> LoginConOAuthAsync(OAuthCallbackRequestDto request);
}