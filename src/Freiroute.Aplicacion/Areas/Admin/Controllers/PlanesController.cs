using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

using Freiroute.BLL.Interfaces;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC para gestión de planes de suscripción del SaaS (Super Admin).
/// </summary>
public class PlanesController : BaseAdminController
{
    private readonly IPlanService _planService;

    public PlanesController(IPlanService planService)
    {
        _planService = planService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"]      = "Planes de Suscripción";
        ViewData["ActiveMenu"] = "admin-planes";
        var planes = await _planService.GetAllAsync(soloActivos: false);
        return View(planes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"]      = "Nuevo Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        return View(new Freiroute.DTO.Plan.PlanRequestDto());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["Title"]      = "Editar Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        ViewData["PlanId"]     = id;
        
        var plan = await _planService.GetByIdAsync(id);
        if (plan == null) return NotFound();

        var dto = new Freiroute.DTO.Plan.PlanRequestDto
        {
            Nombre = plan.Nombre,
            Codigo = plan.Codigo,
            Moneda = plan.Moneda,
            EsPublico = plan.EsPublico,
            Descripcion = plan.Descripcion ?? "",
            LimiteUsuarios = plan.LimiteUsuarios,
            LimiteEmbarquesMes = plan.LimiteEmbarquesMes,
            LimiteStorageGb = plan.LimiteStorageGb,
            PrecioMensual = plan.PrecioMensual,
            PrecioAnual = plan.PrecioAnual,
            ModulosDisponibles = plan.ModulosDisponibles.ToList()
        };

        return View(dto);
    }
}
