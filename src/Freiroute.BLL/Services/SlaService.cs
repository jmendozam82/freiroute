using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Mappings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Orders;

namespace Freiroute.BLL.Services;

/// <summary>
/// Monitoreo de SLA por cliente (HU-031). El estado de SLA se calcula en
/// BLL (SlaCalculator) — no viene de BD directamente (CA-04/CA-05/CA-06).
/// </summary>
public class SlaService : ISlaService
{
    private readonly IOrdenRepository _ordenRepository;
    private readonly IClienteRepository _clienteRepository;

    public SlaService(IOrdenRepository ordenRepository, IClienteRepository clienteRepository)
    {
        _ordenRepository = ordenRepository;
        _clienteRepository = clienteRepository;
    }

    /// <summary>
    /// Órdenes con SLA en riesgo: entrega requerida en las próximas 24 h
    /// o ya vencida, con estado no terminal (HU-031 CA-02).
    /// </summary>
    public async Task<IEnumerable<OrdenListDto>> GetEnRiesgoAsync(Guid empresaId)
    {
        var ordenes = await _ordenRepository.GetSlaEnRiesgoAsync(empresaId);
        return ordenes.Select(OrdenMapper.ToListDto);
    }

    /// <summary>
    /// Cumplimiento SLA de un cliente: % de órdenes DELIVERED a tiempo en
    /// los últimos 30 días (HU-031 CA-03 — GET /api/clientes/{id}/sla-cumplimiento).
    /// </summary>
    public async Task<SlaClienteResponseDto> GetCumplimientoClienteAsync(
        Guid clienteId, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, empresaId)
            ?? throw new BusinessException("Cliente no encontrado");

        var hasta = DateTime.UtcNow;
        var desde = hasta.AddDays(-30);

        var (totalOrdenes, ordenesATiempo) =
            await _ordenRepository.GetSlaClienteAsync(clienteId, empresaId, desde, hasta);

        var ordenesTardias = totalOrdenes - ordenesATiempo;
        var porcentaje = totalOrdenes > 0
            ? Math.Round(ordenesATiempo * 100m / totalOrdenes, 2)
            : 0m;

        return new SlaClienteResponseDto
        {
            ClienteId = cliente.Id,
            ClienteNombre = cliente.Nombre,
            TotalOrdenes = totalOrdenes,
            OrdenesATiempo = ordenesATiempo,
            OrdenesTardias = ordenesTardias,
            PorcentajeCumplimiento = porcentaje,
            SlaDias = cliente.SlaDiasEntrega ?? 30
        };
    }

    /// <summary>
    /// Reporte de cumplimiento SLA por cliente en el período configurable
    /// (HU-031 CA-07 — GET /api/reportes/sla).
    /// El repositorio devuelve los DTOs listos (JOIN clientes).
    /// </summary>
    public async Task<IEnumerable<SlaReporteItemDto>> GetReporteSlaAsync(
        Guid empresaId, DateTime desde, DateTime hasta) =>
        await _ordenRepository.GetSlaReporteAsync(empresaId, desde, hasta);

    /// <summary>
    /// Helper puro — delega en SlaCalculator (única fuente de verdad de
    /// umbrales 24 h / 6 h, HU-031 CA-04/CA-05/CA-06).
    /// </summary>
    public string CalcularSlaStatus(DateTime? fechaEntregaRequerida, string estadoOrden) =>
        SlaCalculator.Calcular(fechaEntregaRequerida, estadoOrden);
}