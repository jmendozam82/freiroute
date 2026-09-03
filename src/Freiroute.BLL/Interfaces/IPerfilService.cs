using Freiroute.DTO.Perfil;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de perfiles (roles) por tenant (HU-006).
/// Todo método recibe empresaId extraído del JWT — nunca del body del request.
/// </summary>
public interface IPerfilService
{
    /// <summary>Obtiene los perfiles activos de la empresa.</summary>
    Task<IEnumerable<PerfilResponseDto>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un perfil por Id dentro de la empresa.</summary>
    Task<PerfilResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea un perfil personalizado. Los perfiles base (es_sistema) los crea el sistema.</summary>
    Task<PerfilResponseDto> CreateAsync(PerfilRequestDto dto, Guid empresaId);

    /// <summary>Actualiza un perfil de la empresa.</summary>
    Task<PerfilResponseDto> UpdateAsync(Guid id, PerfilRequestDto dto, Guid empresaId);

    /// <summary>Soft delete de un perfil. Nunca desactiva perfiles con es_sistema = true.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
}