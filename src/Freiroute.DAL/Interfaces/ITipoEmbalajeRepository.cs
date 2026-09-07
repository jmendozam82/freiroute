using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos del catálogo de tipos de embalaje
/// (HU-018, ADR-003). Todo método recibe <paramref name="empresaId"/>.
/// No existe DeleteAsync: soft delete.
/// </summary>
public interface ITipoEmbalajeRepository
{
    /// <summary>Lista todos los embalajes activos de la empresa.</summary>
    Task<IEnumerable<TipoEmbalaje>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un embalaje por Id dentro de la empresa.</summary>
    Task<TipoEmbalaje?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea el embalaje. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(TipoEmbalaje entidad);

    /// <summary>Actualiza el embalaje de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(TipoEmbalaje entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Copia los embalajes estándar de la empresa raíz (plantilla:
    /// PLT, CAJA, TAM, CTN20, CTN40, GRA, BOB) al nuevo tenant al
    /// crearlo (HU-018 CA-04, seeds del spec).
    /// </summary>
    Task CopiarEmbalajesEstandarAsync(
        Guid empresaIdOrigen, Guid empresaIdDestino);
}