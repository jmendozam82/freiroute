using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Unidad;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints del catálogo de tipos de embalaje (HU-018): pallets,
/// cajas, tambores, contenedores. Código único por empresa.
/// </summary>
[ApiController]
[Route("api/tipos-embalaje")]
[Authorize]
public class TiposEmbalajeController : ControllerBase
{
    private readonly ITipoEmbalajeService _embalajeService;

    public TiposEmbalajeController(ITipoEmbalajeService embalajeService)
    {
        _embalajeService = embalajeService;
    }

    /// <summary>Lista todos los embalajes activos de la empresa.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TipoEmbalajeResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TipoEmbalajeResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _embalajeService.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<TipoEmbalajeResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene un embalaje por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<TipoEmbalajeResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _embalajeService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Tipo de embalaje con id '{id}' no encontrado."))
            : Ok(ApiResponse<TipoEmbalajeResponseDto>.Ok(data));
    }

    /// <summary>Crea un tipo de embalaje (código único por empresa).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<TipoEmbalajeResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TipoEmbalajeResponseDto>>> Create(TipoEmbalajeRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _embalajeService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<TipoEmbalajeResponseDto>.Ok(data, "Tipo de embalaje creado"));
    }

    /// <summary>Actualiza un tipo de embalaje.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<TipoEmbalajeResponseDto>>> Update(Guid id, TipoEmbalajeRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _embalajeService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<TipoEmbalajeResponseDto>.Ok(data, "Tipo de embalaje actualizado"));
    }

    /// <summary>Soft delete de un tipo de embalaje (ADR-005).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _embalajeService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Tipo de embalaje desactivado"));
    }
}