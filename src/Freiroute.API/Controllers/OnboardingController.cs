using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Onboarding;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints del wizard de onboarding multi-paso de un tenant (HU-012, ADR-010).
/// El empresa_id se extrae del JWT (o header X-Empresa-Id). Los pasos 1→5
/// persisten el progreso en la tabla 'empresas' (onboarding_paso_actual).
/// </summary>
[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    /// <summary>Obtiene el estado actual del onboarding (paso, %) y datos para pre-llenar.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<OnboardingEstadoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OnboardingEstadoResponseDto>>> GetEstado()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _onboardingService.GetEstadoAsync(empresaId);
        return Ok(ApiResponse<OnboardingEstadoResponseDto>.Ok(data));
    }

    /// <summary>Guarda el Paso 1: datos de la empresa (HU-012 CA-02).</summary>
    [HttpPost("paso1")]
    public async Task<IActionResult> GuardarPaso1(OnboardingPaso1RequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _onboardingService.GuardarPaso1Async(request, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Paso 1 guardado"));
    }

    /// <summary>Guarda el Paso 2: identidad visual (HU-012 CA-03).</summary>
    [HttpPost("paso2")]
    public async Task<IActionResult> GuardarPaso2(OnboardingPaso2RequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _onboardingService.GuardarPaso2Async(request, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Paso 2 guardado"));
    }

    /// <summary>Guarda el Paso 3: configuración operativa (HU-012 CA-04).</summary>
    [HttpPost("paso3")]
    public async Task<IActionResult> GuardarPaso3(OnboardingPaso3RequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _onboardingService.GuardarPaso3Async(request, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Paso 3 guardado"));
    }

    /// <summary>Guarda el Paso 4: primer administrador (HU-012 CA-05).</summary>
    [HttpPost("paso4")]
    public async Task<IActionResult> GuardarPaso4(OnboardingPaso4RequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        await _onboardingService.GuardarPaso4Async(request, empresaId, usuarioId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Paso 4 guardado"));
    }

    /// <summary>Guarda el Paso 5: invita al equipo (máx 5). Lista vacía = skip (HU-012 CA-06).</summary>
    [HttpPost("paso5")]
    public async Task<IActionResult> GuardarPaso5(OnboardingPaso5RequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var invitadoPorId = User.GetUsuarioId();
        await _onboardingService.GuardarPaso5Async(request, empresaId, invitadoPorId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Paso 5 guardado"));
    }

    /// <summary>Marca el onboarding como completado (HU-012 CA-08).</summary>
    [HttpPost("completar")]
    public async Task<IActionResult> Completar()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _onboardingService.CompletarAsync(empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Onboarding completado"));
    }

    /// <summary>Sube el logo del tenant a Supabase Storage (bucket privado) y devuelve la signed URL.</summary>
    [HttpPost("logo")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubirLogo([FromForm] IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Debe adjuntar el archivo del logo."));
        }

        var empresaId = User.GetTenantEfectivo(HttpContext);
        var extension = System.IO.Path.GetExtension(archivo.FileName);
        using var stream = archivo.OpenReadStream();
        var url = await _onboardingService.GuardarLogoAsync(empresaId, stream, extension);
        return Ok(ApiResponse<string>.Ok(url, "Logo subido exitosamente"));
    }
}
