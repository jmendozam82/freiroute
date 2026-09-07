using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio del catálogo 'unidades_medida' (HU-018, ADR-003).
/// ADR-003: todo método filtra por empresaId.
/// Las unidades estándar del sistema se siembran en la empresa raíz
/// (migración 20260906000011) y CopiarUnidadesEstandarAsync las replica
/// a cada tenant nuevo al crearlo (HU-018 CA-02, EmpresaService.CreateAsync).
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class UnidadMedidaRepository : IUnidadMedidaRepository
{
    // Columnas de 'unidades_medida' mapeadas a la entidad UnidadMedida.
    private const string Col = @"
        id                 AS Id,
        empresa_id         AS EmpresaId,
        nombre             AS Nombre,
        simbolo            AS Simbolo,
        tipo               AS Tipo,
        factor_conversion  AS FactorConversion,
        unidad_base        AS UnidadBase,
        activo             AS Activo,
        fecha_creacion     AS FechaCreacion,
        fecha_modificacion AS FechaModificacion";

    private readonly IDbConnection _connection;

    public UnidadMedidaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista unidades activas de la empresa con filtro opcional por tipo
    /// (PESO, VOLUMEN, LONGITUD, TEMPERATURA).
    /// </summary>
    public async Task<IEnumerable<UnidadMedida>> GetAllAsync(
        Guid empresaId, string? tipo = null)
    {
        var sql = $@"
            SELECT {Col}
            FROM unidades_medida
            WHERE empresa_id = @EmpresaId
              AND activo = true";

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            sql += " AND tipo = @Tipo";
        }

        sql += " ORDER BY tipo ASC, simbolo ASC";

        return await _connection.QueryAsync<UnidadMedida>(
            sql, new { EmpresaId = empresaId, Tipo = tipo });
    }

    /// <summary>Obtiene una unidad activa por Id dentro de la empresa.</summary>
    public async Task<UnidadMedida?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                simbolo            AS Simbolo,
                tipo               AS Tipo,
                factor_conversion  AS FactorConversion,
                unidad_base        AS UnidadBase,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM unidades_medida
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<UnidadMedida>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Obtiene una unidad activa por símbolo (ej: "kg") dentro de la empresa.</summary>
    public async Task<UnidadMedida?> GetBySimboloAsync(string simbolo, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                simbolo            AS Simbolo,
                tipo               AS Tipo,
                factor_conversion  AS FactorConversion,
                unidad_base        AS UnidadBase,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM unidades_medida
            WHERE LOWER(simbolo) = LOWER(@Simbolo)
              AND empresa_id = @EmpresaId
              AND activo = true
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<UnidadMedida>(
            sql, new { Simbolo = simbolo, EmpresaId = empresaId });
    }

    /// <summary>Insertar unidad de medida. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(UnidadMedida entidad)
    {
        const string sql = @"
            INSERT INTO unidades_medida (
                empresa_id,
                nombre,
                simbolo,
                tipo,
                factor_conversion,
                unidad_base
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Simbolo,
                @Tipo,
                @FactorConversion,
                @UnidadBase
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza una unidad activa de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(UnidadMedida entidad)
    {
        const string sql = @"
            UPDATE unidades_medida SET
                nombre            = @Nombre,
                simbolo           = @Simbolo,
                tipo              = @Tipo,
                factor_conversion = @FactorConversion,
                unidad_base       = @UnidadBase
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
            UPDATE unidades_medida
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Copia las unidades estándar de la empresa raíz (plantilla) al nuevo
    /// tenant. Insert ... Select con ON CONFLICT DO NOTHING: idempotente
    /// (HU-018 CA-02). Solo replica unidades activas de la plantilla.
    /// </summary>
    public async Task CopiarUnidadesEstandarAsync(
        Guid empresaIdOrigen, Guid empresaIdDestino)
    {
        const string sql = @"
            INSERT INTO unidades_medida (
                empresa_id,
                nombre,
                simbolo,
                tipo,
                factor_conversion,
                unidad_base,
                activo
            )
            SELECT
                @EmpresaIdDestino,
                nombre,
                simbolo,
                tipo,
                factor_conversion,
                unidad_base,
                true
            FROM unidades_medida
            WHERE empresa_id = @EmpresaIdOrigen
              AND activo = true
            ON CONFLICT (empresa_id, simbolo) DO NOTHING";

        await _connection.ExecuteAsync(sql, new
        {
            EmpresaIdOrigen = empresaIdOrigen,
            EmpresaIdDestino = empresaIdDestino
        });
    }
}