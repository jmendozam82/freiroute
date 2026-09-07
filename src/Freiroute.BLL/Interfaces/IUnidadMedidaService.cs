using Freiroute.DTO.Unidad;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del catálogo de unidades de medida
/// (HU-018). Todo método recibe empresaId extraído del JWT.
/// Incluye el simulador de conversión entre unidades del mismo tipo (CA-05).
/// </summary>
public interface IUnidadMedidaService
{
    /// <summary>Lista unidades con filtro opcional por tipo (PESO, VOLUMEN, LONGITUD, TEMPERATURA).</summary>
    Task<IEnumerable<UnidadMedidaResponseDto>> GetAllAsync(
        Guid empresaId, string? tipo = null);

    /// <summary>Obtiene una unidad por Id dentro de la empresa.</summary>
    Task<UnidadMedidaResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea la unidad de medida.</summary>
    Task<UnidadMedidaResponseDto> CreateAsync(
        UnidadMedidaRequestDto dto, Guid empresaId);

    /// <summary>Actualiza la unidad de medida.</summary>
    Task<UnidadMedidaResponseDto> UpdateAsync(
        Guid id, UnidadMedidaRequestDto dto, Guid empresaId);

    /// <summary>
    /// Soft delete (ADR-005). Valida que ninguna mercancía referencie
    /// la unidad (HU-018 CA-06).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Simulador de conversión (CA-05): convierte un valor entre dos
    /// símbolos del mismo tipo usando factor_conversion
    /// (ej: 100 kg → 220.46 lb).
    /// </summary>
    Task<decimal> ConvertirAsync(decimal valor,
        string simboloDesde, string simboloHacia, Guid empresaId);
}