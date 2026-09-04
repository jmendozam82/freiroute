using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC para gestión de suscripciones del SaaS (Super Admin).
/// Las vistas cargan datos via AJAX desde /api/admin/suscripciones.
/// </summary>
public class SuscripcionesController : BaseAdminController
{
    public IActionResult Index()
    {
        ViewData["Title"]      = "Suscripciones";
        ViewData["ActiveMenu"] = "admin-suscripciones";
        return View();
    }
}
