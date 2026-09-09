using Freiroute.DTO.Reclamo;
using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de reclamos (HU-032).
/// Incluye CRUD, FSM de estados, historial, numeración (REC-...),
/// reportes y consultas por cliente. Corresponde a las tablas
/// 'reclamos' e 'historial_estados_reclamo' (INSERT-only).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT (ADR-003).
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IReclamoRepository
{
    /// <summary>Crea el reclamo. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(Reclamo entity);

    /// <summary>Obtiene un reclamo por Id dentro de la empresa.</summary>
    Task<Reclamo?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Lista paginada de reclamos con filtros por estado, tipo y rango
    /// de fechas. Retorna los ítems (JOIN con ordenes y clientes para
    /// nombres legibles) y el total de registros sin paginar.
    /// </summary>
    Task<(IEnumerable<Reclamo> Items, int Total)> GetAllAsync(
        Guid empresaId, ReclamoFiltroDto filtro);

    /// <summary>
    /// Actualiza el estado del reclamo (transición FSM validada antes
    /// en BLL). Persiste modificado_por y fecha_modificacion.
    /// </summary>
    Task UpdateEstadoAsync(Guid id, Guid empresaId, string estadoNuevo,
        Guid modificadoPor, DateTime fechaModificacion);

    /// <summary>Lista los reclamos de un cliente (via ordenes.cliente_id).</summary>
    Task<IEnumerable<Reclamo>> GetByClienteAsync(Guid clienteId, Guid empresaId);

    /// <summary>Reporte agregado por tipo/estado en el período (HU-032 CA-09).</summary>
    Task<IEnumerable<ReclamoReporteItemDto>> GetReporteAsync(
        Guid empresaId, DateTime desde, DateTime hasta);

    /// <summary>Registra una entrada en historial_estados_reclamo (INSERT-only).</summary>
    Task InsertHistorialAsync(HistorialEstadoReclamo historial);

    /// <summary>Obtiene el historial de estados de un reclamo ordenado por fecha DESC.</summary>
    Task<IEnumerable<HistorialEstadoReclamo>> GetHistorialAsync(
        Guid reclamoId, Guid empresaId);

    /// <summary>
    /// Genera el número de reclamo atómico usando la función PostgreSQL
    /// generar_numero_reclamo() (patrón ADR-020, Opción B: contadores_reclamo).
    /// Formato REC-{PREFIJO}-{AÑO}-{NNNN}. Llamada al crear el reclamo.
    /// </summary>
    Task<string> GenerarNumeroReclamoAsync(Guid empresaId, string prefijo, int anio);

    /// <summary>Obtiene el número legible generado del reclamo.</summary>
    Task<string?> GetNumeroReclamoAsync(Guid id, Guid empresaId);

    /// <summary>Persiste el número legible generado (REC-{PREFIX}-{AÑO}-{SECUENCIA}).</summary>
    Task UpdateNumeroReclamoAsync(Guid id, Guid empresaId, string numero);
}