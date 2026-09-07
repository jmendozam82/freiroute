using Freiroute.DTO.Tarifa;
using Freiroute.Utility.Pagination;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del catálogo de tarifas base
/// (HU-020, ADR-015). Todo método recibe empresaId extraído del JWT.
/// Las tarifas se versionan: UpdateAsync crea una nueva versión y
/// cierra la vigencia de la anterior — nunca modifica el historial.
/// </summary>
public interface ITarifaBaseService
{
    /// <summary>
    /// Lista paginada de tarifas con filtros por zona origen, zona
    /// destino, modo y vigencia. EsVigente/EsVencida se calculan aquí.
    /// </summary>
    Task<PagedResult<TarifaBaseResponseDto>> GetAllAsync(Guid empresaId,
        Guid? zonaOrigenId, Guid? zonaDestinoId,
        string? modo, bool? soloVigentes, int page, int pageSize);

    /// <summary>Obtiene una tarifa con sus recargos por Id dentro de la empresa.</summary>
    Task<TarifaBaseResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene la tarifa vigente para la combinación zona/modo/servicio
    /// en la fecha indicada (ADR-015). Retorna null si no existe (CA-05).
    /// </summary>
    Task<TarifaBaseResponseDto?> GetVigenteAsync(Guid empresaId,
        Guid? zonaOrigenId, Guid? zonaDestinoId,
        string modo, string tipoServicio, DateOnly fecha);

    /// <summary>Crea la tarifa con sus recargos (nueva versión).</summary>
    Task<TarifaBaseResponseDto> CreateAsync(
        TarifaBaseRequestDto dto, Guid empresaId);

    /// <summary>
    /// Actualiza creando NUEVA versión (ADR-015, CA-03):
    /// 1) cierra la vigencia de la actual (fecha_vigencia_hasta = ayer),
    /// 2) crea la nueva con vigencia desde hoy,
    /// 3) copia los recargos de la versión anterior.
    /// </summary>
    Task<TarifaBaseResponseDto> UpdateAsync(
        Guid id, TarifaBaseRequestDto dto, Guid empresaId);

    /// <summary>Soft delete: activo = false — conserva historial (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    // ── Recargos ───────────────────────────────────────────────

    /// <summary>Agrega un recargo a la tarifa.</summary>
    Task<RecargoTarifaResponseDto> AgregarRecargoAsync(
        Guid tarifaId, RecargoTarifaRequestDto dto, Guid empresaId);

    /// <summary>Actualiza un recargo de la tarifa.</summary>
    Task<RecargoTarifaResponseDto> UpdateRecargoAsync(
        Guid tarifaId, Guid recargoId,
        RecargoTarifaRequestDto dto, Guid empresaId);

    /// <summary>Soft delete de un recargo: activo = false.</summary>
    Task<bool> DeactivateRecargoAsync(
        Guid tarifaId, Guid recargoId, Guid empresaId);

    // ── Simulador de costo ─────────────────────────────────────

    /// <summary>
    /// Simulador de costo (HU-020 CA-04, ADR-015): aplica la tarifa
    /// vigente y calcula CostoBase + recargos. Respeta PrecioMinimo.
    /// Si no hay tarifa, TarifaEncontrada=false con advertencia.
    /// </summary>
    Task<SimularCostoResponseDto> SimularCostoAsync(
        SimularCostoRequestDto dto, Guid empresaId);
}