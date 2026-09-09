using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de gestión de rechazos de entrega y re-entregas (HU-030).
/// El registro del rechazo mueve la orden a FAILED_DELIVERY vía FSM
/// (ADR-019) y la re-entrega crea una orden nueva CONFIRMED vinculada
/// por orden_origen_id. Todo método recibe <paramref name="empresaId"/>
/// extraído del JWT (ADR-003).
/// </summary>
public interface IRechazoEntregaService
{
    /// <summary>
    /// Registra el rechazo con motivo obligatorio (HU-030 CA-01/CA-02),
    /// mueve la orden a FAILED_DELIVERY (CA-03) y audita RECHAZO_ENTREGA (CA-11).
    /// </summary>
    Task<RechazoEntregaResponseDto> RegistrarRechazoAsync(
        Guid ordenId, RechazoEntregaRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Crea la re-entrega heredando cliente, origen, destino, tipo de
    /// mercancía y unidad de medida de la orden original (HU-030 CA-05).
    /// Inicia en CONFIRMED (CA-07) con orden_origen_id a la orden fallida
    /// (CA-06) y audita CREAR_REENTREGA (CA-11).
    /// </summary>
    Task<OrdenResponseDto> CrearReentregaAsync(
        Guid ordenId, ReEntregaRequestDto? dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Historial de re-entregas de una orden (HU-030 CA-10 —
    /// GET /api/ordenes/{id}/re-entregas). Lista las órdenes cuyo
    /// orden_origen_id apunta a la orden consultada.
    /// </summary>
    Task<IEnumerable<OrdenListDto>> GetReentregasAsync(Guid ordenId, Guid empresaId);
}