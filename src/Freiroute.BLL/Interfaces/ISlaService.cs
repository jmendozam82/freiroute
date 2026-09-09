using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de monitoreo de SLA por cliente (HU-031).
/// El estado de SLA se calcula en BLL — no viene de BD directamente.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT (ADR-003).
/// </summary>
public interface ISlaService
{
    /// <summary>
    /// Lista órdenes en riesgo de SLA: fecha_entrega_requerida dentro de
    /// las próximas 24h y estado anterior a IN_TRANSIT (HU-031 CA-02 —
    /// GET /api/ordenes/sla-en-riesgo).
    /// </summary>
    Task<IEnumerable<OrdenListDto>> GetEnRiesgoAsync(Guid empresaId);

    /// <summary>
    /// Cumplimiento SLA de un cliente: % de órdenes DELIVERED a tiempo en
    /// los últimos 30 días (HU-031 CA-03 — GET /api/clientes/{id}/sla-cumplimiento).
    /// </summary>
    Task<SlaClienteResponseDto> GetCumplimientoClienteAsync(
        Guid clienteId, Guid empresaId);

    /// <summary>
    /// Reporte de cumplimiento SLA por cliente en el período configurable
    /// (HU-031 CA-07 — GET /api/reportes/sla).
    /// </summary>
    Task<IEnumerable<SlaReporteItemDto>> GetReporteSlaAsync(
        Guid empresaId, DateTime desde, DateTime hasta);

    /// <summary>
    /// Helper puro para calcular el estado de SLA de una orden (HU-031
    /// CA-04/CA-05/CA-06): OK | EN_RIESGO | CRITICO | VENCIDO.
    /// Estados finales (DELIVERED/CLOSED/CANCELLED) siempre retornan OK.
    /// </summary>
    string CalcularSlaStatus(DateTime? fechaEntregaRequerida, string estadoOrden);
}