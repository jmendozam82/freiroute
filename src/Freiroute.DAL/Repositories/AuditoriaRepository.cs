using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;
using Freiroute.Utility.Pagination;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'auditoria_actividad' (log inmutable, HU-008).
/// - RegistrarAsync: solo escritura, NUNCA propaga excepciones (la auditoría no
///   puede tumbar la operación de negocio; el fallo se loguea y se continúa).
/// - GetPagedAsync: consulta paginada con filtros (módulo, acción, rango fechas).
/// No existe Update ni Deactivate — el log es inmutable.
/// </summary>
public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<AuditoriaRepository> _logger;

    public AuditoriaRepository(IDbConnection connection, ILogger<AuditoriaRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Registra una acción de auditoría. El UUID lo genera la BD.
    /// Nunca falla silenciosamente: si el INSERT falla se loguea el error con
    /// contexto completo, pero NO se propaga la excepción al llamador.
    /// </summary>
    public async Task RegistrarAsync(AuditoriaActividad auditoria)
    {
        const string sql = @"
            INSERT INTO auditoria_actividad (
                empresa_id,
                usuario_id,
                modulo,
                accion,
                entidad_tipo,
                entidad_id,
                ip_address,
                user_agent,
                detalles
            ) VALUES (
                @EmpresaId,
                @UsuarioId,
                @Modulo,
                @Accion,
                @EntidadTipo,
                @EntidadId,
                @IpAddress::inet,
                @UserAgent,
                @Detalles::jsonb
            )";

        try
        {
            await _connection.ExecuteAsync(sql, auditoria);
        }
        catch (Exception ex)
        {
            // La auditoría nunca debe tumbar la operación de negocio.
            // Se loguea con contexto completo para trazabilidad posterior (Serilog).
            _logger.LogError(ex,
                "Fallo al registrar auditoría. Modulo={Modulo}, Accion={Accion}, EmpresaId={EmpresaId}, UsuarioId={UsuarioId}",
                auditoria.Modulo, auditoria.Accion, auditoria.EmpresaId, auditoria.UsuarioId);
        }
    }

    /// <summary>
    /// Consulta paginada del log con filtros opcionales (HU-008 CA-03/04).
    /// Los filtros se construyen dinámicamente sobre la base empresa_id (ADR-003).
    /// Campo ip_address se lee con cast ::text (la entidad usa string, no IPAddress).
    /// </summary>
    public async Task<PagedResult<AuditoriaActividad>> GetPagedAsync(
        Guid empresaId,
        string? modulo,
        string? accion,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pageNumber,
        int pageSize)
    {
        // Valores seguros de paginación (RNF-01.4: default 20, máx 100)
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var where = new List<string> { "empresa_id = @EmpresaId" };
        var parameters = new DynamicParameters();
        parameters.Add("EmpresaId", empresaId);

        if (!string.IsNullOrWhiteSpace(modulo))
        {
            where.Add("modulo = @Modulo");
            parameters.Add("Modulo", modulo);
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            where.Add("accion = @Accion");
            parameters.Add("Accion", accion);
        }

        if (fechaDesde.HasValue)
        {
            where.Add("fecha_creacion >= @FechaDesde");
            parameters.Add("FechaDesde", fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            where.Add("fecha_creacion <= @FechaHasta");
            parameters.Add("FechaHasta", fechaHasta.Value);
        }

        var whereSql = string.Join(" AND ", where);
        var offset = (pageNumber - 1) * pageSize;

        // Total de registros (para calcular páginas)
        var sqlCount = $@"
            SELECT COUNT(*)
            FROM auditoria_actividad
            WHERE {whereSql}";

        var totalItems = await _connection.ExecuteScalarAsync<int>(sqlCount, parameters);

        // Página de registros ordenados por fecha DESC (log más reciente primero)
        var sqlQuery = $@"
            SELECT
                id             AS Id,
                empresa_id     AS EmpresaId,
                usuario_id     AS UsuarioId,
                modulo         AS Modulo,
                accion         AS Accion,
                entidad_tipo   AS EntidadTipo,
                entidad_id     AS EntidadId,
                ip_address::text AS IpAddress,
                user_agent     AS UserAgent,
                detalles::text AS Detalles,
                fecha_creacion AS FechaCreacion
            FROM auditoria_actividad
            WHERE {whereSql}
            ORDER BY fecha_creacion DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await _connection.QueryAsync<AuditoriaActividad>(sqlQuery, parameters);

        return new PagedResult<AuditoriaActividad>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}