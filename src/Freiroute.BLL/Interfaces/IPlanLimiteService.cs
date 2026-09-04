using Freiroute.DTO.Plan;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de verificación de límites del plan contratado por un tenant (HU-013 CA-08, ADR-004).
/// Lanza BusinessException si se supera un límite operativo antes de persistir.
/// </summary>
public interface IPlanLimiteService
{
    /// <summary>
    /// Verifica que el tenant no haya superado el límite de usuarios del plan.
    /// Lanza BusinessException si lo supera.
    /// </summary>
    Task VerificarLimiteUsuariosAsync(Guid empresaId);

    /// <summary>
    /// Verifica que el tenant no haya superado el límite de embarques del mes.
    /// Lanza BusinessException si lo supera.
    /// </summary>
    Task VerificarLimiteEmbarquesMesAsync(Guid empresaId);

    /// <summary>
    /// Verifica si un módulo está disponible para el plan del tenant.
    /// Usado por el middleware/módulos para gating de funcionalidades (ADR-004).
    /// </summary>
    Task<bool> ModuloDisponibleAsync(string modulo, Guid empresaId);

    /// <summary>Obtiene el plan activo del tenant (para mostrar el nombre en mensajes de error).</summary>
    Task<PlanResponseDto?> GetPlanActivoAsync(Guid empresaId);

    /// <summary>
    /// Obtiene el plan superior al actual (para sugerir upgrade en los mensajes de límite).
    /// Retorna null si no hay plan superior (ya está en el máximo).
    /// </summary>
    Task<PlanResponseDto?> GetPlanSuperiorAsync(string planActualCodigo);
}
