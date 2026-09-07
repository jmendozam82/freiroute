using Freiroute.DTO.Zona;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC de Zonas de Entrega (HU-016).
/// El Index y el Create son client-side: las vistas consumen
/// GET/POST /api/zonas-entrega vía FrApi (el JWT viaja en el header).
/// </summary>
public class ZonasController : TenantBaseController
{
    // ── GET: Index ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "zonas";
        return View();
    }

    // ── GET: Create ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "zonas";
        return View(new ZonaRequestDto());
    }
}