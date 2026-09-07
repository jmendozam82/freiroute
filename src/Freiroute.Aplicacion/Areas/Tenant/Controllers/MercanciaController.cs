using Freiroute.DTO.Mercancia;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC del catálogo de Tipos de Mercancía (HU-017).
/// Vistas client-side: consumen GET/POST /api/tipos-mercancia vía FrApi.
/// </summary>
public class MercanciaController : TenantBaseController
{
    // ── GET: Index ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "mercancia";
        return View();
    }

    // ── GET: Create ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "mercancia";
        return View(new TipoMercanciaRequestDto());
    }
}