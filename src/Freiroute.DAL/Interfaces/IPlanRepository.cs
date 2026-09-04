using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'planes' (catálogo GLOBAL).
/// NOTA: NO recibe empresaId — es un catálogo del SaaS gestionado por el Super Admin
/// (ADR-004). NO tiene RLS.
/// </summary>
public interface IPlanRepository
{
    /// <summary>Obtiene todos los planes (por defecto solo los activos).</summary>
    Task<IEnumerable<Plan>> GetAllAsync(bool soloActivos = true);

    /// <summary>Obtiene un plan por su Id.</summary>
    Task<Plan?> GetByIdAsync(Guid id);

    /// <summary>Obtiene un plan por su código único (STARTER, PROFESSIONAL, ENTERPRISE).</summary>
    Task<Plan?> GetByCodigoAsync(string codigo);

    /// <summary>Insertar un plan nuevo. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Plan entidad);

    /// <summary>Actualiza los datos de un plan.</summary>
    Task<bool> UpdateAsync(Plan entidad);

    /// <summary>Soft delete de un plan: SET activo = false WHERE id = @Id.</summary>
    Task<bool> DeactivateAsync(Guid id);

    /// <summary>
    /// Cuenta las empresas activas suscritas a este plan.
    /// Usado para validar que NO se desactive un plan con empresas activas (HU-010 CA-04).
    /// </summary>
    Task<int> CountEmpresasSuscritasAsync(Guid planId);
}
