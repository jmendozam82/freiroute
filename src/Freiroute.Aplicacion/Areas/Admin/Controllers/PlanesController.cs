using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC para gestión de planes de suscripción del SaaS (Super Admin).
/// Las vistas cargan datos via AJAX desde /api/admin/planes.
/// </summary>
public class PlanesController : BaseAdminController
{
    public IActionResult Index()
    {
        ViewData["Title"]      = "Planes de Suscripción";
        ViewData["ActiveMenu"] = "admin-planes";
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"]      = "Nuevo Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        return View();
    }

    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        ViewData["Title"]      = "Editar Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        ViewData["PlanId"]     = id;
        return View();
    }
}
