using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC del Dashboard Super Admin (Panel de Administración Global).
/// Muestra KPIs, empresas por estado/plan y tenants por vencer.
/// Las métricas se cargan via AJAX desde /api/admin/dashboard.
/// </summary>
public class DashboardController : BaseAdminController
{
    public IActionResult Index()
    {
        ViewData["Title"]      = "Panel de Administración";
        ViewData["ActiveMenu"] = "admin-dashboard";
        return View(new Freiroute.DTO.Admin.DashboardGlobalResponseDto
        {
            EmpresasPorEstado = new Dictionary<string, int>(),
            EmpresasPorPlan = new Dictionary<string, int>(),
            TenantsPorVencer = new List<Freiroute.DTO.Suscripcion.SuscripcionResponseDto>()
        });
    }
}
