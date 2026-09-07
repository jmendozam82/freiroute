using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de 'zonas_entrega' y su relación N:N con ubicaciones
/// (tabla 'ubicacion_zonas', HU-016, ADR-003, ADR-018).
/// ADR-003: todo método filtra por empresaId.
/// EXCEPCIÓN documentada (ADR-018): DesasignarUbicacionAsync usa DELETE
/// físico sobre la tabla de relación — es la única operación con borrado
/// real del sistema. Las zonas mismas usan soft delete (ADR-005).
/// GetZonasPorPuntoAsync retorna candidatas para el punto; la BLL aplica
/// ray casting a las POLIGONO y compara listas contra la dirección
/// reverso-geocodificada para las demás (ADR-018).
/// </summary>
public class ZonaEntregaRepository : IZonaEntregaRepository
{
    // Columnas de 'zonas_entrega' mapeadas a la entidad ZonaEntrega (PascalCase).
    private const string Col = @"
        id                 AS Id,
        empresa_id         AS EmpresaId,
        nombre             AS Nombre,
        codigo             AS Codigo,
        descripcion        AS Descripcion,
        color_hex          AS ColorHex,
        tipo_definicion    AS TipoDefinicion,
        poligono_geojson   AS PoligonoGeoJson,
        codigos_postales   AS CodigosPostales,
        ciudades           AS Ciudades,
        departamentos      AS Departamentos,
        paises             AS Paises,
        activo             AS Activo,
        fecha_creacion     AS FechaCreacion,
        fecha_modificacion AS FechaModificacion";

    private readonly IDbConnection _connection;

    public ZonaEntregaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Lista todas las zonas activas de la empresa.</summary>
    public async Task<IEnumerable<ZonaEntrega>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                codigo             AS Codigo,
                descripcion        AS Descripcion,
                color_hex          AS ColorHex,
                tipo_definicion    AS TipoDefinicion,
                poligono_geojson   AS PoligonoGeoJson,
                codigos_postales   AS CodigosPostales,
                ciudades           AS Ciudades,
                departamentos      AS Departamentos,
                paises             AS Paises,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM zonas_entrega
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<ZonaEntrega>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene una zona activa por Id dentro de la empresa.</summary>
    public async Task<ZonaEntrega?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                codigo             AS Codigo,
                descripcion        AS Descripcion,
                color_hex          AS ColorHex,
                tipo_definicion    AS TipoDefinicion,
                poligono_geojson   AS PoligonoGeoJson,
                codigos_postales   AS CodigosPostales,
                ciudades           AS Ciudades,
                departamentos      AS Departamentos,
                paises             AS Paises,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM zonas_entrega
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<ZonaEntrega>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Insertar zona. El UUID lo genera la BD (gen_random_uuid).
    /// Los arreglos text[] (códigos postales, ciudades, etc.) se insertan
    /// directamente desde la entidad.
    /// </summary>
    public async Task<Guid> CreateAsync(ZonaEntrega entidad)
    {
        const string sql = @"
            INSERT INTO zonas_entrega (
                empresa_id,
                nombre,
                codigo,
                descripcion,
                color_hex,
                tipo_definicion,
                poligono_geojson,
                codigos_postales,
                ciudades,
                departamentos,
                paises
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Codigo,
                @Descripcion,
                @ColorHex,
                @TipoDefinicion,
                @PoligonoGeoJson,
                @CodigosPostales,
                @Ciudades,
                @Departamentos,
                @Paises
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza una zona activa de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(ZonaEntrega entidad)
    {
        const string sql = @"
            UPDATE zonas_entrega SET
                nombre             = @Nombre,
                codigo             = @Codigo,
                descripcion        = @Descripcion,
                color_hex          = @ColorHex,
                tipo_definicion    = @TipoDefinicion,
                poligono_geojson   = @PoligonoGeoJson,
                codigos_postales   = @CodigosPostales,
                ciudades           = @Ciudades,
                departamentos      = @Departamentos,
                paises             = @Paises
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
            UPDATE zonas_entrega
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>true si la zona tiene ubicaciones asignadas (protección de borrado).</summary>
    public async Task<bool> TieneUbicacionesAsync(Guid zonaId, Guid empresaId)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1
                FROM ubicacion_zonas
                WHERE zona_id = @ZonaId
                  AND empresa_id = @EmpresaId
            )";

        return await _connection.ExecuteScalarAsync<bool>(
            sql, new { ZonaId = zonaId, EmpresaId = empresaId });
    }

    /// <summary>
    /// true si la zona está referenciada por tarifas activas
    /// (HU-016 CA-06) como origen o destino.
    /// </summary>
    public async Task<bool> TieneTarifasActivasAsync(Guid zonaId, Guid empresaId)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1
                FROM tarifas_base
                WHERE (zona_origen_id = @ZonaId OR zona_destino_id = @ZonaId)
                  AND empresa_id = @EmpresaId
                  AND activo = true
            )";

        return await _connection.ExecuteScalarAsync<bool>(
            sql, new { ZonaId = zonaId, EmpresaId = empresaId });
    }

    /// <summary>
    /// Asigna una ubicación a la zona insertando en 'ubicacion_zonas'.
    /// ON CONFLICT DO NOTHING: si la relación ya existe, no falla.
    /// </summary>
    public async Task AsignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId)
    {
        const string sql = @"
            INSERT INTO ubicacion_zonas (empresa_id, ubicacion_id, zona_id)
            VALUES (@EmpresaId, @UbicacionId, @ZonaId)
            ON CONFLICT (ubicacion_id, zona_id) DO NOTHING";

        await _connection.ExecuteAsync(sql, new
        {
            EmpresaId = empresaId,
            UbicacionId = ubicacionId,
            ZonaId = zonaId
        });
    }

    /// <summary>
    /// Desasigna una ubicación de la zona con DELETE físico sobre la tabla
    /// de relación. EXCEPCIÓN al soft delete universal (ADR-005) justificada
    /// por ADR-018: 'ubicacion_zonas' es append-only, no un registro histórico.
    /// </summary>
    public async Task DesasignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId)
    {
        const string sql = @"
            DELETE FROM ubicacion_zonas
            WHERE zona_id = @ZonaId
              AND ubicacion_id = @UbicacionId
              AND empresa_id = @EmpresaId";

        await _connection.ExecuteAsync(sql, new
        {
            ZonaId = zonaId,
            UbicacionId = ubicacionId,
            EmpresaId = empresaId
        });
    }

    /// <summary>
    /// Devuelve las candidatas de zona para el punto (latitud, longitud):
    ///  - POLIGONO con GeoJSON: la BLL evalúa ray casting (ADR-018).
    ///  - Listas (CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES): la BLL
    ///    compara las listas contra la dirección reverso-geocodificada del
    ///    punto — una coordenada no se resuelve contra texto en SQL.
    /// </summary>
    public async Task<IEnumerable<ZonaEntrega>> GetZonasPorPuntoAsync(
        double latitud, double longitud, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre             AS Nombre,
                codigo             AS Codigo,
                descripcion        AS Descripcion,
                color_hex          AS ColorHex,
                tipo_definicion    AS TipoDefinicion,
                poligono_geojson   AS PoligonoGeoJson,
                codigos_postales   AS CodigosPostales,
                ciudades           AS Ciudades,
                departamentos      AS Departamentos,
                paises             AS Paises,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM zonas_entrega
            WHERE empresa_id = @EmpresaId
              AND activo = true
              AND (
                  (tipo_definicion = 'POLIGONO'
                   AND poligono_geojson IS NOT NULL
                   AND poligono_geojson <> '')
                  OR tipo_definicion IN
                      ('CODIGOS_POSTALES', 'CIUDADES', 'DEPARTAMENTOS', 'PAISES')
              )
            ORDER BY (tipo_definicion = 'POLIGONO') DESC, nombre ASC";

        return await _connection.QueryAsync<ZonaEntrega>(sql, new
        {
            Latitud = latitud,
            Longitud = longitud,
            EmpresaId = empresaId
        });
    }
}