using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Gestión de empresas/tenants del panel Super Admin (HU-001).
/// Solo el SUPER_ADMIN puede registrar/gestionar empresas (CA-07).
/// Índice paginado (RNF-01.4: 20 por página) usando IEmpresaService.
/// </summary>
public class EmpresasController : BaseAdminController
{
    private readonly IEmpresaService _empresaService;

    public EmpresasController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, string? estado = null)
    {
        if (!EsSuperAdmin)
        {
            return Forbid();
        }

        ViewData["Title"] = "Empresas";
        ViewData["ActiveMenu"] = "empresas";
        ViewData["Q"] = q;
        ViewData["Estado"] = estado;

        var todas = await _empresaService.GetAllAsync();

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
        return View(new EmpresaRequestDto());
    }
}
