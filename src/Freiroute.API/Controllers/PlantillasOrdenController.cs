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
/// Controlador para la gestión de plantillas de órdenes y su recurrencia (HU-027).
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class PlantillasOrdenController : ControllerBase
{
    private readonly IPlantillaOrdenService _plantillaService;

    public PlantillasOrdenController(IPlantillaOrdenService plantillaService)
    {
        _plantillaService = plantillaService;
    }

    [HttpPost("ordenes/{ordenId:guid}/guardar-como-plantilla")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Create)]
    public async Task<ActionResult<ApiResponse<PlantillaOrdenResponseDto>>> GuardarComoPlantilla(Guid ordenId, PlantillaOrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _plantillaService.GuardarComoPlantillaAsync(ordenId, request, empresaId, usuarioId);
        return Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result, "Plantilla guardada correctamente"));
    }

    [HttpGet("plantillas-orden")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PlantillaOrdenResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _plantillaService.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<PlantillaOrdenResponseDto>>.Ok(result));
    }

    [HttpGet("plantillas-orden/{id:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<PlantillaOrdenResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _plantillaService.GetByIdAsync(id, empresaId);
        return result is null 
            ? NotFound(ApiResponse<PlantillaOrdenResponseDto>.Fail("Plantilla no encontrada")) 
            : Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result));
    }

    [HttpPost("plantillas-orden/{id:guid}/crear-orden")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Create)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> CrearOrdenDesdePlantilla(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _plantillaService.CrearOrdenDesdePlantillaAsync(id, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "Orden creada desde la plantilla"));
    }

    [HttpPut("plantillas-orden/{id:guid}/recurrencia")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<PlantillaOrdenResponseDto>>> ConfigurarRecurrencia(Guid id, ConfigurarRecurrenciaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _plantillaService.ConfigurarRecurrenciaAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result, "Recurrencia configurada correctamente"));
    }

    [HttpPut("plantillas-orden/{id:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<PlantillaOrdenResponseDto>>> Update(Guid id, PlantillaOrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _plantillaService.UpdateAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result, "Plantilla actualizada"));
    }

    [HttpDelete("plantillas-orden/{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var success = await _plantillaService.DeactivateAsync(id, empresaId, usuarioId);
        return Ok(ApiResponse<bool>.Ok(success, "Plantilla desactivada"));
    }
}
