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
    private readonly IPrioridadOrdenService _prioridadService;
    private readonly ISlaService _slaService;
    private readonly IRechazoEntregaService _rechazoService;
    private readonly IOrdenPoService _ordenPoService;

    public OrdenesController(
        IOrdenService ordenService,
        IPrioridadOrdenService prioridadService,
        ISlaService slaService,
        IRechazoEntregaService rechazoService,
        IOrdenPoService ordenPoService)
    {
        _ordenService = ordenService;
        _prioridadService = prioridadService;
        _slaService = slaService;
        _rechazoService = rechazoService;
        _ordenPoService = ordenPoService;
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

    // ── Sprint 5: PO Integration (HU-028) ─────────────────────────

    /// <summary>Órdenes del tenant con un número de PO exacto (HU-028 CA-05).</summary>
    [HttpGet("por-po/{numero}")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetPorPo(string numero)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var items = await _ordenPoService.GetPorPoAsync(numero, empresaId);
        return Ok(ApiResponse<IEnumerable<OrdenListDto>>.Ok(items, "Órdenes con PO encontradas"));
    }

    /// <summary>Vincula o actualiza el PO/SO de una orden (HU-028 — PATCH).</summary>
    [HttpPatch("{id:guid}/po")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> VincularPo(Guid id, OrdenPoRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _ordenPoService.VincularPoAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "PO vinculado correctamente"));
    }

    // ── Sprint 5: Priorización dinámica (HU-029) ──────────────────

    /// <summary>Cambio manual de prioridad (HU-029 CA-09).</summary>
    [HttpPatch("{id:guid}/prioridad")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> CambiarPrioridad(Guid id, PrioridadRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _prioridadService.CambiarPrioridadAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<OrdenResponseDto>.Ok(result, "Prioridad actualizada"));
    }

    /// <summary>Órdenes críticas (CRITICO/ALTO con más de 4 h sin avanzar — HU-029 CA-04).</summary>
    [HttpGet("criticas")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetCriticas()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var items = await _prioridadService.GetCriticasAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<OrdenListDto>>.Ok(items, "Órdenes críticas"));
    }

    // ── Sprint 5: SLA (HU-031 CA-02) ──────────────────────────────

    /// <summary>Órdenes con SLA en riesgo o vencido (HU-031 CA-02).</summary>
    [HttpGet("sla-en-riesgo")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetSlaEnRiesgo()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var items = await _slaService.GetEnRiesgoAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<OrdenListDto>>.Ok(items, "Órdenes con SLA en riesgo"));
    }

    // ── Sprint 5: Rechazos y re-entregas (HU-030) ─────────────────

    /// <summary>Registra un rechazo de entrega y mueve la orden a FAILED_DELIVERY (HU-030 CA-01..03).</summary>
    [HttpPost("{id:guid}/rechazo")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<RechazoEntregaResponseDto>>> RegistrarRechazo(Guid id, RechazoEntregaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _rechazoService.RegistrarRechazoAsync(id, request, empresaId, usuarioId);
        return Ok(ApiResponse<RechazoEntregaResponseDto>.Ok(result, "Rechazo registrado"));
    }

    /// <summary>Crea la re-entrega de una orden FAILED_DELIVERY (HU-030 CA-05..07).</summary>
    [HttpPost("{id:guid}/re-entrega")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> CrearReentrega(Guid id, ReEntregaRequestDto? request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        var result = await _rechazoService.CrearReentregaAsync(id, request, empresaId, usuarioId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<OrdenResponseDto>.Ok(result, "Re-entrega creada"));
    }

    /// <summary>Historial de re-entregas de una orden (HU-030 CA-10).</summary>
    [HttpGet("{id:guid}/re-entregas")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<IActionResult> GetReentregas(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var items = await _rechazoService.GetReentregasAsync(id, empresaId);
        return Ok(ApiResponse<IEnumerable<OrdenListDto>>.Ok(items, "Re-entregas de la orden"));
    }
}
