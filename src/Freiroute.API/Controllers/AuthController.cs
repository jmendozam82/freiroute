using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Utility.ApiResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de autenticación (HU-003, HU-007).
/// login/refresh/forgot-password/reset-password son públicos (AllowAnonymous).
/// logout requiere sesión activa ([Authorize]).
/// Todas las respuestas usan el wrapper ApiResponse&lt;T&gt; (ADR-008).
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Inicia sesión con email y contraseña (HU-003). Devuelve access + refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Inicio de sesión exitoso"));
    }

    /// <summary>Renueva el access token con el refresh token (rotación del refresh, HU-003 CA-02).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token renovado"));
    }

    /// <summary>Cierra la sesión invalidando el refresh token (HU-003). Idempotente.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Sesión cerrada"));
    }

    /// <summary>Solicita la recuperación de contraseña (HU-007). Respuesta SIEMPRE genérica (CA-03).</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<string>.Ok(string.Empty,
            "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña."));
    }

    /// <summary>Restablece la contraseña con el token de un solo uso (HU-007 CA-04/05).</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Contraseña actualizada"));
    }

    /// <summary>Prepara el alta de 2FA TOTP (HU-005 CA-01). Devuelve secret + códigos UNA vez.</summary>
    [Authorize]
    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(ApiResponse<Setup2faResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Setup2faResponseDto>>> Setup2fa()
    {
        var resultado = await _authService.Setup2faAsync(User.GetUsuarioId(), User.GetEmpresaId());
        return Ok(ApiResponse<Setup2faResponseDto>.Ok(resultado));
    }

    /// <summary>Activa el 2FA tras verificar el primer código TOTP (HU-005 CA-01).</summary>
    [Authorize]
    [HttpPost("2fa/activar")]
    public async Task<IActionResult> Activar2fa(Activar2faRequestDto request)
    {
        await _authService.Activar2faAsync(request, User.GetUsuarioId(), User.GetEmpresaId());
        return Ok(ApiResponse<string>.Ok(string.Empty, "2FA activado"));
    }

    /// <summary>Desactiva el 2FA del usuario autenticado (requiere código actual para confirmar).</summary>
    [Authorize]
    [HttpPost("2fa/deactivate")]
    public async Task<IActionResult> Desactivar2fa(Desactivar2faRequestDto request)
    {
        await _authService.Desactivar2faAsync(User.GetUsuarioId(), User.GetEmpresaId(), request.Codigo);
        return Ok(ApiResponse<string>.Ok(string.Empty, "2FA desactivado"));
    }

    /// <summary>
    /// Segundo paso del login con 2FA (HU-005): valida el temp token y el código.
    /// Devuelve el access + refresh token completo si la verificación es correcta.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("2fa/verify")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Verificar2fa(Verificar2faRequestDto request)
    {
        var resultado = await _authService.Verificar2faAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Autenticación 2FA correcta"));
    }

    /// <summary>
    /// Login con OAuth (HU-004): recibe el token de Supabase Auth de un proveedor
    /// (google/microsoft) y resuelve la sesión. Implementación base en este sprint.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("oauth/callback")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> OAuthCallback(OAuthCallbackRequestDto request)
    {
        var resultado = await _authService.LoginConOAuthAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Inicio de sesión con OAuth exitoso"));
    }

    /// <summary>
    /// Obtiene los códigos de recuperación de 2FA. SIEMPRE retorna 422 porque los
    /// códigos en BD son hashes no reversibles — solo se muestran una vez al activar 2FA.
    /// </summary>
    [Authorize]
    [HttpGet("2fa/recovery-codes")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetRecoveryCodes()
    {
        // Siempre lanza BusinessException → 422 por el middleware.
        await _authService.GetRecoveryCodesAsync(
            User.GetUsuarioId(), User.GetEmpresaId());
        return Ok(); // nunca llega aquí
    }

    /// <summary>
    /// Regenera los 8 códigos de recuperación de 2FA. Retorna los códigos en claro —
    /// solo se muestran una vez; guárdalos en un lugar seguro.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/recovery-codes/regenerate")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> RegenerarRecoveryCodes()
    {
        var codigos = await _authService.RegenerarRecoveryCodesAsync(
            User.GetUsuarioId(), User.GetEmpresaId());
        return Ok(ApiResponse<List<string>>.Ok(
            codigos, "Nuevos códigos de recuperación generados. " +
            "Guárdalos en un lugar seguro — no se mostrarán de nuevo."));
    }
}