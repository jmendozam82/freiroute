using Freiroute.DTO.Mercancia;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del catálogo de tipos de mercancía
/// (HU-017). Todo método recibe empresaId extraído del JWT.
/// Valida HAZMAT: clase ONU (1-9 con subclases) y rango de temperatura
/// completo si requiere_refrigeracion (CA-02/CA-04).
/// </summary>
public interface ITipoMercanciaService
{
    /// <summary>
    /// Lista tipos de mercancía con filtro opcional de peligrosas
    /// (HU-017 CA-07: se resaltan con badge danger y clase HAZMAT).
    /// </summary>
    Task<IEnumerable<TipoMercanciaResponseDto>> GetAllAsync(
        Guid empresaId, bool? soloPeligrosas = null);

    /// <summary>Obtiene un tipo de mercancía por Id dentro de la empresa.</summary>
    Task<TipoMercanciaResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea el tipo de mercancía con validaciones HAZMAT.</summary>
    Task<TipoMercanciaResponseDto> CreateAsync(
        TipoMercanciaRequestDto dto, Guid empresaId);

    /// <summary>Actualiza el tipo de mercancía con validaciones HAZMAT.</summary>
    Task<TipoMercanciaResponseDto> UpdateAsync(
        Guid id, TipoMercanciaRequestDto dto, Guid empresaId);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Importación masiva CSV (HU-017 CA-06, ADR-017): filas válidas
    /// se importan, las inválidas se registran y continúa. Retorna el
    /// número de registros importados exitosamente.
    /// </summary>
    Task<int> ImportarCsvAsync(Stream csv, Guid empresaId);
}