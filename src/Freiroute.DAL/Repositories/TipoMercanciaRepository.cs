using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio del catálogo 'tipos_mercancia' (HU-017, ADR-003).
/// ADR-003: todo método filtra por empresaId.
/// La clasificación HAZMAT (clase 1-9 ONU + código ONU) y los flags de
/// manejo (frágil, perecedero, refrigeración, sobredimensión, fumigación)
/// se persisten como columnas de la entidad. No existe DeleteAsync:
/// soft delete (ADR-005).
/// </summary>
public class TipoMercanciaRepository : ITipoMercanciaRepository
{
    // Columnas de 'tipos_mercancia' mapeadas a la entidad TipoMercancia.
    private const string Col = @"
        id                        AS Id,
        empresa_id                AS EmpresaId,
        nombre                    AS Nombre,
        codigo                    AS Codigo,
        descripcion               AS Descripcion,
        categoria                 AS Categoria,
        clase_peligrosidad        AS ClasePeligrosidad,
        codigo_onu                AS CodigoOnu,
        codigo_hs                 AS CodigoHs,
        peso_maximo_kg            AS PesoMaximoKg,
        volumen_maximo_m3         AS VolumenMaximoM3,
        temperatura_minima_c      AS TemperaturaMinC,
        temperatura_maxima_c      AS TemperaturaMaxC,
        requiere_refrigeracion    AS RequiereRefrigeracion,
        es_fragil                 AS EsFragil,
        es_peligroso              AS EsPeligroso,
        es_perecedero             AS EsPerecedero,
        es_sobredimensionado      AS EsSobredimensionado,
        requiere_fumigacion       AS RequiereFumigacion,
        activo                    AS Activo,
        fecha_creacion            AS FechaCreacion,
        fecha_modificacion        AS FechaModificacion";

    private readonly IDbConnection _connection;

    public TipoMercanciaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista tipos de mercancía activos de la empresa. Con
    /// soloPeligrosas=true filtra solo los que tienen es_peligroso = true
    /// (HU-017 CA-07 — resaltado de HAZMAT).
    /// </summary>
    public async Task<IEnumerable<TipoMercancia>> GetAllAsync(
        Guid empresaId, bool? soloPeligrosas = null)
    {
        var sql = $@"
            SELECT {Col}
            FROM tipos_mercancia
            WHERE empresa_id = @EmpresaId
              AND activo = true";

        if (soloPeligrosas == true)
        {
            sql += " AND es_peligroso = true";
        }

        sql += " ORDER BY nombre ASC";

        return await _connection.QueryAsync<TipoMercancia>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene un tipo de mercancía activo por Id dentro de la empresa.</summary>
    public async Task<TipoMercancia?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                        AS Id,
                empresa_id                AS EmpresaId,
                nombre                    AS Nombre,
                codigo                    AS Codigo,
                descripcion               AS Descripcion,
                categoria                 AS Categoria,
                clase_peligrosidad        AS ClasePeligrosidad,
                codigo_onu                AS CodigoOnu,
                codigo_hs                 AS CodigoHs,
                peso_maximo_kg            AS PesoMaximoKg,
                volumen_maximo_m3         AS VolumenMaximoM3,
                temperatura_minima_c      AS TemperaturaMinC,
                temperatura_maxima_c      AS TemperaturaMaxC,
                requiere_refrigeracion    AS RequiereRefrigeracion,
                es_fragil                 AS EsFragil,
                es_peligroso              AS EsPeligroso,
                es_perecedero             AS EsPerecedero,
                es_sobredimensionado      AS EsSobredimensionado,
                requiere_fumigacion       AS RequiereFumigacion,
                activo                    AS Activo,
                fecha_creacion            AS FechaCreacion,
                fecha_modificacion        AS FechaModificacion
            FROM tipos_mercancia
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<TipoMercancia>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar tipo de mercancía. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(TipoMercancia entidad)
    {
        const string sql = @"
            INSERT INTO tipos_mercancia (
                empresa_id,
                nombre,
                codigo,
                descripcion,
                categoria,
                clase_peligrosidad,
                codigo_onu,
                codigo_hs,
                peso_maximo_kg,
                volumen_maximo_m3,
                temperatura_minima_c,
                temperatura_maxima_c,
                requiere_refrigeracion,
                es_fragil,
                es_peligroso,
                es_perecedero,
                es_sobredimensionado,
                requiere_fumigacion
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Codigo,
                @Descripcion,
                @Categoria,
                @ClasePeligrosidad,
                @CodigoOnu,
                @CodigoHs,
                @PesoMaximoKg,
                @VolumenMaximoM3,
                @TemperaturaMinC,
                @TemperaturaMaxC,
                @RequiereRefrigeracion,
                @EsFragil,
                @EsPeligroso,
                @EsPerecedero,
                @EsSobredimensionado,
                @RequiereFumigacion
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un tipo de mercancía activo de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(TipoMercancia entidad)
    {
        const string sql = @"
            UPDATE tipos_mercancia SET
                nombre                 = @Nombre,
                codigo                 = @Codigo,
                descripcion            = @Descripcion,
                categoria              = @Categoria,
                clase_peligrosidad     = @ClasePeligrosidad,
                codigo_onu             = @CodigoOnu,
                codigo_hs              = @CodigoHs,
                peso_maximo_kg         = @PesoMaximoKg,
                volumen_maximo_m3      = @VolumenMaximoM3,
                temperatura_minima_c   = @TemperaturaMinC,
                temperatura_maxima_c   = @TemperaturaMaxC,
                requiere_refrigeracion = @RequiereRefrigeracion,
                es_fragil              = @EsFragil,
                es_peligroso           = @EsPeligroso,
                es_perecedero          = @EsPerecedero,
                es_sobredimensionado   = @EsSobredimensionado,
                requiere_fumigacion    = @RequiereFumigacion
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
            UPDATE tipos_mercancia
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }
}