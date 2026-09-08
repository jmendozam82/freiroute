using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Freiroute.API.BackgroundJobs;

/// <summary>
/// Job de fondo que procesa las plantillas de órdenes recurrentes (HU-027 CA-06).
/// Ejecuta diariamente a las 00:05.
/// </summary>
public class RecurrenciaOrdenesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurrenciaOrdenesJob> _logger;
    private readonly TimeSpan _periodo;

    public RecurrenciaOrdenesJob(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<RecurrenciaOrdenesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var cadaHoras = configuration.GetValue<int?>("Jobs:Recurrencia:CadaHoras") ?? 24;
        _periodo = TimeSpan.FromHours(Math.Max(cadaHoras, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurrenciaOrdenesJob iniciado (cada {Horas} h)", _periodo.TotalHours);

        using var timer = new PeriodicTimer(_periodo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await EjecutarPasada(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en el procesamiento de órdenes recurrentes");
            }
        }
    }

    private async Task EjecutarPasada(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var plantillaService = scope.ServiceProvider.GetRequiredService<IPlantillaOrdenService>();

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        _logger.LogInformation("Procesando plantillas recurrentes para {Fecha}...", hoy);
        
        await plantillaService.ProcesarRecurrenciasPendientesAsync(hoy);
        
        _logger.LogInformation("Procesamiento de plantillas finalizado.");
    }
}
