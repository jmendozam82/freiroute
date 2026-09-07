using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de zonas de entrega y su relación
/// muchos-a-muchos con ubicaciones (HU-016, ADR-003).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync: soft delete.
/// </summary>
public interface IZonaEntregaRepository
{
    /// <summary>Lista todas las zonas activas de la empresa.</summary>
    Task<IEnumerable<ZonaEntrega>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene una zona por Id dentro de la empresa.</summary>
    Task<ZonaEntrega?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea la zona. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(ZonaEntrega entidad);

    /// <summary>Actualiza una zona de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(ZonaEntrega entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>true si la zona tiene ubicaciones asignadas (protección de borrado).</summary>
    Task<bool> TieneUbicacionesAsync(Guid zonaId, Guid empresaId);

    /// <summary>true si la zona está referenciada por tarifas activas (HU-016 CA-06).</summary>
    Task<bool> TieneTarifasActivasAsync(Guid zonaId, Guid empresaId);

    // ── Relación ubicación-zona ────────────────────────────────

    /// <summary>Asigna una ubicación a la zona (tabla ubicacion_zonas).</summary>
    Task AsignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId);

    /// <summary>Desasigna una ubicación de la zona (tabla ubicacion_zonas).</summary>
    Task DesasignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId);

    /// <summary>
    /// Devuelve las zonas donde cae el punto geográfico según su método
    /// de definición (códigos postales/ciudades/departamentos/países).
    /// Para zonas tipo POLIGONO el filtro point-in-polygon se aplica
    /// en la BLL (ADR-018).
    /// </summary>
    Task<IEnumerable<ZonaEntrega>> GetZonasPorPuntoAsync(
        double latitud, double longitud, Guid empresaId);
}