using Freiroute.DTO.Orden;
using Freiroute.Utility.Pagination;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio principal del módulo de órdenes (EP-04).
/// Incluye CRUD, cambio de estado (FSM — ADR-019), split, consolidación
/// y consultas. Todo método recibe empresaId extraído del JWT — nunca
/// del body del request (ADR-003). No existe DeleteAsync — solo soft
/// delete (ADR-005).
/// </summary>
public interface IOrdenService
{
    // ── CRUD ──────────────────────────────────────────────────

    /// <summary>
    /// Lista paginada de órdenes con filtros (HU-021 CA-14).
    /// Retorna PagedResult con badges de estado y transiciones disponibles.
    /// </summary>
    Task<PagedResult<OrdenListDto>> GetAllAsync(
        Guid empresaId, OrdenFiltroDto filtro);

    /// <summary>Obtiene una orden con sus líneas, labels y transiciones disponibles.</summary>
    Task<OrdenResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Crea una orden en estado DRAFT con numero_orden = NULL (HU-021).
    /// Valida: cliente_id, origen_id, destino_id, tipo_mercancia_id,
    /// unidad_medida_id obligatorios; peso_kg > 0; cantidad > 0;
    /// fecha_entrega >= fecha_pickup; origen != destino.
    /// </summary>
    Task<OrdenResponseDto> CreateAsync(OrdenRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Actualiza una orden solo si está en estado DRAFT o CONFIRMED
    /// (HU-021 CA-12). Reemplaza las líneas de detalle.
    /// </summary>
    Task<OrdenResponseDto> UpdateAsync(
        Guid id, OrdenRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Soft delete: activo = false (ADR-005). Solo permitido en
    /// estado DRAFT (HU-021 CA-13).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId, Guid usuarioId);

    // ── Estados (ADR-019) ────────────────────────────────────

    /// <summary>
    /// Cambia el estado de una orden validando la FSM (HU-024).
    /// En DRAFT → CONFIRMED genera numero_orden (ADR-020).
    /// Registra la transición en historial_estados_orden.
    /// </summary>
    Task<OrdenResponseDto> CambiarEstadoAsync(
        Guid id, CambiarEstadoOrdenRequestDto dto,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Retorna el historial de estados de una orden ordenado por
    /// fecha DESC (HU-024 CA-06).
    /// </summary>
    Task<IEnumerable<HistorialEstadoOrdenDto>> GetHistorialAsync(
        Guid ordenId, Guid empresaId);

    // ── Split (HU-026) ───────────────────────────────────────

    /// <summary>
    /// Divide una orden en múltiples sub-órdenes. Solo permitido en
    /// estados CONFIRMED o ASSIGNED. La suma de cantidad y peso debe
    /// igualar la orden original. La orden padre pasa a PARTIALLY_SPLIT.
    /// Cada sub-orden inicia en CONFIRMED con su propio ciclo FSM.
    /// </summary>
    Task<IEnumerable<OrdenResponseDto>> SplitAsync(
        Guid ordenId, SplitOrdenRequestDto dto,
        Guid empresaId, Guid usuarioId);

    // ── Consolidación (HU-025) ───────────────────────────────

    /// <summary>
    /// Consolida múltiples órdenes en un shipment. Solo órdenes
    /// CONFIRMED. Mínimo 2. Las órdenes pasan a ASSIGNED.
    /// Si shipmentId es null se crea un nuevo shipment PLANNED.
    /// </summary>
    Task<ShipmentResumenDto> ConsolidarAsync(
        ConsolidarOrdenesRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Desconsolida una orden: devuelve a CONFIRMED y limpia shipment_id.
    /// Solo si el shipment no tiene carrier asignado (PLANNED).
    /// </summary>
    Task<OrdenResponseDto> DesconsolidarAsync(
        Guid ordenId, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Lista todas las órdenes de un shipment (HU-025 CA-08).
    /// </summary>
    Task<IEnumerable<OrdenListDto>> GetByShipmentIdAsync(
        Guid shipmentId, Guid empresaId);
}
