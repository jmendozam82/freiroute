using Freiroute.DTO.Ubicacion;
using Freiroute.Utility.Pagination;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de ubicaciones (HU-015, ADR-014).
/// Todo método recibe empresaId extraído del JWT — nunca del body.
/// La creación/actualización con dirección dispara geocodificación
/// automática en background (fail-soft, no bloquea la respuesta).
/// </summary>
public interface IUbicacionService
{
    /// <summary>Lista paginada de ubicaciones con filtros por tipo y texto.</summary>
    Task<PagedResult<UbicacionResponseDto>> GetAllAsync(Guid empresaId,
        string? tipo, string? q, int page, int pageSize);

    /// <summary>Obtiene una ubicación por Id dentro de la empresa.</summary>
    Task<UbicacionResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Todas las ubicaciones con lat/lng para el mapa (HU-015 CA-04).
    /// Payload mínimo — sin datos de contacto.
    /// </summary>
    Task<IEnumerable<UbicacionMapaDto>> GetParaMapaAsync(Guid empresaId);

    /// <summary>
    /// Crea la ubicación y dispara geocodificación en background si
    /// tiene dirección (HU-015 CA-02). Si falla, georeferenciada=false
    /// con advertencia (CA-03) — nunca bloquea ni lanza.
    /// </summary>
    Task<UbicacionResponseDto> CreateAsync(
        UbicacionRequestDto dto, Guid empresaId);

    /// <summary>
    /// Actualiza la ubicación y re-geocodifica si cambió la dirección
    /// (HU-015 CA-02).
    /// </summary>
    Task<UbicacionResponseDto> UpdateAsync(
        Guid id, UbicacionRequestDto dto, Guid empresaId);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>Ajuste manual de coordenadas tras geocodificación inexacta (CA-05).</summary>
    Task<UbicacionResponseDto> ActualizarCoordenadasAsync(
        Guid id, ActualizarCoordenadasDto dto, Guid empresaId);

    /// <summary>
    /// Importación masiva CSV (HU-015 CA-06, ADR-017): filas válidas
    /// se importan, las inválidas se registran y continúa. Retorna el
    /// número de registros importados exitosamente.
    /// </summary>
    Task<int> ImportarCsvAsync(Stream csv, Guid empresaId);
}