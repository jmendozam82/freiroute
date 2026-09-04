using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'planes' (catálogo GLOBAL del SaaS).
/// ADR-004: NO recibe empresaId — es un catálogo sin filtro de tenant.
/// Solo el SUPER_ADMIN la opera (HU-010).
/// </summary>
public class PlanRepository : IPlanRepository
{
    private readonly IDbConnection _connection;

    public PlanRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Obtiene todos los planes, ordenados por precio mensual ascendente.
    /// Si soloActivos = true, filtra solo los activos (default).
    /// </summary>
    public async Task<IEnumerable<Plan>> GetAllAsync(bool soloActivos = true)
    {
        const string sql = @"
            SELECT
                id                   AS Id,
                nombre               AS Nombre,
                codigo               AS Codigo,
                descripcion          AS Descripcion,
                limite_usuarios      AS LimiteUsuarios,
                limite_embarques_mes AS LimiteEmbarquesMes,
                limite_storage_gb    AS LimiteStorageGb,
                precio_mensual       AS PrecioMensual,
                precio_anual         AS PrecioAnual,
                moneda               AS Moneda,
                modulos_disponibles  AS ModulosDisponibles,
                es_publico           AS EsPublico,
                activo               AS Activo,
                fecha_creacion       AS FechaCreacion,
                fecha_modificacion   AS FechaModificacion
            FROM planes
            WHERE (@SoloActivos = false OR activo = true)
            ORDER BY precio_mensual ASC";

        return await _connection.QueryAsync<Plan>(sql, new { SoloActivos = soloActivos });
    }

    /// <summary>Obtiene un plan por su Id.</summary>
    public async Task<Plan?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT
                id                   AS Id,
                nombre               AS Nombre,
                codigo               AS Codigo,
                descripcion          AS Descripcion,
                limite_usuarios      AS LimiteUsuarios,
                limite_embarques_mes AS LimiteEmbarquesMes,
                limite_storage_gb    AS LimiteStorageGb,
                precio_mensual       AS PrecioMensual,
                precio_anual         AS PrecioAnual,
                moneda               AS Moneda,
                modulos_disponibles  AS ModulosDisponibles,
                es_publico           AS EsPublico,
                activo               AS Activo,
                fecha_creacion       AS FechaCreacion,
                fecha_modificacion   AS FechaModificacion
            FROM planes
            WHERE id = @Id";

        return await _connection.QueryFirstOrDefaultAsync<Plan>(sql, new { Id = id });
    }

    /// <summary>Obtiene un plan por su código único (STARTER, PROFESSIONAL, ENTERPRISE).</summary>
    public async Task<Plan?> GetByCodigoAsync(string codigo)
    {
        const string sql = @"
            SELECT
                id                   AS Id,
                nombre               AS Nombre,
                codigo               AS Codigo,
                descripcion          AS Descripcion,
                limite_usuarios      AS LimiteUsuarios,
                limite_embarques_mes AS LimiteEmbarquesMes,
                limite_storage_gb    AS LimiteStorageGb,
                precio_mensual       AS PrecioMensual,
                precio_anual         AS PrecioAnual,
                moneda               AS Moneda,
                modulos_disponibles  AS ModulosDisponibles,
                es_publico           AS EsPublico,
                activo               AS Activo,
                fecha_creacion       AS FechaCreacion,
                fecha_modificacion   AS FechaModificacion
            FROM planes
            WHERE codigo = @Codigo
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Plan>(sql, new { Codigo = codigo });
    }

    /// <summary>Inserta un plan nuevo. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Plan entidad)
    {
        const string sql = @"
            INSERT INTO planes (
                nombre,
                codigo,
                descripcion,
                limite_usuarios,
                limite_embarques_mes,
                limite_storage_gb,
                precio_mensual,
                precio_anual,
                moneda,
                modulos_disponibles,
                es_publico
            ) VALUES (
                @Nombre,
                @Codigo,
                @Descripcion,
                @LimiteUsuarios,
                @LimiteEmbarquesMes,
                @LimiteStorageGb,
                @PrecioMensual,
                @PrecioAnual,
                @Moneda,
                @ModulosDisponibles,
                @EsPublico
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza los datos de un plan.</summary>
    public async Task<bool> UpdateAsync(Plan entidad)
    {
        const string sql = @"
            UPDATE planes SET
                nombre               = @Nombre,
                codigo               = @Codigo,
                descripcion          = @Descripcion,
                limite_usuarios      = @LimiteUsuarios,
                limite_embarques_mes = @LimiteEmbarquesMes,
                limite_storage_gb    = @LimiteStorageGb,
                precio_mensual       = @PrecioMensual,
                precio_anual         = @PrecioAnual,
                moneda               = @Moneda,
                modulos_disponibles  = @ModulosDisponibles,
                es_publico           = @EsPublico
            WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>
    /// Soft delete de un plan: SET activo = false WHERE id = @Id.
    /// Antes de desactivar, verifica que NO haya empresas suscritas activas
    /// (HU-010 CA-04). Si hay al menos una, retorna false — la BLL lanzará una
    /// BusinessException con el mensaje apropiado.
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        // Regla de negocio (HU-010 CA-04): no se puede desactivar un plan
        // con empresas suscritas activas. El bloqueo se hace a nivel de repositorio
        // como red de seguridad; la BLL también lo valida con CountEmpresasSuscritasAsync.
        var empresasSuscritas = await CountEmpresasSuscritasAsync(id);
        if (empresasSuscritas > 0)
        {
            return false;
        }

        const string sql = @"
            UPDATE planes
            SET activo = false
            WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    /// <summary>
    /// Cuenta las empresas con suscripciones activas a este plan.
    /// Se usa para validar que NO se desactive un plan con empresas activas (HU-010 CA-04).
    /// Retorna el número de suscripciones activas vinculadas al plan que NO están canceladas.
    /// </summary>
    public async Task<int> CountEmpresasSuscritasAsync(Guid planId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM suscripciones
            WHERE plan_id = @PlanId
              AND activo = true
              AND estado != 'CANCELLED'";

        return await _connection.ExecuteScalarAsync<int>(sql, new { PlanId = planId });
    }
}
