using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'ubicaciones' (HU-015, ADR-003, ADR-014).
/// ADR-003: todo método filtra por empresaId (capa 1 de aislamiento
/// multi-tenant). Las coordenadas se persisten como derivadas de la
/// geocodificación (ADR-014): se actualizan vía ActualizarCoordenadasAsync
/// tras geocodificar o ajustar manualmente el pin. No existe DeleteAsync:
/// soft delete (ADR-005).
/// </summary>
public class UbicacionRepository : IUbicacionRepository
{
    // Columnas de 'ubicaciones' mapeadas a la entidad Ubicacion (PascalCase).
    private const string Col = @"
        id                     AS Id,
        empresa_id             AS EmpresaId,
        nombre                 AS Nombre,
        codigo                 AS Codigo,
        tipo                   AS Tipo,
        direccion              AS Direccion,
        pais                   AS Pais,
        departamento           AS Departamento,
        ciudad                 AS Ciudad,
        codigo_postal          AS CodigoPostal,
        latitud                AS Latitud,
        longitud               AS Longitud,
        georeferenciada        AS Georeferenciada,
        direccion_normalizada  AS DireccionNormalizada,
        contacto_nombre        AS ContactoNombre,
        contacto_telefono      AS ContactoTelefono,
        contacto_email         AS ContactoEmail,
        substring(horario_apertura::text, 1, 5) AS HorarioApertura,
        substring(horario_cierre::text, 1, 5)   AS HorarioCierre,
        tiempo_servicio_min    AS TiempoServicioMin,
        instrucciones          AS Instrucciones,
        activo                 AS Activo,
        fecha_creacion         AS FechaCreacion,
        fecha_modificacion     AS FechaModificacion";

    private readonly IDbConnection _connection;

    public UbicacionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista ubicaciones activas de la empresa con filtros opcionales
    /// por tipo y texto de búsqueda (nombre, código o ciudad).
    /// </summary>
    public async Task<IEnumerable<Ubicacion>> GetAllAsync(
        Guid empresaId, string? tipo = null, string? q = null)
    {
        var sql = $@"
            SELECT {Col}
            FROM ubicaciones
            WHERE empresa_id = @EmpresaId
              AND activo = true";

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            sql += " AND tipo = @Tipo";
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            sql += @" AND (
                nombre  ILIKE '%' || @Q || '%'
                OR codigo ILIKE '%' || @Q || '%'
                OR ciudad ILIKE '%' || @Q || '%'
            )";
        }

        sql += " ORDER BY nombre ASC";

        return await _connection.QueryAsync<Ubicacion>(sql,
            new { EmpresaId = empresaId, Tipo = tipo, Q = q });
    }

    /// <summary>Obtiene una ubicación activa por Id dentro de la empresa.</summary>
    public async Task<Ubicacion?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                empresa_id             AS EmpresaId,
                nombre                 AS Nombre,
                codigo                 AS Codigo,
                tipo                   AS Tipo,
                direccion              AS Direccion,
                pais                   AS Pais,
                departamento           AS Departamento,
                ciudad                 AS Ciudad,
                codigo_postal          AS CodigoPostal,
                latitud                AS Latitud,
                longitud               AS Longitud,
                georeferenciada        AS Georeferenciada,
                direccion_normalizada  AS DireccionNormalizada,
                contacto_nombre        AS ContactoNombre,
                contacto_telefono      AS ContactoTelefono,
                contacto_email         AS ContactoEmail,
                substring(horario_apertura::text, 1, 5) AS HorarioApertura,
                substring(horario_cierre::text, 1, 5)   AS HorarioCierre,
                tiempo_servicio_min    AS TiempoServicioMin,
                instrucciones          AS Instrucciones,
                activo                 AS Activo,
                fecha_creacion         AS FechaCreacion,
                fecha_modificacion     AS FechaModificacion
            FROM ubicaciones
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Ubicacion>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Solo las ubicaciones con coordenadas validadas — payload para el
    /// mapa Leaflet (HU-015 CA-04, ADR-014).
    /// </summary>
    public async Task<IEnumerable<Ubicacion>> GetGeoreferenciadasAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                empresa_id             AS EmpresaId,
                nombre                 AS Nombre,
                codigo                 AS Codigo,
                tipo                   AS Tipo,
                direccion              AS Direccion,
                pais                   AS Pais,
                departamento           AS Departamento,
                ciudad                 AS Ciudad,
                codigo_postal          AS CodigoPostal,
                latitud                AS Latitud,
                longitud               AS Longitud,
                georeferenciada        AS Georeferenciada,
                direccion_normalizada  AS DireccionNormalizada,
                contacto_nombre        AS ContactoNombre,
                contacto_telefono      AS ContactoTelefono,
                contacto_email         AS ContactoEmail,
                substring(horario_apertura::text, 1, 5) AS HorarioApertura,
                substring(horario_cierre::text, 1, 5)   AS HorarioCierre,
                tiempo_servicio_min    AS TiempoServicioMin,
                instrucciones          AS Instrucciones,
                activo                 AS Activo,
                fecha_creacion         AS FechaCreacion,
                fecha_modificacion     AS FechaModificacion
            FROM ubicaciones
            WHERE empresa_id = @EmpresaId
              AND activo = true
              AND georeferenciada = true
              AND latitud IS NOT NULL
              AND longitud IS NOT NULL
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<Ubicacion>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Insertar ubicación. El UUID lo genera la BD (gen_random_uuid).
    /// Las coordenadas llegan ya geocodificadas o null (ADR-014: si la
    /// geocodificación falla se guarda sin coordenadas y georeferenciada=false).
    /// </summary>
    public async Task<Guid> CreateAsync(Ubicacion entidad)
    {
        const string sql = @"
            INSERT INTO ubicaciones (
                empresa_id,
                nombre,
                codigo,
                tipo,
                direccion,
                pais,
                departamento,
                ciudad,
                codigo_postal,
                latitud,
                longitud,
                georeferenciada,
                direccion_normalizada,
                contacto_nombre,
                contacto_telefono,
                contacto_email,
                horario_apertura,
                horario_cierre,
                tiempo_servicio_min,
                instrucciones
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Codigo,
                @Tipo,
                @Direccion,
                @Pais,
                @Departamento,
                @Ciudad,
                @CodigoPostal,
                @Latitud,
                @Longitud,
                @Georeferenciada,
                @DireccionNormalizada,
                @ContactoNombre,
                @ContactoTelefono,
                @ContactoEmail,
                @HorarioApertura,
                @HorarioCierre,
                @TiempoServicioMin,
                @Instrucciones
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Actualiza los datos de negocio de una ubicación activa de la empresa.
    /// Incluye las coordenadas actuales de la entidad (si el usuario ajustó
    /// el pin o re-geocodificó, la BLL persiste vía ActualizarCoordenadasAsync).
    /// </summary>
    public async Task<bool> UpdateAsync(Ubicacion entidad)
    {
        const string sql = @"
            UPDATE ubicaciones SET
                nombre                = @Nombre,
                codigo                = @Codigo,
                tipo                  = @Tipo,
                direccion             = @Direccion,
                pais                  = @Pais,
                departamento          = @Departamento,
                ciudad                = @Ciudad,
                codigo_postal         = @CodigoPostal,
                latitud               = @Latitud,
                longitud              = @Longitud,
                georeferenciada       = @Georeferenciada,
                direccion_normalizada = @DireccionNormalizada,
                contacto_nombre       = @ContactoNombre,
                contacto_telefono     = @ContactoTelefono,
                contacto_email        = @ContactoEmail,
                horario_apertura      = @HorarioApertura,
                horario_cierre        = @HorarioCierre,
                tiempo_servicio_min   = @TiempoServicioMin,
                instrucciones         = @Instrucciones
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false. Nunca se borra físicamente (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE ubicaciones
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Actualiza solo coordenadas + dirección normalizada tras una
    /// geocodificación (HU-015 CA-02/CA-05, ADR-014). Marca georeferenciada = true.
    /// </summary>
    public async Task<bool> ActualizarCoordenadasAsync(
        Guid id, Guid empresaId, double latitud, double longitud,
        string? direccionNormalizada)
    {
        const string sql = @"
            UPDATE ubicaciones
            SET latitud               = @Latitud,
                longitud              = @Longitud,
                direccion_normalizada = @DireccionNormalizada,
                georeferenciada       = true
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            Id = id,
            EmpresaId = empresaId,
            Latitud = latitud,
            Longitud = longitud,
            DireccionNormalizada = direccionNormalizada
        });
        return rows > 0;
    }

    /// <summary>
    /// Obtiene las ubicaciones activas asignadas a una zona de entrega
    /// (HU-016 CA-04) vía la tabla de relación 'ubicacion_zonas'.
    /// </summary>
    public async Task<IEnumerable<Ubicacion>> GetByZonaAsync(
        Guid zonaId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                u.id                     AS Id,
                u.empresa_id             AS EmpresaId,
                u.nombre                 AS Nombre,
                u.codigo                 AS Codigo,
                u.tipo                   AS Tipo,
                u.direccion              AS Direccion,
                u.pais                   AS Pais,
                u.departamento           AS Departamento,
                u.ciudad                 AS Ciudad,
                u.codigo_postal          AS CodigoPostal,
                u.latitud                AS Latitud,
                u.longitud               AS Longitud,
                u.georeferenciada        AS Georeferenciada,
                u.direccion_normalizada  AS DireccionNormalizada,
                u.contacto_nombre        AS ContactoNombre,
                u.contacto_telefono      AS ContactoTelefono,
                u.contacto_email         AS ContactoEmail,
                substring(u.horario_apertura::text, 1, 5) AS HorarioApertura,
                substring(u.horario_cierre::text, 1, 5)   AS HorarioCierre,
                u.tiempo_servicio_min    AS TiempoServicioMin,
                u.instrucciones          AS Instrucciones,
                u.activo                 AS Activo,
                u.fecha_creacion         AS FechaCreacion,
                u.fecha_modificacion     AS FechaModificacion
            FROM ubicaciones u
            INNER JOIN ubicacion_zonas uz ON uz.ubicacion_id = u.id
            WHERE uz.zona_id = @ZonaId
              AND u.empresa_id = @EmpresaId
              AND u.activo = true
            ORDER BY u.nombre ASC";

        return await _connection.QueryAsync<Ubicacion>(
            sql, new { ZonaId = zonaId, EmpresaId = empresaId });
    }
}