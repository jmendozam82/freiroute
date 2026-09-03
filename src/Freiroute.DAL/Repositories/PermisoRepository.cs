using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'permisos' (permisos granulares por perfil y módulo).
/// ADR-009: modelo de flags booleanos (puede_leer, puede_crear, puede_actualizar).
/// ADR-003: todo método filtra por empresaId.
/// Solo READ/CREATE/UPDATE — no existe DeleteAsync.
/// </summary>
public class PermisoRepository : IPermisoRepository
{
    private readonly IDbConnection _connection;

    public PermisoRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene los permisos activos de una empresa.</summary>
    public async Task<IEnumerable<Permiso>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                modulo             AS Modulo,
                puede_leer         AS PuedeLeer,
                puede_crear        AS PuedeCrear,
                puede_actualizar   AS PuedeActualizar,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM permisos
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY perfil_id, modulo ASC";

        return await _connection.QueryAsync<Permiso>(sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene los permisos activos de un perfil (GET /api/perfiles/{id}/permisos).
    /// También se usa para construir los claims "modulo:accion" del JWT (HU-003).
    /// </summary>
    public async Task<IEnumerable<Permiso>> GetByPerfilAsync(Guid perfilId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                modulo             AS Modulo,
                puede_leer         AS PuedeLeer,
                puede_crear        AS PuedeCrear,
                puede_actualizar   AS PuedeActualizar,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM permisos
            WHERE perfil_id = @PerfilId
              AND empresa_id = @EmpresaId
              AND activo = true
            ORDER BY modulo ASC";

        return await _connection.QueryAsync<Permiso>(
            sql, new { PerfilId = perfilId, EmpresaId = empresaId });
    }

    /// <summary>Obtiene un permiso activo por Id dentro de la empresa.</summary>
    public async Task<Permiso?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                modulo             AS Modulo,
                puede_leer         AS PuedeLeer,
                puede_crear        AS PuedeCrear,
                puede_actualizar   AS PuedeActualizar,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM permisos
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Permiso>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar un permiso. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Permiso permiso)
    {
        const string sql = @"
            INSERT INTO permisos (
                empresa_id,
                perfil_id,
                modulo,
                puede_leer,
                puede_crear,
                puede_actualizar
            ) VALUES (
                @EmpresaId,
                @PerfilId,
                @Modulo,
                @PuedeLeer,
                @PuedeCrear,
                @PuedeActualizar
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, permiso);
    }

    /// <summary>Actualiza los flags de un permiso activo.</summary>
    public async Task<bool> UpdateAsync(Permiso permiso)
    {
        const string sql = @"
            UPDATE permisos SET
                modulo           = @Modulo,
                puede_leer       = @PuedeLeer,
                puede_crear      = @PuedeCrear,
                puede_actualizar = @PuedeActualizar
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, permiso);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE permisos
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Reemplaza en UNA transacción atómica el set completo de permisos de un perfil
    /// (PUT /api/perfiles/{id}/permisos, HU-006):
    ///   1. DELETE físico de los permisos existentes del perfil en la empresa
    ///   2. INSERT de todos los permisos nuevos
    /// Si falla cualquier INSERT → rollback completo de la transacción.
    /// NOTA: el DELETE físico es deliberado y exclusivo de esta operación de
    /// reemplazo (ver reporte Fase 2 — excepción documentada al soft delete).
    /// </summary>
    public async Task<bool> ReemplazarPermisosAsync(Guid perfilId, IEnumerable<Permiso> permisos, Guid empresaId)
    {
        // La conexión del contenedor puede estar abierta o cerrada; gestionamos
        // su ciclo solo si la abrimos nosotros (no interferir con el contenedor).
        var wasClosed = _connection.State == ConnectionState.Closed;
        if (wasClosed) _connection.Open();

        using var transaction = _connection.BeginTransaction();
        try
        {
            // 1. Eliminar los permisos previos del perfil (solo los de esta empresa)
            const string sqlDelete = @"
                DELETE FROM permisos
                WHERE perfil_id = @PerfilId
                  AND empresa_id = @EmpresaId";

            await _connection.ExecuteAsync(
                sqlDelete,
                new { PerfilId = perfilId, EmpresaId = empresaId },
                transaction);

            // 2. Insertar los nuevos permisos
            const string sqlInsert = @"
                INSERT INTO permisos (
                    empresa_id,
                    perfil_id,
                    modulo,
                    puede_leer,
                    puede_crear,
                    puede_actualizar
                ) VALUES (
                    @EmpresaId,
                    @PerfilId,
                    @Modulo,
                    @PuedeLeer,
                    @PuedeCrear,
                    @PuedeActualizar
                )";

            foreach (var permiso in permisos)
            {
                await _connection.ExecuteAsync(
                    sqlInsert,
                    new
                    {
                        EmpresaId = empresaId,
                        PerfilId = perfilId,
                        Modulo = permiso.Modulo,
                        permiso.PuedeLeer,
                        permiso.PuedeCrear,
                        permiso.PuedeActualizar
                    },
                    transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            // Rollback completo: ni DELETE ni INSERTs quedan aplicados
            transaction.Rollback();
            throw;
        }
        finally
        {
            if (wasClosed) _connection.Close();
        }
    }
}