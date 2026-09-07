using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de ubicaciones (HU-015, ADR-003, ADR-014).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT —
/// nunca del body del request (ADR-003). No existe DeleteAsync: soft delete.
/// </summary>
public interface IUbicacionRepository
{
    /// <summary>
    /// Lista ubicaciones de la empresa con filtros opcionales
    /// por tipo y texto (nombre, código, ciudad).
    /// </summary>
    Task<IEnumerable<Ubicacion>> GetAllAsync(Guid empresaId,
        string? tipo = null, string? q = null);

    /// <summary>Obtiene una ubicación por Id dentro de la empresa.</summary>
    Task<Ubicacion?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Solo las ubicaciones con lat/lng — payload para el mapa
    /// (HU-015 CA-04, ADR-014).
    /// </summary>
    Task<IEnumerable<Ubicacion>> GetGeoreferenciadasAsync(Guid empresaId);

    /// <summary>Crea la ubicación. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(Ubicacion entidad);

    /// <summary>Actualiza una ubicación de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(Ubicacion entidad);

    /// <summary>Soft delete: activo = false. Nunca elimina físicamente (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Actualiza solo coordenadas + dirección normalizada tras una
    /// geocodificación (HU-015 CA-02/CA-05). Marca georeferenciada = true.
    /// </summary>
    Task<bool> ActualizarCoordenadasAsync(Guid id, Guid empresaId,
        double latitud, double longitud, string? direccionNormalizada);

    /// <summary>Obtiene las ubicaciones asignadas a una zona (HU-016 CA-04).</summary>
    Task<IEnumerable<Ubicacion>> GetByZonaAsync(
        Guid zonaId, Guid empresaId);
}