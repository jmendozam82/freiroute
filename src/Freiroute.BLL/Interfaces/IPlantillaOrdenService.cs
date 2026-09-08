using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio para plantillas de órdenes y
/// recurrencia (HU-027). Incluye CRUD de plantillas, creación de
/// órdenes desde plantilla y configuración de recurrencia.
/// El background job RecurrenciaOrdenesJob ejecuta las plantillas
/// recurrentes cada día a las 00:05 (patrón ADR-013).
/// Todo método recibe empresaId extraído del JWT (ADR-003).
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IPlantillaOrdenService
{
    /// <summary>
    /// Guarda una orden existente como plantilla (HU-027 CA-01).
    /// Crea un snapshot JSON de los campos de OrdenRequestDto.
    /// </summary>
    Task<PlantillaOrdenResponseDto> GuardarComoPlantillaAsync(
        Guid ordenId, PlantillaOrdenRequestDto dto,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Lista todas las plantillas de la empresa (HU-027).
    /// </summary>
    Task<IEnumerable<PlantillaOrdenResponseDto>> GetAllAsync(Guid empresaId);

    /// <summary>
    /// Obtiene una plantilla por Id (HU-027).
    /// </summary>
    Task<PlantillaOrdenResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Crea una orden en DRAFT desde el snapshot de una plantilla
    /// (HU-027 CA-02). La orden tiene origen_creacion = 'RECURRENTE'.
    /// </summary>
    Task<OrdenResponseDto> CrearOrdenDesdePlantillaAsync(
        Guid plantillaId, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Configura la recurrencia de una plantilla (HU-027 CA-04 a CA-06).
    /// Calcula proxima_ejecucion según la frecuencia.
    /// </summary>
    Task<PlantillaOrdenResponseDto> ConfigurarRecurrenciaAsync(
        Guid plantillaId, ConfigurarRecurrenciaRequestDto dto,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Actualiza los datos de una plantilla existente.
    /// </summary>
    Task<PlantillaOrdenResponseDto> UpdateAsync(
        Guid id, PlantillaOrdenRequestDto dto,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Soft delete: activo = false (ADR-005). Detiene las recurrencias
    /// sin borrar el historial (HU-027 CA-10).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Procesa todas las plantillas recurrentes pendientes (cross-tenant).
    /// </summary>
    Task ProcesarRecurrenciasPendientesAsync(DateOnly fecha);
}
