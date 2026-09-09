using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.API.Controllers;

/// <summary>
/// Reportes operacionales de Sprint 5 (HU-031 SLA · HU-032 Reclamos).
/// Accesible con permiso analytics:read (módulo configurable por el Admin).
/// </summary>
[ApiController]
[Authorize]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly ISlaService _slaService;
    private readonly IReclamoService _reclamoService;

    public ReportesController(ISlaService slaService, IReclamoService reclamoService)
    {
        _slaService = slaService;
        _reclamoService = reclamoService;
    }

    /// <summary>Reporte de cumplimiento SLA por cliente en el período (HU-031 CA-07).</summary>
    [HttpGet("sla")]
    [RequirePermission(ModuloPermiso.Analytics, PermissionType.Read)]
    [SwaggerOperation(Summary = "Reporte SLA por cliente",
        Description = "HU-031 CA-07 — porcentaje de entregas a tiempo por cliente en el período")]
    public async Task<IActionResult> Sla(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var fin = hasta?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;
        var inicio = desde ?? fin.AddDays(-30);
        var items = await _slaService.GetReporteSlaAsync(User.GetEmpresaId(), inicio, fin);
        return Ok(ApiResponse<IEnumerable<SlaReporteItemDto>>.Ok(items));
    }

    /// <summary>Reporte de reclamos agregado por tipo/estado en el período (HU-032 CA-09).</summary>
    [HttpGet("reclamos")]
    [RequirePermission(ModuloPermiso.Analytics, PermissionType.Read)]
    [SwaggerOperation(Summary = "Reporte de reclamos",
        Description = "HU-032 CA-09 — conteo y montos por tipo y estado en el período")]
    public async Task<IActionResult> Reclamos(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var fin = hasta?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;
        var inicio = desde ?? fin.AddDays(-30);
        var items = await _reclamoService.GetReporteAsync(User.GetEmpresaId(), inicio, fin);
        return Ok(ApiResponse<IEnumerable<ReclamoReporteItemDto>>.Ok(items));
    }
}