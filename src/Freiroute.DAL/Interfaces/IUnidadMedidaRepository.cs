using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos del catálogo de unidades de medida
/// (HU-018, ADR-003). Todo método recibe <paramref name="empresaId"/>.
/// No existe DeleteAsync: soft delete.
/// </summary>
public interface IUnidadMedidaRepository
{
    /// <summary>
    /// Lista unidades de la empresa con filtro opcional por tipo
    /// (PESO, VOLUMEN, LONGITUD, TEMPERATURA).
    /// </summary>
    Task<IEnumerable<UnidadMedida>> GetAllAsync(
        Guid empresaId, string? tipo = null);

    /// <summary>Obtiene una unidad por Id dentro de la empresa.</summary>
    Task<UnidadMedida?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene una unidad activa por símbolo (ej: "kg") dentro de la empresa.</summary>
    Task<UnidadMedida?> GetBySimboloAsync(string simbolo, Guid empresaId);

    /// <summary>Crea la unidad. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(UnidadMedida entidad);

    /// <summary>Actualiza la unidad de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(UnidadMedida entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Copia las unidades estándar de la empresa raíz (plantilla)
    /// al nuevo tenant al crearlo (HU-018 CA-02, seeds del spec).
    /// </summary>
    Task CopiarUnidadesEstandarAsync(
        Guid empresaIdOrigen, Guid empresaIdDestino);
}