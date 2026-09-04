using Freiroute.DTO.Plan;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de planes de suscripción (HU-010).
/// Catálogo GLOBAL del SaaS — el Super Admin gestiona los planes.
/// </summary>
public interface IPlanService
{
    /// <summary>Obtiene todos los planes (por defecto solo los activos).</summary>
    Task<IEnumerable<PlanResponseDto>> GetAllAsync(bool soloActivos = true);

    /// <summary>Obtiene un plan por su Id.</summary>
    Task<PlanResponseDto?> GetByIdAsync(Guid id);

    /// <summary>Crea un plan nuevo. Por defecto busca es_publico = true.</summary>
    Task<PlanResponseDto> CreateAsync(PlanRequestDto dto);

    /// <summary>Actualiza los datos de un plan existente.</summary>
    Task<PlanResponseDto> UpdateAsync(Guid id, PlanRequestDto dto);

    /// <summary>
    /// Desactiva un plan. Lanza BusinessException si el plan tiene empresas
    /// activas suscritas (HU-010 CA-04).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id);
}
