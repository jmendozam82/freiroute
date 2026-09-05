using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Settings;
using Freiroute.DTO.Auth;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Freiroute.Aplicacion.Areas.Auth.Controllers;

/// <summary>
/// Controlador de autenticación de la capa MVC (sirve las vistas de Login,
/// ForgotPassword y ResetPassword). Delega en IAuthService (BLL) para la
/// lógica de negocio real y emite una cookie de autenticación con los claims
/// del JWT (ADR-007) para que las vistas Razor puedan usar User.HasPermission.
/// </summary>
[Area("Auth")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── GET: Login ───────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay sesión activa, redirigir al dashboard.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequestDto());
    }

    // ── POST: Login (AJAX) ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            // Construir los claims desde el access token JWT real (ADR-007).
            var claims = DecodeJwtClaims(result.AccessToken);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Guardar el refresh token en cookie para logout.
            Response.Cookies.Append("fr_refresh_token", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = false,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                });

            return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Inicio de sesión exitoso"));
        }
        catch (Freiroute.Utility.Exceptions.BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<LoginResponseDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail("Error en el inicio de sesión: " + ex.Message));
        }
    }

    // ── GET: ForgotPassword ──────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordRequestDto());

    // ── POST: ForgotPassword ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request)
    {
        try
        {
            // Respuesta SIEMPRE genérica (HU-007 CA-03): no revelar si el email existe.
            await _authService.ForgotPasswordAsync(request);
            return Ok(ApiResponse<string>.Ok(string.Empty,
                "Si el email está registrado, recibirás un enlace para restablecer tu contraseña"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── GET: ResetPassword ───────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        return View(new ResetPasswordRequestDto { Token = token ?? string.Empty });
    }

    // ── POST: ResetPassword ──────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(ApiResponse<string>.Ok(string.Empty,
                "Contraseña restablecida correctamente. Ya puede iniciar sesión."));
        }
        catch (Freiroute.Utility.Exceptions.BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST: Logout ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Invalidar refresh token (si lo tenemos) antes de limpiar la cookie.
        var refresh = Request.Cookies["fr_refresh_token"];
        if (!string.IsNullOrEmpty(refresh))
        {
            try { await _authService.LogoutAsync(refresh); }
            catch { /* logout idempotente */ }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("fr_refresh_token");
        return RedirectToAction(nameof(Login), new { area = "Auth" });
    }

    // ── GET: AccessDenied ───────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Acceso denegado";
        return View();
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Decodifica los claims del JWT generado por el BLL (JwtService, ADR-007).
    /// No valida la firma aquí — el token lo acaba de generar el JwtService
    /// en el mismo proceso; simplemente extraemos los claims para el principal.
    /// </summary>
    private static List<Claim> DecodeJwtClaims(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        var claims = jwt.Claims.ToList();

        // Asegurar claim tipo "rol" para User.IsInRole() y ClaimTypes.Email.
        // El JWT usa tipo_usuario como rol; lo re-mapeamos también a ClaimTypes.Role.
        var tipoUsuario = claims.FirstOrDefault(c => c.Type == "tipo_usuario")?.Value;
        if (!string.IsNullOrEmpty(tipoUsuario))
        {
            claims.Add(new Claim(ClaimTypes.Role, tipoUsuario));
        }

        var email = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim(ClaimTypes.Name, email));
        }

        // Si el claim "permisos" no existe (SUPER_ADMIN no usa tabla permisos),
        // no añadimos ninguno — la extensión HasPermission da acceso total por rol.
        return claims;
    }

    private IActionResult RedirectToDashboard()
    {
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }
}
