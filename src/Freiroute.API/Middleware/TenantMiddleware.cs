using System.Data;
using System.Security.Claims;
using Dapper;
using Freiroute.API.Extensions;
using Freiroute.Utility.Constants;
using Serilog;

namespace Freiroute.API.Middleware;

/// <summary>
/// Middleware de resolución de tenant por request (multi-tenant ADR-003/007).
/// Se ejecuta DESPUÉS de UseAuthentication() para que las claims ya existan.
///
/// Responsabilidades:
///  1. Rutas públicas excluidas (/api/auth/*, /swagger, /health) pasan sin resolver tenant.
///  2. Usuario autenticado NO super-admin SIN claim empresa_id → 401 (token mal formado).
///  3. SUPER_ADMIN: no tiene empresa_id en el token; puede actuar sobre un tenant
///     concreto vía header X-Empresa-Id (URLs con tenant en el path, así lo usará el TreeCRUD).
///  4. La empresa resuelta se inyecta en la sesión de PostgreSQL
///     (set_config 'app.current_empresa_id') para que RLS aísle los datos (ADR-003).
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 1. Rutas públicas excluidas (login/refresh/forgot/reset/health/swagger).
        if (EsRutaExcluida(path))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        var empresaClaim = user.FindFirst("empresa_id")?.Value;
        var tipoUsuario = user.FindFirst(ClaimTypes.Role)?.Value
                          ?? user.FindFirst("tipo_usuario")?.Value;

        // 2. Usuario autenticado sin tenant → token inválido/incompleto (401).
        if (user.Identity?.IsAuthenticated == true &&
            string.IsNullOrEmpty(empresaClaim) &&
            tipoUsuario != TipoUsuario.SUPER_ADMIN)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "El token no contiene empresa_id válido."
            });
            return;
        }

        // 3. Resolución final de la empresa del request.
        string? empresaResuelta = empresaClaim;
        if (tipoUsuario == TipoUsuario.SUPER_ADMIN)
        {
            // El Super Admin puede operar sobre un tenant vía header explícito.
            var header = context.Request.Headers["X-Empresa-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(header))
            {
                empresaResuelta = header;
            }
        }

        // 4. Inyectar el tenant en la sesión de PostgreSQL para RLS (ADR-003).
        if (!string.IsNullOrEmpty(empresaResuelta) && Guid.TryParse(empresaResuelta, out _))
        {
            await AplicarContextoTenantAsync(context, empresaResuelta);
        }
        else if (user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning(
                "Tenant sin resolver para {Ruta} (usuario autenticado)", path);
        }

        await _next(context);
    }

    private async Task AplicarContextoTenantAsync(HttpContext context, string empresaId)
    {
        // La conexión Npgsql es scoped (registrada en IOC); si la app la registra,
        // se inyecta el tenant por request. Si un test reemplaza la conexión y no
        // la resuelve, se omite sin romper el flujo.
        var db = context.RequestServices.GetService<IDbConnection>();
        if (db is null)
        {
            return;
        }

        try
        {
            await db.ExecuteAsync(
                "SELECT set_config('app.current_empresa_id', @val, true)",
                new { val = empresaId });

            Log.Debug("RLS: tenant {EmpresaId} inyectado en la sesión SQL", empresaId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo inyectar el tenant {EmpresaId} para RLS", empresaId);
        }
    }

    private static bool EsRutaExcluida(string path)
    {
        return path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/", StringComparison.OrdinalIgnoreCase);
    }
}