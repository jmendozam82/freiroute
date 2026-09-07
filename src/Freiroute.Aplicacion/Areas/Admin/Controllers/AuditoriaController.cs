using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC de Auditoría (G-02, HU-008).
/// La vista es client-side: consume GET /api/auditoria con filtros
/// (modulo, accion, fechaDesde, fechaHasta, page) y el export CSV
/// GET /api/auditoria/export vía FrApi/window.location.
/// Accesible para SUPER_ADMIN y ADMIN con permiso configuracion:read.
/// </summary>
public class AuditoriaController : BaseAdminController
{
    // ── GET: Index ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "auditoria";
        return View();
    }
}