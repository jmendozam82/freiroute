using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Controlador principal del módulo de órdenes (HU-021, HU-024, HU-025, HU-026).
/// Expone operaciones CRUD, transiciones de estado, consolidación y split.
/// </summary>
[ApiController]
[Route("api/ordenes")]
[Authorize]
public class OrdenesController : ControllerBase
{
    private readonly IOrdenService _ordenService;

    public OrdenesController(IOrdenService ordenService)
    {
        _ordenService = ordenService;
    }

    [HttpGet]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<ActionResult<PagedResult<OrdenListDto>>> GetAll([FromQuery] OrdenFiltroDto filtro)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _ordenService.GetAllAsync(empresaId, filtro);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<OrdenResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _ordenService.GetByIdAsync(id, empresaId);
        return result is null 
            ? NotFound(ApiResponse<OrdenResponseDto>.Fail("Orden no encontrada."))
            : Ok(ApiResponse<OrdenResponseDto>.Ok(result));
    }

    [HttpPost]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Create)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> Create(OrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.CreateAsync(request, empresaId, usuarioId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<OrdenResponseDto>.Ok(result, "Orden creada correctamente"));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> Update(Guid id, OrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.UpdateAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "Orden actualizada correctamente"));
    }

    [HttpDelete("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var success = await _ordenService.DeactivateAsync(id, empresaId, usuarioId);
        return Ok(ApiResponse<bool>.Ok(success, "Orden desactivada"));
    }

    [HttpPatch("{id:guid}/estado")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> CambiarEstado(Guid id, CambiarEstadoOrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.CambiarEstadoAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "Estado actualizado"));
    }

    [HttpGet("{id:guid}/historial")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<HistorialEstadoOrdenDto>>>> GetHistorial(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _ordenService.GetHistorialAsync(id, empresaId);
        return Ok(ApiResponse<IEnumerable<HistorialEstadoOrdenDto>>.Ok(result));
    }

    [HttpPost("consolidar")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ShipmentResumenDto>>> Consolidar(ConsolidarOrdenesRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.ConsolidarAsync(request, empresaId, usuarioId);
        return Ok(ApiResponse<ShipmentResumenDto>.Ok(result, "Órdenes consolidadas correctamente"));
    }

    [HttpDelete("{id:guid}/desconsolidar")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> Desconsolidar(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.DesconsolidarAsync(id, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "Orden desconsolidada correctamente"));
    }

    [HttpPost("{id:guid}/split")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrdenResponseDto>>>> Split(Guid id, SplitOrdenRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenService.SplitAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<IEnumerable<OrdenResponseDto>>.Ok(result, "Orden dividida correctamente"));
    }
}
