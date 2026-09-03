using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Freiroute.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador del Dashboard del área Admin. Muestra KPIs resumidos del tenant.
/// Los datos provienen del BLL; en este Sprint los KPIs son estáticos/demo.
/// </summary>
public class SharedController : BaseAdminController
{
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["ActiveMenu"] = "dashboard";
        ViewData["TenantNombre"] = "Freiroute SaaS Admin";
        return View("Dashboard/Index");
    }
}
