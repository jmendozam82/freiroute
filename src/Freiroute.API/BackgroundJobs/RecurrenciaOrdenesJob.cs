using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.Utility.Constants;
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

    public RecurrenciaOrdenesJob(
        IServiceProvider serviceProvider,
        ILogger<RecurrenciaOrdenesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurrenciaOrdenesJob iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Calcular tiempo hasta las 00:05 del día siguiente
            var nextRun = now.Date.AddDays(1).AddMinutes(5);
            var delay = nextRun - now;

            _logger.LogInformation("Próxima ejecución de RecurrenciaOrdenesJob: {NextRun}", nextRun);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EjecutarPasada(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el procesamiento de órdenes recurrentes");
                }
            }
        }
    }

    private async Task EjecutarPasada(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        // We need to fetch all active templates that have ProximaEjecucion <= today.
        // Wait, since this is a background job without tenant context, we need a cross-tenant query.
        // However, IPlantillaOrdenRepository methods require empresaId.
        // We will need to get all empresas, and then iterate.
        var empresaRepository = scope.ServiceProvider.GetRequiredService<IEmpresaRepository>();
        var plantillaRepository = scope.ServiceProvider.GetRequiredService<IPlantillaOrdenRepository>();
        var plantillaService = scope.ServiceProvider.GetRequiredService<IPlantillaOrdenService>();

        var empresas = await empresaRepository.GetAllAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var empresa in empresas)
        {
            try
            {
                var plantillas = await plantillaRepository.GetPlantillasParaEjecutarAsync(empresa.Id, hoy);

                foreach (var plantilla in plantillas)
                {
                    try
                    {
                        // Crear orden
                        await plantillaService.CrearOrdenDesdePlantillaAsync(plantilla.Id, empresa.Id, Guid.Empty); // Using Guid.Empty for System user

                        // Actualizar proxima_ejecucion
                        DateOnly proxima = plantilla.FrecuenciaRecurrencia switch
                        {
                            "DIARIA" => hoy.AddDays(1),
                            "SEMANAL" => hoy.AddDays(7),
                            "QUINCENAL" => hoy.AddDays(15),
                            "MENSUAL" => hoy.AddMonths(1),
                            _ => hoy.AddDays(1)
                        };

                        await plantillaRepository.ActualizarProximaEjecucionAsync(plantilla.Id, proxima, empresa.Id);
                        _logger.LogInformation("Orden recurrente creada para plantilla {PlantillaId} en empresa {EmpresaId}", plantilla.Id, empresa.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar plantilla recurrente {PlantillaId}", plantilla.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando plantillas para empresa {EmpresaId}", empresa.Id);
            }
        }
    }
}
