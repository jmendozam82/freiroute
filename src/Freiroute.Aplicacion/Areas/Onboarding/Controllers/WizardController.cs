using System.Security.Claims;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Onboarding;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Onboarding.Controllers;

/// <summary>
/// Controlador MVC del wizard de onboarding (HU-012, ADR-010).
/// Sirve las vistas Areas/Onboarding/Views/Wizard/Paso1..Paso5.cshtml usando
/// rutas por path (/onboarding/paso/N) coherentes con el JS de las vistas y
/// con el redirect del OnboardingRedirectMiddleware (Fix re-smoke test #6).
/// Los POST delegan DIRECTAMENTE en el BLL in-process (IOnboardingService) —
/// mismo patrón que AccountController con IAuthService (no pasa por la REST API).
/// </summary>
[Area("Onboarding")]
[Authorize]
public class WizardController : Controller
{
    private readonly IOnboardingService _onboardingService;
    private readonly IPerfilService _perfilService;

    public WizardController(IOnboardingService onboardingService, IPerfilService perfilService)
    {
        _onboardingService = onboardingService;
        _perfilService = perfilService;
    }

    /// <summary>empresa_id extraído del claim del JWT (cookie FreirouteSession, ADR-007).</summary>
    private Guid EmpresaId =>
        Guid.TryParse(User.FindFirst("empresa_id")?.Value, out var id) ? id : Guid.Empty;

    /// <summary>user_id extraído del claim del JWT (cookie FreirouteSession, ADR-007).</summary>
    private Guid UsuarioId =>
        Guid.TryParse(User.FindFirst("user_id")?.Value, out var id) ? id : Guid.Empty;

    // ── GET: pasos ──────────────────────────────────────────────

    /// <summary>Paso 1 — Datos de la empresa.</summary>
    [HttpGet]
    [Route("onboarding/paso/1")]
    public IActionResult Paso1()
    {
        ViewData["Paso"] = 1;
        ViewData["Porcentaje"] = 20;
        return View();
    }

    /// <summary>Paso 2 — Identidad visual (colores + logo).</summary>
    [HttpGet]
    [Route("onboarding/paso/2")]
    public IActionResult Paso2()
    {
        ViewData["Paso"] = 2;
        ViewData["Porcentaje"] = 40;
        return View();
    }

    /// <summary>Paso 3 — Configuración operativa.</summary>
    [HttpGet]
    [Route("onboarding/paso/3")]
    public IActionResult Paso3()
    {
        ViewData["Paso"] = 3;
        ViewData["Porcentaje"] = 60;
        return View();
    }

    /// <summary>Paso 4 — Primer administrador (prefill con el email del usuario logueado).</summary>
    [HttpGet]
    [Route("onboarding/paso/4")]
    public IActionResult Paso4()
    {
        ViewData["Paso"] = 4;
        ViewData["Porcentaje"] = 80;

        // Prefill del nombre con el email del claim (ClaimTypes.Email o fallback "email").
        ViewData["NombreUsuario"] =
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;

        return View();
    }

    /// <summary>Paso 5 — Invitaciones al equipo (perfiles reales del tenant).</summary>
    [HttpGet]
    [Route("onboarding/paso/5")]
    public async Task<IActionResult> Paso5()
    {
        ViewData["Paso"] = 5;
        ViewData["Porcentaje"] = 100;

        // Perfiles reales del tenant para renderizar los selects server-side.
        ViewData["Perfiles"] = await _perfilService.GetAllAsync(EmpresaId);

        return View();
    }

    // ── POST: guardado de pasos (delegación directa al BLL) ─────

    /// <summary>Guarda el Paso 1 — datos de la empresa.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/1/guardar")]
    public async Task<IActionResult> GuardarPaso1([FromBody] OnboardingPaso1RequestDto dto)
    {
        try
        {
            await _onboardingService.GuardarPaso1Async(dto, EmpresaId);
            return Json(ApiResponse<bool>.Ok(true, "Paso 1 guardado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al guardar el paso 1: " + ex.Message));
        }
    }

    /// <summary>Guarda el Paso 2 — identidad visual (colores + URL del logo ya subido).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/2/guardar")]
    public async Task<IActionResult> GuardarPaso2([FromBody] OnboardingPaso2RequestDto dto)
    {
        try
        {
            await _onboardingService.GuardarPaso2Async(dto, EmpresaId);
            return Json(ApiResponse<bool>.Ok(true, "Paso 2 guardado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al guardar el paso 2: " + ex.Message));
        }
    }

    /// <summary>Sube el logo del tenant y devuelve la URL firmada (campo multipart "archivo").</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/2/logo")]
    public async Task<IActionResult> SubirLogo(IFormFile archivo)
    {
        try
        {
            if (archivo is null || archivo.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("No se recibió ningún archivo válido."));
            }

            var extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return UnprocessableEntity(ApiResponse<string>.Fail(
                    "La extensión del archivo no es válida (use PNG, SVG, JPEG o WebP)."));
            }

            await using var stream = archivo.OpenReadStream();
            var url = await _onboardingService.GuardarLogoAsync(EmpresaId, stream, extension);

            return Json(ApiResponse<string>.Ok(url, "Logo subido correctamente"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<string>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail("Error al subir el logo: " + ex.Message));
        }
    }

    /// <summary>Guarda el Paso 3 — configuración operativa.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/3/guardar")]
    public async Task<IActionResult> GuardarPaso3([FromBody] OnboardingPaso3RequestDto dto)
    {
        try
        {
            await _onboardingService.GuardarPaso3Async(dto, EmpresaId);
            return Json(ApiResponse<bool>.Ok(true, "Paso 3 guardado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al guardar el paso 3: " + ex.Message));
        }
    }

    /// <summary>Guarda el Paso 4 — primer administrador (actualiza nombre/teléfono del usuario).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/4/guardar")]
    public async Task<IActionResult> GuardarPaso4([FromBody] OnboardingPaso4RequestDto dto)
    {
        try
        {
            await _onboardingService.GuardarPaso4Async(dto, EmpresaId, UsuarioId);
            return Json(ApiResponse<bool>.Ok(true, "Paso 4 guardado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al guardar el paso 4: " + ex.Message));
        }
    }

    /// <summary>Guarda el Paso 5 — invitaciones al equipo (máx 5).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/paso/5/guardar")]
    public async Task<IActionResult> GuardarPaso5([FromBody] OnboardingPaso5RequestDto dto)
    {
        try
        {
            await _onboardingService.GuardarPaso5Async(dto, EmpresaId, UsuarioId);
            return Json(ApiResponse<bool>.Ok(true, "Paso 5 guardado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al guardar el paso 5: " + ex.Message));
        }
    }

    /// <summary>Marca el onboarding como completado y redirige al dashboard.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("onboarding/completar")]
    public async Task<IActionResult> Completar()
    {
        try
        {
            await _onboardingService.CompletarAsync(EmpresaId);
            return Json(ApiResponse<bool>.Ok(true, "Onboarding completado"));
        }
        catch (BusinessException ex)
        {
            return UnprocessableEntity(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Error al completar el onboarding: " + ex.Message));
        }
    }
}