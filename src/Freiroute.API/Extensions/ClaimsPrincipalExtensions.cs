using System.Security.Claims;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Http;

namespace Freiroute.API.Extensions;

/// <summary>
/// Extensiones de ClaimsPrincipal para extraer de forma tipada las claims del
/// JWT de Freiroute (ADR-007): user_id, empresa_id, perfil_id, tipo_usuario.
/// Uso: en los controllers, User.GetEmpresaId() → Guid del tenant autenticado.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>user_id del JWT. Lanza si no existe (token inválido).</summary>
    public static Guid GetUsuarioId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("user_id")?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>Alias del spec Sprint 1 — GetUserId() ≡ GetUsuarioId().</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) => user.GetUsuarioId();

    /// <summary>empresa_id del JWT. Guid.Empty para SUPER_ADMIN (sin tenant propio).</summary>
    public static Guid GetEmpresaId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("empresa_id")?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>perfil_id del JWT.</summary>
    public static Guid GetPerfilId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("perfil_id")?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>tipo_usuario del JWT (SUPER_ADMIN|ADMIN|OPERADOR|...).</summary>
    public static string GetTipoUsuario(this ClaimsPrincipal user) =>
        user.FindFirst("tipo_usuario")?.Value ?? string.Empty;

    /// <summary>True si el usuario es SUPER_ADMIN (acceso global al SaaS).</summary>
    public static bool IsSuperAdmin(this ClaimsPrincipal user) =>
        user.GetTipoUsuario() == TipoUsuario.SUPER_ADMIN;

    /// <summary>Nombre del usuario (claim 'nombre').</summary>
    public static string GetNombre(this ClaimsPrincipal user) =>
        user.FindFirst("nombre")?.Value ?? string.Empty;

    /// <summary>Todos los permisos del usuario en formato "modulo:accion" (claims múltiples "permisos").</summary>
    public static IEnumerable<string> GetPermisos(this ClaimsPrincipal user) =>
        user.FindAll("permisos").Select(c => c.Value);

    /// <summary>IP remota del request actual (IPAddress of HttpContext.Connection).</summary>
    public static string? GetIpAddress(this ClaimsPrincipal user, IHttpContextAccessor accessor)
    {
        var ip = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? null : ip;
    }

    /// <summary>Resuelve el tenant efectivo: empresa_id token, o header X-Empresa-Id para SUPER_ADMIN.</summary>
    public static Guid GetTenantEfectivo(this ClaimsPrincipal user, HttpContext httpContext)
    {
        var empresa = user.GetEmpresaId();
        if (empresa == Guid.Empty && httpContext.Request.Headers.TryGetValue("X-Empresa-Id", out var header))
        {
            return Guid.TryParse(header.ToString(), out var headerId) ? headerId : Guid.Empty;
        }

        return empresa;
    }
}