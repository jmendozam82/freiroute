using Freiroute.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace Freiroute.Aplicacion.Middleware;

/// <summary>
/// Middleware de redirección del onboarding (HU-012, ADR-010).
/// Si un tenant autenticado NO ha completado el wizard de onboarding, se le
/// redirige al primer paso para garantizar la configuración mínima antes de
/// operar el TMS (CA-01).
/// </summary>
public class OnboardingRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public OnboardingRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IEmpresaRepository empresaRepository)
    {
        var request = context.Request;

        // 1. Solo navegación (Accept text/html)
        var accept = request.Headers.Accept.ToString();
        if (request.Method != HttpMethods.Get || !accept.Contains("text/html"))
        {
            await _next(context);
            return;
        }

        // 2. Omite rutas que no requieren onboarding.
        if (EsRutaExenta(request.Path))
        {
            await _next(context);
            return;
        }

        // 3. Requiere usuario autenticado con tenant resuelto.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var empresaIdClaim = context.User.FindFirst("empresa_id")?.Value;
        if (!Guid.TryParse(empresaIdClaim, out var empresaId) || empresaId == Guid.Empty)
        {
            await _next(context);
            return;
        }

        // 4. Si el tenant ya completó el onboarding, continuar.
        var empresa = await empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null || empresa.OnboardingCompletado)
        {
            await _next(context);
            return;
        }

        // 5. Redirigir al paso 1 del wizard.
        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
        context.Response.Headers.Location = "/onboarding/paso/1";
        await context.Response.CompleteAsync();
    }

    private static bool EsRutaExenta(PathString path)
    {
        var p = path.ToString().ToLowerInvariant();
        return p.StartsWith("/onboarding")
            || p.StartsWith("/auth")
            || p.StartsWith("/css")
            || p.StartsWith("/js")
            || p.StartsWith("/lib")
            || p.StartsWith("/assets")
            || p.StartsWith("/favicon");
    }
}
