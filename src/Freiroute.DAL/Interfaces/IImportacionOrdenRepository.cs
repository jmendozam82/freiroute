using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de importaciones CSV de órdenes (HU-022).
/// Registra el historial de cada importación con contadores y detalle
/// de errores. Patrón fail-soft (ADR-017).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IImportacionOrdenRepository
{
    /// <summary>
    /// Lista las importaciones de la empresa ordenadas por fecha DESC.
    /// </summary>
    Task<IEnumerable<ImportacionOrden>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene una importación por Id dentro de la empresa.</summary>
    Task<ImportacionOrden?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Registra el resultado de una importación. Retorna el Id generado en BD.
    /// </summary>
    Task<Guid> CreateAsync(ImportacionOrden entidad);

    /// <summary>
    /// Actualiza los contadores y detalle de errores de una importación.
    /// </summary>
    Task<bool> UpdateAsync(ImportacionOrden entidad);
}
