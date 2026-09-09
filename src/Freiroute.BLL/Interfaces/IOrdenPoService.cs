using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de integración de órdenes de compra/venta (HU-028 — PO Integration).
/// Una PO puede vincularse a múltiples órdenes de transporte (relación 1:N).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT (ADR-003).
/// </summary>
public interface IOrdenPoService
{
    /// <summary>
    /// Lista todas las órdenes con un número de PO exacto del mismo tenant
    /// (HU-028 CA-05 — GET /api/ordenes/por-po/{numero}).
    /// </summary>
    Task<IEnumerable<OrdenListDto>> GetPorPoAsync(string numeroPo, Guid empresaId);

    /// <summary>
    /// Registra en auditoría el vínculo o cambio del número de PO de una
    /// orden (HU-028 CA-09 — accion VINCULAR_PO con detalles JSON).
    /// </summary>
    Task AuditarCambioPo(Guid ordenId, string? numeroPo, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Vincula o actualiza el PO/SO de una orden (HU-028 — PATCH
    /// /api/ordenes/{id}/po). Persiste numero_po/numero_so y registra
    /// auditoría VINCULAR_PO con los valores nuevos.
    /// </summary>
    Task<OrdenResponseDto> VincularPoAsync(
        Guid ordenId, OrdenPoRequestDto dto, Guid empresaId, Guid usuarioId);
}