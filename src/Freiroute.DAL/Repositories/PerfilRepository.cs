using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'perfiles'.
/// ADR-003: TODO método filtra por empresaId (capa 1 de aislamiento multi-tenant).
/// </summary>
public class PerfilRepository : IPerfilRepository
{
    private readonly IDbConnection _connection;

    public PerfilRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene los perfiles activos de una empresa.</summary>
    public async Task<IEnumerable<Perfil>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                descripcion        AS Descripcion,
                tipo_perfil        AS TipoPerfil,
                es_sistema         AS EsSistema,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM perfiles
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY es_sistema DESC, nombre ASC";

        return await _connection.QueryAsync<Perfil>(sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene un perfil activo por Id dentro de la empresa.</summary>
    public async Task<Perfil?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                descripcion        AS Descripcion,
                tipo_perfil        AS TipoPerfil,
                es_sistema         AS EsSistema,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM perfiles
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Perfil>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene el perfil base de una empresa por tipo (ADMIN, DISPATCHER, OPERADOR,
    /// CONDUCTOR, CLIENTE). Se usa para asignar el perfil por defecto al crear
    /// usuarios (HU-001, HU-003).
    /// </summary>
    public async Task<Perfil?> GetByTipoAsync(string tipoPerfil, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                descripcion        AS Descripcion,
                tipo_perfil        AS TipoPerfil,
                es_sistema         AS EsSistema,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM perfiles
            WHERE tipo_perfil = @TipoPerfil
              AND empresa_id = @EmpresaId
              AND activo = true
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Perfil>(
            sql, new { TipoPerfil = tipoPerfil, EmpresaId = empresaId });
    }

    /// <summary>Insertar un perfil. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Perfil perfil)
    {
        const string sql = @"
            INSERT INTO perfiles (
                empresa_id,
                nombre,
                descripcion,
                tipo_perfil,
                es_sistema
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Descripcion,
                @TipoPerfil,
                @EsSistema
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, perfil);
    }

    /// <summary>Cuenta los usuarios activos asignados a un perfil (HU-006).</summary>
    public async Task<int> CountUsuariosAsync(Guid perfilId, Guid empresaId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM usuarios
            WHERE perfil_id = @PerfilId
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.ExecuteScalarAsync<int>(
            sql, new { PerfilId = perfilId, EmpresaId = empresaId });
    }

    /// <summary>Actualiza un perfil activo de la empresa.</summary>
    public async Task<bool> UpdateAsync(Perfil perfil)
    {
        const string sql = @"
            UPDATE perfiles SET
                nombre      = @Nombre,
                descripcion = @Descripcion,
                tipo_perfil = @TipoPerfil,
                es_sistema  = @EsSistema
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, perfil);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE perfiles
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND es_sistema = false";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }
}