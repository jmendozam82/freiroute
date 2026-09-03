using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Freiroute.API.Attributes;

/// <summary>
/// Atributo de autorización por permisos del módulo (HU-006, ADR-007/009).
/// Uso: [RequirePermission("embarques", PermissionType.Create)]
///
/// Reglas:
///  1. El SUPER_ADMIN SIEMPRE tiene acceso total (claim tipo_usuario = SUPER_ADMIN).
///  2. El usuario debe tener el claim "permisos" = "{modulo}:{tipo.ToLower()}"
///     (ej: "embarques:create"). Se usa User.FindAll para soportar múltiples claims.
///  3. Sin el permiso → 403 Forbid (no 401 — el usuario SÍ está autenticado).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _modulo;
    private readonly PermissionType _tipo;

    public RequirePermissionAttribute(string modulo, PermissionType tipo)
    {
        _modulo = modulo;
        _tipo = tipo;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // 1. Bypass total para SUPER_ADMIN.
        var tipoUsuario = user.FindFirst("tipo_usuario")?.Value;
        if (tipoUsuario == TipoUsuario.SUPER_ADMIN)
        {
            return;
        }

        // 2. Claim de permiso: formato "{modulo}:{read|create|update}".
        var permisoRequerido = $"{_modulo.ToLowerInvariant()}:{_tipo.ToString().ToLowerInvariant()}";
        var permisos = user.FindAll("permisos").Select(c => c.Value);

        if (!permisos.Contains(permisoRequerido))
        {
            context.Result = new ObjectResult(
                ApiResponse<string>.Fail("No tiene el permiso requerido para esta operación."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}