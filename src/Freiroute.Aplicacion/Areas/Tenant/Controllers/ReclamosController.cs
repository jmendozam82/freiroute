using Freiroute.Aplicacion.Areas.Tenant.ViewModels;
using Freiroute.Aplicacion.Helpers;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Reclamo;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controller MVC del módulo de reclamos (HU-032 — Claims Management).
/// Sigue el patrón server-side del área Tenant (ClientesController /
/// OrdenesController): inyecta el servicio BLL y renderiza listado,
/// formulario de creación y detalle con historial. El cambio de estado y
/// la creación se ejecutan desde el cliente vía FrApi hacia la API
/// (POST /api/reclamos, PATCH /api/reclamos/{id}/estado).
/// Los permisos se verifican a nivel API ([RequirePermission]) y en la UI
/// con User.HasPermission — los controllers MVC del área no usan
/// [RequireModulePermission] (patrón existente del proyecto).
/// </summary>
public class ReclamosController : TenantBaseController
{
    /// <summary>20 registros por página — RNF-01.4.</summary>
    private const int PageSize = 20;

    private readonly IReclamoService _reclamoService;

    public ReclamosController(IReclamoService reclamoService)
        => _reclamoService = reclamoService;

    // ── GET: /reclamos ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        string? estado, string? tipo, DateTime? desde, DateTime? hasta, int page = 1)
    {
        ViewData["ActiveMenu"] = "reclamos";
        ViewData["Title"] = "Reclamos";

        if (!User.HasPermission("ordenes", "READ")) return Forbid();

        var filtro = new ReclamoFiltroDto
        {
            Estado = estado,
            Tipo = tipo,
            Desde = desde,
            Hasta = hasta,
            Page = page,
            PageSize = PageSize,
        };

        var (items, total) = await _reclamoService.GetAllAsync(EmpresaId, filtro);

        var vm = new ReclamoIndexViewModel
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = PageSize,
            Filtro = filtro,
        };

        return View(vm);
    }

    // ── GET: /reclamos/crear ────────────────────────────────────
    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["ActiveMenu"] = "reclamos";
        ViewData["Title"] = "Nuevo Reclamo";

        if (!User.HasPermission("ordenes", "CREATE")) return Forbid();

        return View();
    }

    // ── GET: /reclamos/{id} ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id)
    {
        ViewData["ActiveMenu"] = "reclamos";

        if (!User.HasPermission("ordenes", "READ")) return Forbid();

        var reclamo = await _reclamoService.GetByIdAsync(id, EmpresaId);
        if (reclamo is null) return NotFound();

        ViewData["Title"] = reclamo.NumeroReclamo ?? "Reclamo";
        return View(reclamo);
    }
}
