using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Zona;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de zonas de entrega (HU-016, ADR-018).
/// La verificación de pertenencia de un punto se resuelve con point-in-polygon
/// en la BLL para zonas tipo POLIGONO.
/// </summary>
[ApiController]
[Route("api/zonas-entrega")]
[Authorize]
public class ZonasEntregaController : ControllerBase
{
    private readonly IZonaEntregaService _zonaService;

    public ZonasEntregaController(IZonaEntregaService zonaService)
    {
        _zonaService = zonaService;
    }

    /// <summary>Lista todas las zonas activas de la empresa.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ZonaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ZonaResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _zonaService.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<ZonaResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene una zona por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<ZonaResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _zonaService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Zona con id '{id}' no encontrada."))
            : Ok(ApiResponse<ZonaResponseDto>.Ok(data));
    }

    /// <summary>
    /// Verifica en qué zonas cae el punto (CA-05). Ruta literal ANTES de {id}.
    /// </summary>
    [HttpPost("verificar-pertenencia")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ZonaResponseDto>>>> VerificarPertenencia(
        VerificarPertenenciaZonaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _zonaService.VerificarPertenenciaAsync(
            request.Latitud, request.Longitud, empresaId);
        return Ok(ApiResponse<IEnumerable<ZonaResponseDto>>.Ok(data));
    }

    /// <summary>Registra una zona nueva con su método de definición.</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ZonaResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ZonaResponseDto>>> Create(ZonaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _zonaService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<ZonaResponseDto>.Ok(data, "Zona creada"));
    }

    /// <summary>Actualiza la zona (nombre, color, método de definición, listas).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ZonaResponseDto>>> Update(Guid id, ZonaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _zonaService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<ZonaResponseDto>.Ok(data, "Zona actualizada"));
    }

    /// <summary>Soft delete de una zona — valida que no tenga tarifas activas (CA-06).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _zonaService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Zona desactivada"));
    }

    /// <summary>Asigna ubicaciones a la zona (relación muchos-a-muchos, CA-04).</summary>
    [HttpPost("{id:guid}/ubicaciones")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> AsignarUbicaciones(Guid id, [FromBody] List<Guid> ubicacionIds)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _zonaService.AsignarUbicacionesAsync(id, ubicacionIds, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Ubicaciones asignadas a la zona"));
    }

    /// <summary>Desasigna una ubicación de la zona.</summary>
    [HttpDelete("{id:guid}/ubicaciones/{ubicacionId:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> DesasignarUbicacion(Guid id, Guid ubicacionId)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _zonaService.DesasignarUbicacionAsync(id, ubicacionId, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Ubicación desasignada de la zona"));
    }
}