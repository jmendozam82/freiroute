using Freiroute.Entity;
using Freiroute.Utility.Pagination;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'auditoria_actividad'.
/// El log es inmutable: nadie puede editar ni eliminar registros (HU-008 CA-06).
/// Este contrato cubre SOLO: registrar (escritura) y consultar paginado (lectura
/// para el endpoint HU-008). No existe Update ni Deactivate.
/// </summary>
public interface IAuditoriaRepository
{
    /// <summary>
    /// Registra una acción de auditoría. El UUID lo genera la BD.
    /// NUNCA propaga excepciones: los fallos se loguean y se continúa
    /// (la auditoría no puede tumbar la operación de negocio).
    /// </summary>
    Task RegistrarAsync(AuditoriaActividad auditoria);

    /// <summary>
    /// Consulta paginada del log con filtros opcionales por módulo, acción y
    /// rango de fechas (GET /api/auditoria, HU-008 CA-03/04).
    /// </summary>
    Task<PagedResult<AuditoriaActividad>> GetPagedAsync(
        Guid empresaId,
        string? modulo,
        string? accion,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pageNumber,
        int pageSize);
}