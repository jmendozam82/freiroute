using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC de Unidades de Medida y Embalajes (HU-018).
/// Un solo Index con tabs (Unidades | Embalajes) + simulador de conversión.
/// Vistas client-side: consumen GET /api/unidades-medida,
/// GET /api/unidades-medida/convertir y GET /api/tipos-embalaje vía FrApi.
/// </summary>
public class UnidadesController : TenantBaseController
{
    // ── GET: Index ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "unidades";
        return View();
    }
}