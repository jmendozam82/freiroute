using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Controlador para la gestión de API Keys externas del tenant (HU-023).
/// </summary>
[ApiController]
[Route("api/configuracion/api-keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IOrdenApiExternaService _apiExternaService;

    public ApiKeysController(IOrdenApiExternaService apiExternaService)
    {
        _apiExternaService = apiExternaService;
    }

    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ApiKeyResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _apiExternaService.GetApiKeysAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<ApiKeyResponseDto>>.Ok(result));
    }

    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ApiKeyResponseDto>>> Create(ApiKeyOrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _apiExternaService.GenerarApiKeyAsync(request, empresaId);
        return Ok(ApiResponse<ApiKeyResponseDto>.Ok(result, "API Key generada correctamente"));
    }

    [HttpDelete("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var success = await _apiExternaService.DesactivarApiKeyAsync(id, empresaId);
        return Ok(ApiResponse<bool>.Ok(success, "API Key desactivada"));
    }
}
