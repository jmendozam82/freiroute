using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de priorización dinámica de órdenes (HU-029).
/// Incluye cambio manual de prioridad por el dispatcher, consulta de
/// órdenes críticas sin avance y elevación automática (reglas por
/// tenant — REGLA_PRIORIDAD — ejecutada por PrioridadOrdenesJob).
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT (ADR-003).
/// </summary>
public interface IPrioridadOrdenService
{
    /// <summary>
    /// Cambia manualmente la prioridad de una orden (HU-029 CA-09).
    /// Valida el valor contra OrdenPrioridad y registra auditoría con
    /// accion CAMBIO_PRIORIDAD (prioridad anterior y nueva en JSON).
    /// </summary>
    Task<OrdenResponseDto> CambiarPrioridadAsync(
        Guid ordenId, PrioridadRequestDto dto, Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Lista órdenes en CRITICO o ALTO con más de 4 horas sin avanzar al
    /// siguiente estado (HU-029 CA-04 — GET /api/ordenes/criticas).
    /// </summary>
    Task<IEnumerable<OrdenListDto>> GetCriticasAsync(Guid empresaId);

    /// <summary>
    /// Ejecuta las reglas activas del tenant y eleva prioridades
    /// automáticamente. Retorna el número de órdenes elevadas.
    /// Usado por PrioridadOrdenesJob (patrón ADR-013). Auditoría con
    /// accion AUTO_PRIORIDAD (HU-029 CA-08).
    /// </summary>
    Task<int> ElevarPrioridadesAutomaticasAsync(Guid empresaId);
}