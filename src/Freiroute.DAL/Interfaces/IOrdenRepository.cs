using Freiroute.Entity;
using Freiroute.Utility.Pagination;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos del módulo de órdenes de transporte (EP-04).
/// Incluye CRUD, paginación, generación de número de orden (ADR-020),
/// split, consolidación y consultas por shipment.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IOrdenRepository
{
    /// <summary>
    /// Lista órdenes paginadas con filtros opcionales por cliente,
    /// estado, modo de transporte, prioridad, etc.
    /// </summary>
    Task<PagedResult<Orden>> GetAllAsync(
        Guid empresaId, int page, int pageSize,
        string? clienteId = null, string? estado = null,
        string? modoTransporte = null, string? nivelServicio = null,
        string? prioridad = null, string? origenCreacion = null,
        string? shipmentId = null, string? q = null,
        DateOnly? fechaPickupDesde = null, DateOnly? fechaPickupHasta = null,
        DateOnly? fechaEntregaDesde = null, DateOnly? fechaEntregaHasta = null,
        bool? esSplit = null);

    /// <summary>Obtiene una orden por Id dentro de la empresa.</summary>
    Task<Orden?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene las líneas de detalle de una orden.</summary>
    Task<IEnumerable<LineaOrden>> GetLineasAsync(Guid ordenId, Guid empresaId);

    /// <summary>Crea la orden. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(Orden entidad);

    /// <summary>Crea una línea de orden. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateLineaAsync(LineaOrden entidad);

    /// <summary>Elimina todas las líneas de una orden (para recrearlas en Update).</summary>
    Task<bool> DeleteLineasAsync(Guid ordenId, Guid empresaId);

    /// <summary>Actualiza la orden. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(Orden entidad);

    /// <summary>Soft delete: activo = false (ADR-005). Solo en estado DRAFT.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Genera el número de orden atómico usando la función PostgreSQL
    /// generar_numero_orden() (ADR-020). Llamada exclusivamente en
    /// DRAFT → CONFIRMED.
    /// </summary>
    Task<string> GenerarNumeroOrdenAsync(Guid empresaId, string prefijo, int anio);

    /// <summary>
    /// Actualiza el estado de una orden. Retorna true si afectó una fila.
    /// Debe usarse dentro de una transacción con historial_estados.
    /// </summary>
    Task<bool> ActualizarEstadoAsync(Guid id, string estadoNuevo, Guid empresaId);

    /// <summary>Registra una entrada en el historial de estados (ADR-019).</summary>
    Task<bool> RegistrarHistorialEstadoAsync(HistorialEstadoOrden historial);

    /// <summary>Obtiene el historial de estados de una orden ordenado por fecha DESC.</summary>
    Task<IEnumerable<HistorialEstadoOrden>> GetHistorialAsync(
        Guid ordenId, Guid empresaId);

    /// <summary>
    /// Actualiza el shipment de una orden. Si shipmentId es null, limpia
    /// la referencia. Usado en consolidación y desconsolidación (HU-025).
    /// </summary>
    Task<bool> AsignarShipmentAsync(
        Guid ordenId, Guid? shipmentId, string estadoNuevo, Guid empresaId);

    /// <summary>Obtiene todas las órdenes de un shipment.</summary>
    Task<IEnumerable<Orden>> GetByShipmentIdAsync(
        Guid shipmentId, Guid empresaId);

    /// <summary>Obtiene las sub-órdenes de una orden padre (split HU-026).</summary>
    Task<IEnumerable<Orden>> GetSubOrdenesAsync(
        Guid ordenOrigenId, Guid empresaId);

    /// <summary>Crea múltiples órdenes en lote (importación CSV HU-022).</summary>
    Task<IEnumerable<Guid>> CreateBulkAsync(IEnumerable<Orden> ordenes);

    /// <summary>Crea múltiples líneas en lote.</summary>
    Task CreateLineasBulkAsync(IEnumerable<LineaOrden> lineas);
}
