namespace Freiroute.BLL.Interfaces;

using Freiroute.DTO.Auditoria;
using Freiroute.Utility.Pagination;

/// <summary>
/// Servicio transversal de auditoría (HU-008). Registra en 'auditoria_actividad'
/// las acciones del sistema. El log es inmutable — solo escritura.
/// Acciones estándar en Constants.AccionAuditoria.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>
    /// Registra una acción de auditoría con los parámetros del spec HU-008.
    /// </summary>
    /// <param name="modulo">Módulo del TMS afectado (ej: "auth", "empresas", "perfiles").</param>
    /// <param name="accion">Acción ejecutada (LOGIN, LOGOUT, CREATE, UPDATE, DEACTIVATE, EXPORT, CAMBIO_ESTADO...).</param>
    /// <param name="empresaId">Tenant afectado (nullable para operaciones globales del Super Admin).</param>
    /// <param name="usuarioId">Usuario que ejecutó la acción (nullable en eventos pre-auth).</param>
    /// <param name="entidadTipo">Tipo de la entidad afectada (ej: "Usuario", "Perfil").</param>
    /// <param name="entidadId">Id del registro afectado.</param>
    /// <param name="detalles">Objeto serializado a JSONB con valores anteriores/nuevos y contexto.</param>
    /// <param name="ipAddress">IP del cliente.</param>
    /// <param name="userAgent">User agent del cliente.</param>
    Task RegistrarAsync(
        string modulo,
        string accion,
        Guid empresaId,
        Guid? usuarioId = null,
        string? entidadTipo = null,
        Guid? entidadId = null,
        object? detalles = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Consulta paginada del log con filtros opcionales por módulo, acción y
    /// rango de fechas (GET /api/auditoria, HU-008 CA-03/04).
    /// </summary>
    Task<PagedResult<AuditoriaActivityResponseDto>> GetPagedAsync(
        Guid empresaId,
        string? modulo,
        string? accion,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pageNumber,
        int pageSize);
}