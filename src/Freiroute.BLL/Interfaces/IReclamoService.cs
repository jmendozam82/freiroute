using Freiroute.DTO.Reclamo;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de gestión de reclamos (HU-032 — Claims Management).
/// Incluye CRUD, FSM de estados (EstadoReclamo.Transiciones), historial,
/// reportes y consultas por cliente. Solo el perfil con permiso
/// ordenes:update puede mover a APROBADO/RECHAZADO (validación de rol en
/// BLL — CA-12). Todo método recibe <paramref name="empresaId"/>
/// extraído del JWT (ADR-003).
/// </summary>
public interface IReclamoService
{
    /// <summary>
    /// Crea el reclamo en estado ABIERTO (HU-032 CA-01). Valida que la
    /// orden exista en el mismo tenant (CA-02), genera el número legible
    /// (REC-...) y audita CREAR_RECLAMO (CA-11).
    /// </summary>
    Task<ReclamoResponseDto> CreateAsync(
        ReclamoRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>Obtiene el detalle del reclamo con su historial (HU-032 CA-07).</summary>
    Task<ReclamoResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Listado paginado de reclamos del tenant con filtros por estado,
    /// tipo y rango de fechas (HU-032 CA-06).
    /// </summary>
    Task<(IEnumerable<ReclamoListDto> Items, int Total)> GetAllAsync(
        Guid empresaId, ReclamoFiltroDto filtro);

    /// <summary>
    /// Transición de estado con motivo obligatorio y validación contra
    /// EstadoReclamo.Transiciones (HU-032 CA-04/CA-12). Registra el
    /// historial y audita CAMBIO_ESTADO_RECLAMO (CA-11).
    /// </summary>
    Task<ReclamoResponseDto> CambiarEstadoAsync(
        Guid id, ReclamoEstadoRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>Lista los reclamos de un cliente del tenant (HU-032 CA-10).</summary>
    Task<IEnumerable<ReclamoListDto>> GetByClienteAsync(
        Guid clienteId, Guid empresaId);

    /// <summary>Reporte de reclamos por período, tipo y resolución (HU-032 CA-09).</summary>
    Task<IEnumerable<ReclamoReporteItemDto>> GetReporteAsync(
        Guid empresaId, DateTime desde, DateTime hasta);
}