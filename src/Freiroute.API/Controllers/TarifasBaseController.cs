using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Tarifa;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de tarifas de transporte (HU-020, ADR-015).
/// Las actualizaciones crean NUEVA versión (la anterior se cierra vigente hasta ayer);
/// el cálculo de costo simula flete + recargos con precio mínimo y seguro (CA-08).
/// </summary>
[ApiController]
[Route("api/tarifas-base")]
[Authorize]
public class TarifasBaseController : ControllerBase
{
    private readonly ITarifaBaseService _tarifaService;

    public TarifasBaseController(ITarifaBaseService tarifaService)
    {
        _tarifaService = tarifaService;
    }

    /// <summary>Lista paginada de tarifas con filtros por ruta, modo y vigencia.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TarifaBaseResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<TarifaBaseResponseDto>>>> GetAll(
        [FromQuery] Guid? zonaOrigenId,
        [FromQuery] Guid? zonaDestinoId,
        [FromQuery] string? modo,
        [FromQuery] bool? soloVigentes,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.GetAllAsync(
            empresaId, zonaOrigenId, zonaDestinoId, modo, soloVigentes, page, pageSize);
        return Ok(ApiResponse<PagedResult<TarifaBaseResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene una tarifa por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<TarifaBaseResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Tarifa con id '{id}' no encontrada."))
            : Ok(ApiResponse<TarifaBaseResponseDto>.Ok(data));
    }

    /// <summary>Tarifa vigente para la combinación ruta/modo/servicio (CA-08).</summary>
    [HttpGet("vigente")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<TarifaBaseResponseDto?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TarifaBaseResponseDto?>>> GetVigente(
        [FromQuery] Guid zonaOrigenId,
        [FromQuery] Guid? zonaDestinoId,
        [FromQuery] string modo,
        [FromQuery] string? tipoServicio,
        [FromQuery] DateOnly? fecha = null)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.GetVigenteAsync(
            empresaId, zonaOrigenId, zonaDestinoId, modo,
            tipoServicio ?? string.Empty,
            fecha ?? DateOnly.FromDateTime(DateTime.Today));
        return Ok(data is null
            ? ApiResponse<TarifaBaseResponseDto?>.Ok(null, "No existe tarifa vigente para la combinación consultada.")
            : ApiResponse<TarifaBaseResponseDto?>.Ok(data));
    }

    /// <summary>Crea la primera versión de una tarifa (vigencia desde hoy).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<TarifaBaseResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TarifaBaseResponseDto>>> Create(TarifaBaseRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<TarifaBaseResponseDto>.Ok(data, "Tarifa creada (vigente desde hoy)"));
    }

    /// <summary>Actualiza la tarifa: cierra la versión actual y crea una nueva (ADR-015).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<TarifaBaseResponseDto>>> Update(Guid id, TarifaBaseRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<TarifaBaseResponseDto>.Ok(data, "Tarifa actualizada (nueva versión creada)"));
    }

    /// <summary>Soft delete de la versión actual de la tarifa (ADR-005).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _tarifaService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Tarifa desactivada"));
    }

    // ── Recargos ───────────────────────────────────────────────

    /// <summary>Agrega un recargo a la tarifa (código único dentro de la tarifa).</summary>
    [HttpPost("{id:guid}/recargos")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<RecargoTarifaResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RecargoTarifaResponseDto>>> AgregarRecargo(
        Guid id, RecargoTarifaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.AgregarRecargoAsync(id, request, empresaId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RecargoTarifaResponseDto>.Ok(data, "Recargo agregado"));
    }

    /// <summary>Actualiza un recargo de la tarifa.</summary>
    [HttpPut("{id:guid}/recargos/{recargoId:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<RecargoTarifaResponseDto>>> UpdateRecargo(
        Guid id, Guid recargoId, RecargoTarifaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.UpdateRecargoAsync(id, recargoId, request, empresaId);
        return Ok(ApiResponse<RecargoTarifaResponseDto>.Ok(data, "Recargo actualizado"));
    }

    /// <summary>Soft delete de un recargo de la tarifa.</summary>
    [HttpPatch("{id:guid}/recargos/{recargoId:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> DeactivateRecargo(Guid id, Guid recargoId)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _tarifaService.DeactivateRecargoAsync(id, recargoId, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Recargo desactivado"));
    }

    // ── Simulación ─────────────────────────────────────────────

    /// <summary>Simulador de costo de flete: base + recargos + precio mínimo + seguro (CA-08, CA-09).</summary>
    [HttpPost("simular-costo")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<SimularCostoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SimularCostoResponseDto>>> SimularCosto(
        SimularCostoRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _tarifaService.SimularCostoAsync(request, empresaId);
        return Ok(ApiResponse<SimularCostoResponseDto>.Ok(data, "Simulación de costo realizada"));
    }
}