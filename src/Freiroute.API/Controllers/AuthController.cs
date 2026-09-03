using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Utility.ApiResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints públicos de autenticación (HU-003, HU-007).
/// NO requieren token JWT (AllowAnonymous) — son la puerta de entrada del sistema.
/// Todas las respuestas usan el wrapper ApiResponse&lt;T&gt; (ADR-008).
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Inicia sesión con email y contraseña (HU-003). Devuelve access + refresh token.</summary>
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
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token renovado"));
    }

    /// <summary>Cierra la sesión invalidando el refresh token (HU-003). Idempotente.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Sesión cerrada"));
    }

    /// <summary>Solicita la recuperación de contraseña (HU-007). Respuesta SIEMPRE genérica (CA-03).</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<string>.Ok(string.Empty,
            "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña."));
    }

    /// <summary>Restablece la contraseña con el token de un solo uso (HU-007 CA-04/05).</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Contraseña actualizada"));
    }
}