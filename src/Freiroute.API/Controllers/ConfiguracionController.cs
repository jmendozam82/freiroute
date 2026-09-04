using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Configuracion;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de configuración general del tenant (HU-014).
/// Se lee/escribe sobre la tabla 'empresas'; el logo se sube a Supabase Storage.
/// El empresa_id se extrae del JWT (o header X-Empresa-Id).
/// </summary>
[ApiController]
[Route("api/configuracion")]
[Authorize]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    /// <summary>Obtiene la configuración general del tenant.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConfiguracionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfiguracionResponseDto>>> Get()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _configuracionService.GetAsync(empresaId);
        return Ok(ApiResponse<ConfiguracionResponseDto>.Ok(data));
    }

    /// <summary>Actualiza la configuración general del tenant (HU-014 CA-04).</summary>
    [HttpPut]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ConfiguracionResponseDto>>> Update(ConfiguracionRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _configuracionService.UpdateAsync(request, empresaId);
        return Ok(ApiResponse<ConfiguracionResponseDto>.Ok(data, "Configuración actualizada"));
    }

    /// <summary>Sube el logo del tenant a Supabase Storage y devuelve la signed URL (24 h).</summary>
    [HttpPost("logo")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubirLogo([FromForm] IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Debe adjuntar el archivo del logo."));
        }

        var empresaId = User.GetTenantEfectivo(HttpContext);
        using var stream = archivo.OpenReadStream();
        var contentType = archivo.ContentType;
        var url = await _configuracionService.UpdateLogoAsync(empresaId, stream, contentType);
        return Ok(ApiResponse<string>.Ok(url, "Logo actualizado"));
    }

    /// <summary>Elimina el logo actual del tenant.</summary>
    [HttpDelete("logo")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> EliminarLogo()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _configuracionService.DeleteLogoAsync(empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Logo eliminado"));
    }

    /// <summary>Obtiene los prefijos y consecutivos de numeración actuales.</summary>
    [HttpGet("numeracion")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<NumeracionResponseDto>>> GetNumeracion()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _configuracionService.GetNumeracionAsync(empresaId);
        return Ok(ApiResponse<NumeracionResponseDto>.Ok(data));
    }

    /// <summary>Actualiza los prefijos de numeración (hu-014 CA-05). Los consecutivos no se editan.</summary>
    [HttpPut("numeracion")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<NumeracionResponseDto>>> UpdateNumeracion(NumeracionRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _configuracionService.UpdateNumeracionAsync(request, empresaId);
        return Ok(ApiResponse<NumeracionResponseDto>.Ok(data, "Numeración actualizada"));
    }
}
