using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos del catálogo de tipos de mercancía
/// (HU-017, ADR-003). Todo método recibe <paramref name="empresaId"/>.
/// No existe DeleteAsync: soft delete.
/// </summary>
public interface ITipoMercanciaRepository
{
    /// <summary>
    /// Lista tipos de mercancía de la empresa. Con soloPeligrosas=true
    /// filtra solo las que tienen es_peligroso = true (HU-017 CA-07).
    /// </summary>
    Task<IEnumerable<TipoMercancia>> GetAllAsync(Guid empresaId,
        bool? soloPeligrosas = null);

    /// <summary>Obtiene un tipo de mercancía por Id dentro de la empresa.</summary>
    Task<TipoMercancia?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea el tipo. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(TipoMercancia entidad);

    /// <summary>Actualiza el tipo de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(TipoMercancia entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
}