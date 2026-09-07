using Freiroute.DTO.Unidad;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del catálogo de tipos de embalaje
/// (HU-018). Todo método recibe empresaId extraído del JWT.
/// </summary>
public interface ITipoEmbalajeService
{
    /// <summary>Lista todos los embalajes activos de la empresa.</summary>
    Task<IEnumerable<TipoEmbalajeResponseDto>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un embalaje por Id dentro de la empresa.</summary>
    Task<TipoEmbalajeResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea el tipo de embalaje.</summary>
    Task<TipoEmbalajeResponseDto> CreateAsync(
        TipoEmbalajeRequestDto dto, Guid empresaId);

    /// <summary>Actualiza el tipo de embalaje.</summary>
    Task<TipoEmbalajeResponseDto> UpdateAsync(
        Guid id, TipoEmbalajeRequestDto dto, Guid empresaId);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
}