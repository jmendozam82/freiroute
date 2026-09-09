using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;

namespace Freiroute.API.BackgroundJobs;

/// <summary>
/// Elevación automática de prioridades por reglas activas del tenant
/// (HU-029 CA-08 — CRON 0 */6 * * *). Es cross-tenant: recorre TODAS las
/// empresas registradas en el SaaS, por eso consulta sin filtro de
/// empresa_id (AGENTS.md — regla 37).
/// Patrón: BackgroundService + PeriodicTimer (ADR-013). Nunca propaga
/// excepciones — una iteración fallida no detiene las siguientes.
/// </summary>
public class PrioridadOrdenesJob : BackgroundService
{
    private readonly ILogger<PrioridadOrdenesJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _intervalo;

    public PrioridadOrdenesJob(
        ILogger<PrioridadOrdenesJob> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var cadaHoras = configuration.GetValue("Jobs:Prioridad:CadaHoras", 6);
        _intervalo = TimeSpan.FromHours(cadaHoras);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var empresaRepository = scope.ServiceProvider.GetRequiredService<IEmpresaRepository>();
                var servicio = scope.ServiceProvider.GetRequiredService<IPrioridadOrdenService>();

                var empresas = await empresaRepository.GetAllAsync(incluirInactivos: false);

                foreach (var empresa in empresas)
                {
                    var elevadas = await servicio.ElevarPrioridadesAutomaticasAsync(empresa.Id);
                    if (elevadas > 0)
                    {
                        _logger.LogInformation(
                            "PrioridadOrdenesJob: {Count} órdenes elevadas en empresa {EmpresaId}",
                            elevadas, empresa.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "PrioridadOrdenesJob falló en esta iteración — se reintentará en la siguiente");
            }
        }
    }
}