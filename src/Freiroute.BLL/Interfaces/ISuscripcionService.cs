using Freiroute.DTO.Admin;
using Freiroute.DTO.Suscripcion;
using Freiroute.Utility.Pagination;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de suscripciones y facturación (HU-011).
/// Gestiona el ciclo de facturación de cada tenant (ADR-004).
/// </summary>
public interface ISuscripcionService
{
    /// <summary>Obtiene las suscripciones paginadas con filtro opcional por estado.</summary>
    Task<PagedResult<SuscripcionResponseDto>> GetAllAsync(
        string? estado, int pageNumber, int pageSize);

    /// <summary>Obtiene una suscripción por su Id.</summary>
    Task<SuscripcionResponseDto?> GetByIdAsync(Guid id);

    /// <summary>Obtiene la suscripción ACTIVA de una empresa.</summary>
    Task<SuscripcionResponseDto?> GetActivaByEmpresaIdAsync(Guid empresaId);

    /// <summary>
    /// Crea una suscripción nueva. Calcula fecha_vencimiento según el ciclo
    /// (MENSUAL +30 días, ANUAL +365 días) y asigna estado (TRIAL si no hay pago).
    /// </summary>
    Task<SuscripcionResponseDto> CreateAsync(SuscripcionRequestDto dto, Guid creadoPorId);

    /// <summary>
    /// Registra un pago manual y actualiza la fecha de vencimiento de la suscripción.
    /// Si el pago está COMPLETED → estado pasa a ACTIVE (HU-011 CA-03).
    /// </summary>
    Task<PagoResponseDto> RegistrarPagoAsync(Guid suscripcionId,
        PagoRequestDto dto, Guid registradoPorId);

    /// <summary>Obtiene el historial de pagos de una empresa.</summary>
    Task<IEnumerable<PagoResponseDto>> GetPagosByEmpresaAsync(Guid empresaId);

    /// <summary>
    /// Procesa los vencimientos de suscripciones (HU-011 CA-05/06):
    /// ACTIVE vencida → PAST_DUE (pasa por período de gracia).
    /// PAST_DUE > 7 días → SUSPENDED.
    /// Llamado por el background job VencimientoSuscripcionJob.
    /// </summary>
    Task ProcesarVencimientosAsync();

    /// <summary>Obtiene las métricas del dashboard financiero (MRR, ARR, churn, etc.).</summary>
    Task<DashboardFinancieroResponseDto> GetDashboardFinancieroAsync();
}
