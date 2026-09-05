using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

using Freiroute.BLL.Interfaces;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC para gestión de suscripciones del SaaS (Super Admin).
/// </summary>
public class SuscripcionesController : BaseAdminController
{
    private readonly ISuscripcionService _suscripcionService;

    public SuscripcionesController(ISuscripcionService suscripcionService)
    {
        _suscripcionService = suscripcionService;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, string? estado = null)
    {
        ViewData["Title"]      = "Suscripciones";
        ViewData["ActiveMenu"] = "admin-suscripciones";
        ViewData["Q"]          = q;
        ViewData["Estado"]     = estado;
        
        var paged = await _suscripcionService.GetAllAsync(estado, page, 20);
        return View(paged);
    }
}
