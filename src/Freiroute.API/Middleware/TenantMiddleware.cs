using System.Data;
using Dapper;
using Freiroute.DTO.Empresa;
using Freiroute.Entity;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Freiroute.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDbConnection db)
    {
        // Extraer el claim 'empresa_id' del JWT
        var empresaClaim = context.User?.FindFirst("empresa_id")?.Value;

        if (!string.IsNullOrEmpty(empresaClaim))
        {
            try
            {
                // Inyectar en la sesión de PostgreSQL para que RLS lo use
                await db.ExecuteAsync(
                    "SELECT set_config('app.current_empresa_id', @val, true)",
                    new { val = empresaClaim });
                
                Log.Debug("Inyección de tenant aplicada: {@EmpresaId}", empresaClaim);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al aplicar RLS para empresa {EmpresaId}", empresaClaim);
            }
        }
        else
        {
            // Si es SuperAdmin o endpoints públicos, se permite pasar sin filtro explícito
            _logger.LogDebug("No se encontró empresa_id en JWT. Endpoint puede requerir acceso global.");
        }

        await _next(context);
    }
}
