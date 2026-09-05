using Freiroute.API.Extensions;
using Freiroute.DAL.Interfaces;

namespace Freiroute.API.Middleware;

/// <summary>
/// Middleware de redirección del onboarding (HU-012, ADR-010).
/// Si un tenant autenticado NO ha completado el wizard de onboarding, se le
/// redirige al primer paso para garantizar la configuración mínima antes de
/// operar el TMS (CA-01).
///
/// Es CONSERVADOR para no interferir con las APIs JSON:
///   - Solo actúa sobre peticiones GET de navegador (Accept: text/html).
///   - Omite rutas de onboarding/auth/recursos estáticos.
///   - Si no hay claims o el tenant ya completó el onboarding, continúa normal.
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

        // 1. Solo navegación (Accept text/html) — las APIs JSON siguen sin redirigir.
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
        var empresaId = context.User.GetTenantEfectivo(context);
        if (empresaId == Guid.Empty || !context.User.Identity?.IsAuthenticated == true)
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
        // Fix re-smoke test: ruta por path (/onboarding/paso/1) coherente con el
        // JS de las vistas del wizard — antes usaba query param (?paso=1).
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
            || p.StartsWith("/favicon")
            || p.StartsWith("/health")
            || p.StartsWith("/swagger");
    }
}
