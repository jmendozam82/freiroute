using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla raíz 'empresas' (tenants del SaaS).
/// NOTA: NO recibe empresaId — es la tabla raíz sin discriminador de tenant
/// (ver ADR-003). Solo el SUPER_ADMIN la opera.
/// </summary>
public interface IEmpresaRepository
{
    /// <summary>Obtiene una empresa activa por su Id.</summary>
    Task<Empresa?> GetByIdAsync(Guid id);

    /// <summary>Obtiene una empresa por el email de su administrador (HU-001 CA-06: unicidad global).</summary>
    Task<Empresa?> GetByEmailAdminAsync(string emailAdmin);

    /// <summary>Obtiene todas las empresas activas (panel Super Admin).</summary>
    Task<IEnumerable<Empresa>> GetAllAsync();

    /// <summary>Insertar nuevo tenant. El UUID lo genera la BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(Empresa empresa);

    /// <summary>Actualiza los datos de una empresa activa.</summary>
    Task<bool> UpdateAsync(Empresa empresa);

    /// <summary>
    /// Actualiza solo el plan_id de la empresa (migración alter_empresas_sprint2).
    /// Se ejecuta al crear el tenant para vincular la suscripción TRIAL inicial.
    /// </summary>
    Task<bool> UpdatePlanIdAsync(Guid empresaId, Guid planId);

    /// <summary>
    /// Persiste el avance del wizard de onboarding (HU-012). Escribe
    /// onboarding_paso_actual y onboarding_completado — Fix re-smoke test:
    /// el progreso debe sobrevivir a cada paso, no solo al UPDATE masivo.
    /// </summary>
    Task<bool> ActualizarOnboardingAsync(Guid empresaId, int pasoActual, bool completado);

    /// <summary>Soft delete: SET activo = false WHERE id = @Id.</summary>
    Task<bool> DeactivateAsync(Guid id);
}