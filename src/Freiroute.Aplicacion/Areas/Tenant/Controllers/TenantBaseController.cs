using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador base del área Tenant (Admin de tenant, Operador, Dispatcher).
/// Solo accesible para usuarios autenticados. Provee helpers de extracción
/// del empresa_id del JWT (claim ADR-007) para los servicios BLL.
/// </summary>
[Area("Tenant")]
[Authorize]
public abstract class TenantBaseController : Controller
{
    /// <summary>Obtiene el empresa_id del claim del JWT de la sesión (ADR-007).</summary>
    protected Guid EmpresaId =>
        Guid.TryParse(User.FindFirst("empresa_id")?.Value, out var id) ? id : Guid.Empty;

    /// <summary>Obtiene el usuario_id del claim del JWT de la sesión.</summary>
    protected Guid UsuarioId =>
        Guid.TryParse(User.FindFirst("user_id")?.Value, out var id) ? id : Guid.Empty;

    /// <summary>Indica si el usuario actual es Super Admin del SaaS.</summary>
    protected bool EsSuperAdmin => User.IsInRole("SUPER_ADMIN");
}