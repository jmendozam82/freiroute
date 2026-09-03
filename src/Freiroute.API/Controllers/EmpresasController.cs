using Freiroute.API.Attributes;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de gestión de empresas/tenants (HU-001) — SOLO SUPER_ADMIN
/// (los métodos del servicio no reciben empresa_id: operan a nivel SaaS global).
/// Permisos granulares sobre el módulo 'configuracion' + blindaje de rol.
/// </summary>
[ApiController]
[Route("api/empresas")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public EmpresasController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    /// <summary>Obtiene todas las empresas del SaaS (panel Super Admin).</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmpresaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmpresaResponseDto>>>> GetAll()
    {
        var empresas = await _empresaService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<EmpresaResponseDto>>.Ok(empresas));
    }

    /// <summary>Obtiene una empresa por Id.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<EmpresaResponseDto>>> GetById(Guid id)
    {
        var empresa = await _empresaService.GetByIdAsync(id);
        return empresa is null
            ? NotFound(ApiResponse<string>.Fail($"Empresa con id '{id}' no encontrada."))
            : Ok(ApiResponse<EmpresaResponseDto>.Ok(empresa));
    }

    /// <summary>Registra un nuevo tenant con sus perfiles base (HU-001 CA-02/03).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<EmpresaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EmpresaResponseDto>>> Create(EmpresaRequestDto request)
    {
        var empresa = await _empresaService.CreateAsync(request);
        return Ok(ApiResponse<EmpresaResponseDto>.Ok(empresa, "Empresa registrada correctamente"));
    }

    /// <summary>Actualiza los datos de una empresa.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<EmpresaResponseDto>>> Update(Guid id, EmpresaRequestDto request)
    {
        var empresa = await _empresaService.UpdateAsync(id, request);
        return Ok(ApiResponse<EmpresaResponseDto>.Ok(empresa, "Empresa actualizada"));
    }

    /// <summary>Soft delete de una empresa (nunca se elimina físicamente).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _empresaService.DeactivateAsync(id);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Empresa desactivada"));
    }
}