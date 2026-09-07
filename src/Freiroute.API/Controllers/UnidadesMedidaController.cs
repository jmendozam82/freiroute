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
/// Endpoints del catálogo de unidades de medida (HU-018).
/// Incluye el simulador de conversión entre símbolos del mismo tipo (CA-05).
/// </summary>
[ApiController]
[Route("api/unidades-medida")]
[Authorize]
public class UnidadesMedidaController : ControllerBase
{
    private readonly IUnidadMedidaService _unidadService;

    public UnidadesMedidaController(IUnidadMedidaService unidadService)
    {
        _unidadService = unidadService;
    }

    /// <summary>Lista unidades con filtro opcional por tipo (PESO, VOLUMEN, LONGITUD, TEMPERATURA).</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UnidadMedidaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UnidadMedidaResponseDto>>>> GetAll(
        [FromQuery] string? tipo)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _unidadService.GetAllAsync(empresaId, tipo);
        return Ok(ApiResponse<IEnumerable<UnidadMedidaResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene una unidad por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<UnidadMedidaResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _unidadService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Unidad de medida con id '{id}' no encontrada."))
            : Ok(ApiResponse<UnidadMedidaResponseDto>.Ok(data));
    }

    /// <summary>Simulador de conversión entre unidades del mismo tipo (CA-05).</summary>
    [HttpGet("convertir")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<decimal>>> Convertir(
        [FromQuery] decimal valor,
        [FromQuery] string desde,
        [FromQuery] string hacia)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _unidadService.ConvertirAsync(valor, desde, hacia, empresaId);
        return Ok(ApiResponse<decimal>.Ok(data, "Conversión realizada"));
    }

    /// <summary>Crea una unidad de medida (símbolo único por empresa).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<UnidadMedidaResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UnidadMedidaResponseDto>>> Create(UnidadMedidaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _unidadService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<UnidadMedidaResponseDto>.Ok(data, "Unidad de medida creada"));
    }

    /// <summary>Actualiza una unidad de medida.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<UnidadMedidaResponseDto>>> Update(Guid id, UnidadMedidaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _unidadService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<UnidadMedidaResponseDto>.Ok(data, "Unidad de medida actualizada"));
    }

    /// <summary>Soft delete de una unidad de medida (ADR-005).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _unidadService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Unidad de medida desactivada"));
    }
}