using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de tarifas base y recargos (HU-020, ADR-015).
/// Las tarifas se versionan: NO existe UpdateAsync de la tarifa. Al
/// actualizar, la BLL cierra la vigencia de la anterior (CerrarVigenciaAsync)
/// y crea una nueva versión. Los recargos SÍ se actualizan en el registro
/// de la tarifa vigente.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync: soft delete.
/// </summary>
public interface ITarifaBaseRepository
{
    /// <summary>
    /// Lista tarifas de la empresa con filtros opcionales por zona
    /// origen, zona destino, modo y vigencia (soloVigentes).
    /// </summary>
    Task<IEnumerable<TarifaBase>> GetAllAsync(Guid empresaId,
        Guid? zonaOrigenId = null, Guid? zonaDestinoId = null,
        string? modo = null, bool? soloVigentes = null);

    /// <summary>Obtiene una tarifa por Id dentro de la empresa.</summary>
    Task<TarifaBase?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene la tarifa vigente para la combinación zona-origen/destino,
    /// modo y tipo de servicio en la fecha indicada (ADR-015):
    /// debe ser activa, tener fecha_vigencia_desde &lt;= fecha y
    /// (fecha_vigencia_hasta IS NULL o &gt;= fecha).
    /// </summary>
    Task<TarifaBase?> GetVigenteAsync(Guid empresaId,
        Guid? zonaOrigenId, Guid? zonaDestinoId,
        string modo, string tipoServicio, DateOnly fecha);

    /// <summary>Crea la tarifa (nueva versión). Retorna el Id generado en BD.</summary>
    Task<Guid> CreateAsync(TarifaBase entidad);

    /// <summary>
    /// Cierra la vigencia de una tarifa: fija fecha_vigencia_hasta
    /// (spec: ayer al crear nueva versión). NO modifica el resto de campos.
    /// </summary>
    Task<bool> CerrarVigenciaAsync(Guid id, Guid empresaId,
        DateOnly fechaHasta);

    /// <summary>Soft delete: activo = false — conserva historial (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    // ── Recargos ───────────────────────────────────────────────

    /// <summary>Lista los recargos de una tarifa de la empresa.</summary>
    Task<IEnumerable<RecargoTarifa>> GetRecargosAsync(
        Guid tarifaId, Guid empresaId);

    /// <summary>Crea un recargo. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateRecargoAsync(RecargoTarifa entidad);

    /// <summary>Actualiza un recargo. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateRecargoAsync(RecargoTarifa entidad);

    /// <summary>Soft delete de un recargo: activo = false.</summary>
    Task<bool> DeactivateRecargoAsync(Guid recargoId, Guid empresaId);
}