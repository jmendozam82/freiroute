using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Freiroute.BLL.Interfaces;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Controlador MVC para gestión de planes de suscripción del SaaS (Super Admin).
/// </summary>
[Authorize(Roles = "SUPER_ADMIN")]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Freiroute.DTO.Plan.PlanRequestDto dto)
    {
        ViewData["Title"] = "Nuevo Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _planService.CreateAsync(dto, UsuarioId);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al crear el plan: {ex.Message}");
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Freiroute.DTO.Plan.PlanRequestDto dto)
    {
        ViewData["Title"] = "Editar Plan";
        ViewData["ActiveMenu"] = "admin-planes";
        ViewData["PlanId"] = id;
        
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _planService.UpdateAsync(id, dto, UsuarioId);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al actualizar el plan: {ex.Message}");
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await _planService.DeactivateAsync(id, UsuarioId);
            TempData["Success"] = "El plan ha sido desactivado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al desactivar el plan: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            await _planService.ReactivarAsync(id, UsuarioId);
            TempData["Success"] = "El plan ha sido reactivado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al reactivar el plan: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
