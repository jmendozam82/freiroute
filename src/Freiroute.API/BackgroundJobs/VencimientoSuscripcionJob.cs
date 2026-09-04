using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Freiroute.API.BackgroundJobs;

/// <summary>
/// Job de fondo que procesa los vencimientos de suscripciones (HU-011 CA-05/06).
/// Ejecuta ISuscripcionService.ProcesarVencimientosAsync de forma periódica
/// (diaria por defecto, configurable en appsettings como "Jobs:Vencimiento:CadaHoras").
/// ACTIVE vencida → PAST_DUE; PAST_DUE > 7 días → SUSPENDED.
/// </summary>
public class VencimientoSuscripcionJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VencimientoSuscripcionJob> _logger;
    private readonly TimeSpan _periodo;

    public VencimientoSuscripcionJob(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<VencimientoSuscripcionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var cadaHoras = configuration.GetValue<int?>("Jobs:Vencimiento:CadaHoras") ?? 24;
        _periodo = TimeSpan.FromHours(Math.Max(cadaHoras, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VencimientoSuscripcionJob iniciado (cada {Horas} h)", _periodo.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarPasada(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el procesamiento de vencimientos de suscripciones");
            }

            await Task.Delay(_periodo, stoppingToken);
        }
    }

    private async Task EjecutarPasada(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var suscripcionService = scope.ServiceProvider.GetRequiredService<ISuscripcionService>();

        _logger.LogInformation("Procesando vencimientos de suscripciones...");
        await suscripcionService.ProcesarVencimientosAsync();
        _logger.LogInformation("Procesamiento de vencimientos finalizado.");
    }
}
