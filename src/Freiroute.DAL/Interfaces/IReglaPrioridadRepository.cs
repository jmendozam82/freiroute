using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de reglas de prioridad (HU-029).
/// Reglas configurables por tenant para la elevación automática de
/// prioridad de órdenes. Todo método recibe <paramref name="empresaId"/>
/// extraído del JWT. No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IReglaPrioridadRepository
{
    /// <summary>Lista las reglas activas de la empresa (para el job de elevación).</summary>
    Task<IEnumerable<ReglaPrioridad>> GetActivasByEmpresaAsync(Guid empresaId);

    /// <summary>Obtiene una regla por Id dentro de la empresa.</summary>
    Task<ReglaPrioridad?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea la regla. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(ReglaPrioridad entity);

    /// <summary>Actualiza la configuración de la regla.</summary>
    Task UpdateAsync(ReglaPrioridad entity);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task DeactivateAsync(Guid id, Guid empresaId);
}