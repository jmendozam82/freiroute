using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC del catálogo de Tarifas Base (HU-020, ADR-015).
/// Index, Create y Simulador son client-side: las vistas consumen
/// GET/POST /api/tarifas-base y POST /api/tarifas-base/simular-costo vía FrApi.
/// </summary>
public class TarifasController : TenantBaseController
{
    // ── GET: Index ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "tarifas";
        return View();
    }

    // ── GET: Create ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "tarifas";
        return View();
    }

    // ── GET: Simulador ──────────────────────────────────────────
    [HttpGet]
    public IActionResult Simulador()
    {
        ViewData["ActiveMenu"] = "tarifas";
        return View();
    }
}