using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de historial de importaciones CSV masivas de órdenes (HU-022).
/// Patrón fail-soft (ADR-017): las filas válidas se crean y las inválidas
/// se registran en detalle_errores sin abortar el proceso.
/// El campo detalle_errores (JSONB) se almacena como string con cast ::jsonb;
/// al leer se devuelve ::text para la entidad (string?).
/// ADR-003: todo método filtra por empresa_id. No existe DeleteAsync (ADR-005).
/// </summary>
public class ImportacionOrdenRepository : IImportacionOrdenRepository
{
    private readonly IDbConnection _connection;

    public ImportacionOrdenRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista las importaciones de la empresa ordenadas por fecha DESC.
    /// Solo campos de listado — SIN detalle_errores (se carga en GetByIdAsync).
    /// </summary>
    public async Task<IEnumerable<ImportacionOrden>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                usuario_id          AS UsuarioId,
                nombre_archivo      AS NombreArchivo,
                total_filas         AS TotalFilas,
                filas_ok            AS FilasOk,
                filas_error         AS FilasError,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM importaciones_orden
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY fecha_creacion DESC";

        return await _connection.QueryAsync<ImportacionOrden>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene una importación completa dentro de la empresa.
    /// SÍ incluye detalle_errores (lectura ::text para la entidad string?).
    /// </summary>
    public async Task<ImportacionOrden?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                usuario_id          AS UsuarioId,
                nombre_archivo      AS NombreArchivo,
                total_filas         AS TotalFilas,
                filas_ok            AS FilasOk,
                filas_error         AS FilasError,
                detalle_errores::text AS DetalleErrores,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM importaciones_orden
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<ImportacionOrden>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Registra el resultado de una importación. El UUID lo genera la BD.
    /// detalle_errores se pasa como string — el cast ::jsonb lo valida al escribir.
    /// </summary>
    public async Task<Guid> CreateAsync(ImportacionOrden entidad)
    {
        const string sql = @"
            INSERT INTO importaciones_orden (
                empresa_id,
                usuario_id,
                nombre_archivo,
                total_filas,
                filas_ok,
                filas_error,
                detalle_errores,
                activo
            ) VALUES (
                @EmpresaId,
                @UsuarioId,
                @NombreArchivo,
                @TotalFilas,
                @FilasOk,
                @FilasError,
                @DetalleErrores::jsonb,
                @Activo
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Actualiza los contadores y el detalle de errores de una importación
    /// (el proceso CSV actualiza el registro conforme avanza). Retorna true si afectó una fila.
    /// </summary>
    public async Task<bool> UpdateAsync(ImportacionOrden entidad)
    {
        const string sql = @"
            UPDATE importaciones_orden SET
                total_filas     = @TotalFilas,
                filas_ok        = @FilasOk,
                filas_error     = @FilasError,
                detalle_errores = @DetalleErrores::jsonb
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }
}