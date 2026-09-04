using Freiroute.DTO.Admin;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Suscripcion;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del panel de administración global del Super Admin
/// (HU-009, HU-010, HU-011). Opera sobre TODOS los tenants del SaaS.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>
    /// Obtiene las métricas del dashboard global: empresas activas, nuevos este mes,
    /// MRR, ARR, embarques del día, distribución por estado y plan, y próximos a vencer.
    /// </summary>
    Task<DashboardGlobalResponseDto> GetDashboardGlobalAsync();

    /// <summary>Obtiene las métricas del dashboard financiero (MRR, ARR, churn, ingresos).</summary>
    Task<DashboardFinancieroResponseDto> GetDashboardFinancieroAsync();

    /// <summary>
    /// Genera un JWT de impersonación del tenant (HU-009 CA-05).
    /// El JWT incluye el claim "impersonado_por" y registra auditoría con acción IMPERSONACION.
    /// </summary>
    Task<LoginResponseDto> ImpersonarAsync(Guid empresaId, Guid superAdminId);

    /// <summary>
    /// Cambia el plan de un tenant (HU-009 CA-04). Crea una nueva suscripción o actualiza la
    /// existente según el ciclo. Registra auditoría con acción CAMBIAR_PLAN.
    /// </summary>
    Task CambiarPlanAsync(Guid empresaId, Guid nuevoPlanId,
        string? motivo, Guid cambiadoPorId);

    /// <summary>
    /// Cambia el estado de una empresa (suspender/reactivar/cancelar).
    /// Registra auditoría con acción CAMBIO_ESTADO.
    /// </summary>
    Task CambiarEstadoEmpresaAsync(Guid empresaId, string nuevoEstado,
        Guid cambiadoPorId);
}
