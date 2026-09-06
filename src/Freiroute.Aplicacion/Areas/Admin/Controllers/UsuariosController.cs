using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Freiroute.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Gestión de usuarios por tenant (HU-005). Vista de listado mínima para
/// el panel Admin; la creación/edición viaja por API /api/usuarios.
/// </summary>
public class UsuariosController : BaseAdminController
{
    private readonly IPerfilService _perfilService;

    public UsuariosController(IPerfilService perfilService)
    {
        _perfilService = perfilService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Usuarios";
        ViewData["ActiveMenu"] = "usuarios";
        
        var perfiles = await _perfilService.GetAllAsync(EmpresaId);
        ViewData["Perfiles"] = perfiles;
        
        return View();
    }
}
