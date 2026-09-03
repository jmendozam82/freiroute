using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Permiso;
using Freiroute.DTO.Perfil;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de perfiles/roles del tenant (HU-006) + permisos anidados.
/// El empresa_id SIEMPRE viene del JWT (o header X-Empresa-Id si el SUPER_ADMIN
/// opera sobre un tenant concreto) — NUNCA del body del request.
/// Los permisos viven en /api/perfiles/{id}/permisos (contrato IPermisoService).
/// </summary>
[ApiController]
[Route("api/perfiles")]
[Authorize]
public class PerfilesController : ControllerBase
{
    private readonly IPerfilService _perfilService;
    private readonly IPermisoService _permisoService;

    public PerfilesController(
        IPerfilService perfilService,
        IPermisoService permisoService)
    {
        _perfilService = perfilService;
        _permisoService = permisoService;
    }

    /// <summary>Obtiene todos los perfiles del tenant con su conteo de usuarios.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PerfilResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PerfilResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var perfiles = await _perfilService.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<PerfilResponseDto>>.Ok(perfiles));
    }

    /// <summary>Crea un perfil personalizado (CUSTOM) del tenant.</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    public async Task<ActionResult<ApiResponse<PerfilResponseDto>>> Create(PerfilRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var perfil = await _perfilService.CreateAsync(request, empresaId);
        return Ok(ApiResponse<PerfilResponseDto>.Ok(perfil, "Perfil creado"));
    }

    /// <summary>Actualiza los datos de un perfil del tenant.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<PerfilResponseDto>>> Update(Guid id, PerfilRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var perfil = await _perfilService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<PerfilResponseDto>.Ok(perfil, "Perfil actualizado"));
    }

    /// <summary>Soft delete de un perfil. Los perfiles del sistema (es_sistema) no se pueden desactivar.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _perfilService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Perfil desactivado"));
    }

    // ── Permisos anidados del perfil (HU-006 / ADR-009) ────────────

    /// <summary>Obtiene los permisos del perfil indicado (GET /api/perfiles/{id}/permisos).</summary>
    [HttpGet("{id:guid}/permisos")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PermisoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermisoResponseDto>>>> GetPermisos(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var permisos = await _permisoService.GetByPerfilAsync(id, empresaId);
        return Ok(ApiResponse<IEnumerable<PermisoResponseDto>>.Ok(permisos));
    }

    /// <summary>
    /// Reemplaza TODOS los permisos de un perfil (PUT /api/perfiles/{id}/permisos).
    /// Los módulos omitidos en el body se desactivan (transaccional, ADR-009).
    /// </summary>
    [HttpPut("{id:guid}/permisos")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> ReemplazarPermisos(Guid id, PermisoRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _permisoService.ReemplazarPermisosAsync(id, request, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Permisos actualizados"));
    }
}