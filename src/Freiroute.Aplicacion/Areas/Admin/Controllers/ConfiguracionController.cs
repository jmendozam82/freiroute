using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Configuración del tenant. Vista mínima para el panel Admin.
/// </summary>
public class ConfiguracionController : BaseAdminController
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Configuración";
        ViewData["ActiveMenu"] = "config";
        return View();
    }
}
