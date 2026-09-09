using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;

namespace Freiroute.API.BackgroundJobs;

/// <summary>
/// Monitoreo de SLA para todas las empresas (HU-031 — CRON 0 0 * * *).
/// Registra las órdenes con SLA en riesgo/vencido por tenant para que el
/// equipo de operaciones pueda respirar datos frescos del dashboard.
/// Cross-tenant por diseño (AGENTS.md — regla 37).
/// Patrón: BackgroundService + PeriodicTimer (ADR-013).
/// </summary>
public class SlaMonitorJob : BackgroundService
{
    private readonly ILogger<SlaMonitorJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _intervalo;

    public SlaMonitorJob(
        ILogger<SlaMonitorJob> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var cadaHoras = configuration.GetValue("Jobs:Sla:CadaHoras", 24);
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
                var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();

                var empresas = await empresaRepository.GetAllAsync(incluirInactivos: false);

                foreach (var empresa in empresas)
                {
                    var enRiesgo = await slaService.GetEnRiesgoAsync(empresa.Id);
                    _logger.LogInformation(
                        "SlaMonitorJob: {Count} órdenes con SLA en riesgo/vencido en empresa {EmpresaId}",
                        enRiesgo.Count(), empresa.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SlaMonitorJob falló en esta iteración — se reintentará en la siguiente");
            }
        }
    }
}