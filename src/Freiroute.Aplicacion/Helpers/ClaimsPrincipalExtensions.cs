using System.Security.Claims;

namespace Freiroute.Aplicacion.Helpers;

/// <summary>
/// Extensiones de ClaimsPrincipal para las vistas Razor.
/// Permiten consultar permisos granulares ("modulo:accion"), roles y claims
/// del JWT de forma segura. Se usa junto con [RequirePermission] de la API.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Verifica si el usuario tiene el permiso "modulo:accion".
    /// El SUPER_ADMIN tiene acceso total implícito (no usa la tabla permisos).
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, string modulo, string accion)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("SUPER_ADMIN")) return true;

        var claim = $"{modulo}:{accion.ToLowerInvariant()}";
        return user.FindAll("permisos").Any(c => c.Value == claim);
    }

    /// <summary>
    /// Verifica si el usuario tiene al menos un permiso de cualquiera de los módulos indicados.
    /// Útil para mostrar secciones del sidebar que agrupan varios módulos.
    /// </summary>
    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] modulos)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("SUPER_ADMIN")) return true;

        var permisos = user.FindAll("permisos").Select(c => c.Value).ToHashSet();
        var acciones = new[] { "read", "create", "update" };

        return modulos.Any(m => acciones.Any(a => permisos.Contains($"{m}:{a}")));
    }

    /// <summary>
    /// Lee un claim concreto del JWT (ej: "nombre", "perfil_nombre", "empresa_id").
    /// Devuelve null si el claim no existe.
    /// </summary>
    public static string? FindFirstValue(this ClaimsPrincipal user, string claimType)
    {
        return user.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Indica si el usuario es Super Admin del SaaS.
    /// </summary>
    public static bool EsSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("SUPER_ADMIN");
    }

    /// <summary>
    /// Indica si el usuario es Admin de un tenant.
    /// </summary>
    public static bool EsAdminTenant(this ClaimsPrincipal user)
    {
        return user.IsInRole("ADMIN");
    }
}
