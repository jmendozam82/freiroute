using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Home del área Admin. Redirige al Dashboard (Super Admin / Admin de tenant).
/// </summary>
public class HomeController : BaseAdminController
{
    public IActionResult Index()
    {
        return RedirectToAction("Dashboard", "Shared");
    }
}
