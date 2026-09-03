using Freiroute.Aplicacion.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

/// <summary>
/// Gestión de usuarios por tenant (HU-005). Vista de listado mínima para
/// el panel Admin; la creación/edición viaja por API /api/usuarios.
/// </summary>
public class UsuariosController : BaseAdminController
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Usuarios";
        ViewData["ActiveMenu"] = "usuarios";
        return View();
    }
}
