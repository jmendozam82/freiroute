using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.API.Controllers;

/// <summary>
/// Gestión de reclamos de clientes (HU-032 — Claims Management).
/// Ciclo de vida: ABIERTO → EN_REVISION → APROBADO/RECHAZADO → CERRADO
/// (FSM validada en el servicio — EstadoReclamo.Transiciones).
/// </summary>
[ApiController]
[Authorize]
[Route("api/reclamos")]
public class ReclamosController : ControllerBase
{
    private readonly IReclamoService _reclamoService;

    public ReclamosController(IReclamoService reclamoService) =>
        _reclamoService = reclamoService;

    /// <summary>Crea un reclamo en ABIERTO vinculado a una orden del tenant (HU-032 CA-01).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ReclamoResponseDto>), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Crea un reclamo", Description = "HU-032 CA-01/CA-02")]
    public async Task<ActionResult<ApiResponse<ReclamoResponseDto>>> Create(
        ReclamoRequestDto request)
    {
        var data = await _reclamoService.CreateAsync(
            request, User.GetEmpresaId(), User.GetUsuarioId());
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<ReclamoResponseDto>.Ok(data, "Reclamo creado"));
    }

    /// <summary>Detalle del reclamo con historial de estados (HU-032 CA-07).</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var data = await _reclamoService.GetByIdAsync(id, User.GetEmpresaId());
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Reclamo con id '{id}' no encontrado."))
            : Ok(ApiResponse<ReclamoResponseDto>.Ok(data));
    }

    /// <summary>Listado paginado de reclamos con filtros (HU-032 CA-06).</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetAll([FromQuery] ReclamoFiltroDto filtro)
    {
        var (items, total) = await _reclamoService.GetAllAsync(User.GetEmpresaId(), filtro);
        var result = new PagedResult<ReclamoListDto>
        {
            Items = items,
            TotalItems = total,
            PageNumber = filtro.Page,
            PageSize = filtro.PageSize
        };
        return Ok(result);
    }

    /// <summary>Transición de estado con motivo obligatorio (HU-032 CA-04/CA-12 — exige ordenes:update).</summary>
    [HttpPatch("{id:guid}/estado")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ReclamoResponseDto>>> CambiarEstado(
        Guid id, ReclamoEstadoRequestDto request)
    {
        var data = await _reclamoService.CambiarEstadoAsync(
            id, request, User.GetEmpresaId(), User.GetUsuarioId());
        return Ok(ApiResponse<ReclamoResponseDto>.Ok(data, "Estado actualizado"));
    }

    /// <summary>Reclamos de un cliente del tenant (HU-032 CA-10).</summary>
    [HttpGet("cliente/{clienteId:guid}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetByCliente(Guid clienteId)
    {
        var items = await _reclamoService.GetByClienteAsync(clienteId, User.GetEmpresaId());
        return Ok(ApiResponse<IEnumerable<ReclamoListDto>>.Ok(items));
    }
}