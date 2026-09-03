using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Perfil;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Gestión de perfiles/roles por tenant (HU-006). El SUPER_ADMIN administra
/// los perfiles del SaaS global; el ADMIN los de su empresa. Índice paginado.
/// </summary>
public class PerfilesController : BaseAdminController
{
    private readonly IPerfilService _perfilService;
    private readonly IPermisoService _permisoService;

    public PerfilesController(IPerfilService perfilService, IPermisoService permisoService)
    {
        _perfilService = perfilService;
        _permisoService = permisoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? q = null, string? tipo = null)
    {
        ViewData["Title"] = "Perfiles";
        ViewData["ActiveMenu"] = "perfiles";
        ViewData["Q"] = q;
        ViewData["Tipo"] = tipo;

        // SUPER_ADMIN ve los perfiles del SaaS (empresa_id del claim de su sesión);
        // el ADMIN solo los de su tenant.
        Guid empresaBuscada = EsSuperAdmin ? EmpresaId : EmpresaId;

        IEnumerable<PerfilResponseDto> perfiles = await _perfilService.GetAllAsync(empresaBuscada);

        if (!string.IsNullOrWhiteSpace(q))
        {
            perfiles = perfiles.Where(p =>
                p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.Descripcion?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            perfiles = perfiles.Where(p => string.Equals(p.TipoPerfil, tipo, StringComparison.OrdinalIgnoreCase));
        }

        var items = perfiles.ToList();
        const int pageSize = 20;
        var paged = new PagedResult<PerfilResponseDto>
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
        ViewData["Title"] = "Nuevo Perfil";
        ViewData["ActiveMenu"] = "perfiles";
        return View(new PerfilRequestDto());
    }

    [HttpGet]
    public async Task<IActionResult> Permisos(Guid id)
    {
        ViewData["Title"] = "Permisos del Perfil";
        ViewData["ActiveMenu"] = "perfiles";

        var perfil = await _perfilService.GetByIdAsync(id, EmpresaId);
        if (perfil is null)
        {
            return NotFound();
        }

        var permisos = await _permisoService.GetByPerfilAsync(id, EmpresaId);
        ViewData["Perfil"] = perfil;
        return View(permisos);
    }
}
