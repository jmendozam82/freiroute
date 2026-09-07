using Freiroute.DTO.Zona;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de zonas de entrega (HU-016).
/// Todo método recibe empresaId extraído del JWT — nunca del body.
/// La verificación point-in-polygon de zonas tipo POLIGONO se
/// implementa en la BLL (ADR-018).
/// </summary>
public interface IZonaEntregaService
{
    /// <summary>Lista todas las zonas activas de la empresa.</summary>
    Task<IEnumerable<ZonaResponseDto>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene una zona por Id dentro de la empresa.</summary>
    Task<ZonaResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Registra una zona nueva con su método de definición.</summary>
    Task<ZonaResponseDto> CreateAsync(ZonaRequestDto dto, Guid empresaId);

    /// <summary>Actualiza la zona (nombre, color, método de definición, listas).</summary>
    Task<ZonaResponseDto> UpdateAsync(Guid id, ZonaRequestDto dto, Guid empresaId);

    /// <summary>
    /// Soft delete (ADR-005). Valida que la zona no tenga tarifas
    /// activas asociadas (HU-016 CA-06).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>Asigna ubicaciones a la zona (relación muchos-a-muchos, CA-04).</summary>
    Task AsignarUbicacionesAsync(Guid zonaId,
        IEnumerable<Guid> ubicacionIds, Guid empresaId);

    /// <summary>Desasigna una ubicación de la zona.</summary>
    Task DesasignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId);

    /// <summary>
    /// Retorna las zonas donde cae el punto (CA-05). Para POLIGONO usa
    /// point-in-polygon manual (ADR-018); para los demás métodos compara
    /// contra las listas de códigos postales/ciudades/departamentos/países.
    /// </summary>
    Task<IEnumerable<ZonaResponseDto>> VerificarPertenenciaAsync(
        double latitud, double longitud, Guid empresaId);
}