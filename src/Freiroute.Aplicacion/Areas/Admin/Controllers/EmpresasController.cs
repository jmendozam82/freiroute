using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Gestión de empresas/tenants del panel Super Admin (HU-001).
/// Solo el SUPER_ADMIN puede registrar/gestionar empresas (CA-07).
/// Índice paginado (RNF-01.4: 20 por página) usando IEmpresaService.
/// </summary>
[Authorize(Roles = "SUPER_ADMIN")]
public class EmpresasController : BaseAdminController
{
    private readonly IEmpresaService _empresaService;
    private readonly IPlanService _planService;

    public EmpresasController(IEmpresaService empresaService, IPlanService planService)
    {
        _empresaService = empresaService;
        _planService = planService;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, string? estado = null, string? plan = null)
    {


        ViewData["Title"] = "Empresas";
        ViewData["ActiveMenu"] = "empresas";
        ViewData["Q"] = q;
        ViewData["Estado"] = estado;
        ViewData["Plan"] = plan;

        var todas = await _empresaService.GetAllAsync(incluirInactivos: true);

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(q))
        {
            todas = todas.Where(e =>
                e.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (e.RucNit?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                e.EmailAdmin.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            todas = todas.Where(e => string.Equals(e.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(plan))
        {
            todas = todas.Where(e => string.Equals(e.PlanSuscripcion, plan, StringComparison.OrdinalIgnoreCase));
        }

        var items = todas.ToList();
        const int pageSize = 20;
        var paged = new PagedResult<EmpresaResponseDto>
        {
            Items = items.Skip((page - 1) * pageSize).Take(pageSize),
            TotalItems = items.Count,
            PageNumber = Math.Max(1, page),
            PageSize = pageSize
        };

        return View(paged);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!EsSuperAdmin)
        {
            return Forbid();
        }

        ViewData["Title"] = "Nueva Empresa";
        ViewData["ActiveMenu"] = "empresas";

        var planes = _planService.GetAllAsync(soloActivos: true).GetAwaiter().GetResult();
        ViewBag.Planes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(planes, "Nombre", "Nombre");

        return View(new EmpresaRequestDto());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!EsSuperAdmin)
        {
            return Forbid();
        }

        var empresa = await _empresaService.GetByIdAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }

        var dto = new EmpresaRequestDto
        {
            Nombre = empresa.Nombre,
            RucNit = empresa.RucNit,
            EmailAdmin = empresa.EmailAdmin,
            Telefono = empresa.Telefono,
            Pais = empresa.Pais,
            Ciudad = empresa.Ciudad,
            Direccion = empresa.Direccion,
            PlanSuscripcion = empresa.PlanSuscripcion,
            MonedaPrincipal = empresa.MonedaPrincipal,
            ZonaHoraria = empresa.ZonaHoraria,
            ColorPrimario = empresa.ColorPrimario,
            ColorSecundario = empresa.ColorSecundario,
            LogoUrl = empresa.LogoUrl
        };

        ViewData["Title"] = "Editar Empresa";
        ViewData["ActiveMenu"] = "empresas";
        ViewData["EmpresaId"] = id;

        var planes = await _planService.GetAllAsync(soloActivos: true);
        ViewBag.Planes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(planes, "Nombre", "Nombre");

        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        if (!EsSuperAdmin)
        {
            return Forbid();
        }

        var empresa = await _empresaService.GetByIdAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Detalle de Empresa";
        ViewData["ActiveMenu"] = "empresas";
        return View(empresa);
    }
}
