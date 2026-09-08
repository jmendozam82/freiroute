using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de plantillas de orden (HU-027).
/// Incluye CRUD, consulta de plantillas recurrentes y actualización
/// de próxima ejecución para el background job.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IPlantillaOrdenRepository
{
    /// <summary>
    /// Lista todas las plantillas activas de la empresa.
    /// </summary>
    Task<IEnumerable<PlantillaOrden>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene una plantilla por Id dentro de la empresa.</summary>
    Task<PlantillaOrden?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea la plantilla. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateAsync(PlantillaOrden entidad);

    /// <summary>Actualiza la plantilla. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(PlantillaOrden entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene todas las plantillas recurrentes cuya próxima ejecución
    /// es <= hoy. Consulta cross-tenant.
    /// </summary>
    Task<IEnumerable<PlantillaOrden>> GetRecurrentesPendientesAsync(DateOnly fechaReferencia);

    /// <summary>
    /// Actualiza solo la fecha de próxima ejecución de una plantilla.
    /// </summary>
    Task<bool> UpdateProximaEjecucionAsync(
        Guid plantillaId, Guid empresaId, DateOnly nuevaFecha);
}
