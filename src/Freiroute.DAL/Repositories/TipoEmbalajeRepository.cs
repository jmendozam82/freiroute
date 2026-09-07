using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio del catálogo 'tipos_embalaje' (HU-018, ADR-003).
/// ADR-003: todo método filtra por empresaId.
/// Los embalajes estándar del sistema (PLT, CAJA, TAM, CTN20, CTN40,
/// GRA, BOB) se siembran en la empresa raíz (migración 20260906000011)
/// y CopiarEmbalajesEstandarAsync los replica a cada tenant nuevo
/// (HU-018 CA-04, EmpresaService.CreateAsync).
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class TipoEmbalajeRepository : ITipoEmbalajeRepository
{
    // Columnas de 'tipos_embalaje' mapeadas a la entidad TipoEmbalaje.
    private const string Col = @"
        id                 AS Id,
        empresa_id         AS EmpresaId,
        nombre             AS Nombre,
        codigo             AS Codigo,
        descripcion        AS Descripcion,
        capacidad_kg       AS CapacidadKg,
        capacidad_m3       AS CapacidadM3,
        apilable           AS Apilable,
        activo             AS Activo,
        fecha_creacion     AS FechaCreacion,
        fecha_modificacion AS FechaModificacion";

    private readonly IDbConnection _connection;

    public TipoEmbalajeRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Lista todos los embalajes activos de la empresa.</summary>
    public async Task<IEnumerable<TipoEmbalaje>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                codigo             AS Codigo,
                descripcion        AS Descripcion,
                capacidad_kg       AS CapacidadKg,
                capacidad_m3       AS CapacidadM3,
                apilable           AS Apilable,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM tipos_embalaje
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY codigo ASC";

        return await _connection.QueryAsync<TipoEmbalaje>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene un embalaje activo por Id dentro de la empresa.</summary>
    public async Task<TipoEmbalaje?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                codigo             AS Codigo,
                descripcion        AS Descripcion,
                capacidad_kg       AS CapacidadKg,
                capacidad_m3       AS CapacidadM3,
                apilable           AS Apilable,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM tipos_embalaje
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<TipoEmbalaje>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar embalaje. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(TipoEmbalaje entidad)
    {
        const string sql = @"
            INSERT INTO tipos_embalaje (
                empresa_id,
                nombre,
                codigo,
                descripcion,
                capacidad_kg,
                capacidad_m3,
                apilable
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Codigo,
                @Descripcion,
                @CapacidadKg,
                @CapacidadM3,
                @Apilable
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un embalaje activo de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(TipoEmbalaje entidad)
    {
        const string sql = @"
            UPDATE tipos_embalaje SET
                nombre        = @Nombre,
                codigo        = @Codigo,
                descripcion   = @Descripcion,
                capacidad_kg  = @CapacidadKg,
                capacidad_m3  = @CapacidadM3,
                apilable      = @Apilable
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false (ADR-005). Nunca se borra físicamente.</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE tipos_embalaje
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Copia los embalajes estándar de la empresa raíz (plantilla: PLT,
    /// CAJA, TAM, CTN20, CTN40, GRA, BOB) al nuevo tenant. Insert ... Select
    /// con ON CONFLICT DO NOTHING: idempotente (HU-018 CA-04).
    /// </summary>
    public async Task CopiarEmbalajesEstandarAsync(
        Guid empresaIdOrigen, Guid empresaIdDestino)
    {
        const string sql = @"
            INSERT INTO tipos_embalaje (
                empresa_id,
                nombre,
                codigo,
                descripcion,
                capacidad_kg,
                capacidad_m3,
                apilable,
                activo
            )
            SELECT
                @EmpresaIdDestino,
                nombre,
                codigo,
                descripcion,
                capacidad_kg,
                capacidad_m3,
                apilable,
                true
            FROM tipos_embalaje
            WHERE empresa_id = @EmpresaIdOrigen
              AND activo = true
            ON CONFLICT (empresa_id, codigo) DO NOTHING";

        await _connection.ExecuteAsync(sql, new
        {
            EmpresaIdOrigen = empresaIdOrigen,
            EmpresaIdDestino = empresaIdDestino
        });
    }
}