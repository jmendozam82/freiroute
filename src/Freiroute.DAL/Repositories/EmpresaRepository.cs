using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla raíz 'empresas' (tenants del SaaS).
/// ADR-003: es la tabla raíz — NO recibe empresaId en ningún método.
/// Solo el SUPER_ADMIN la opera (HU-001).
/// </summary>
public class EmpresaRepository : IEmpresaRepository
{
    private readonly IDbConnection _connection;

    public EmpresaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene una empresa activa por su Id.</summary>
    public async Task<Empresa?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                nombre                 AS Nombre,
                ruc_nit                AS RucNit,
                email_admin            AS EmailAdmin,
                telefono               AS Telefono,
                pais                   AS Pais,
                ciudad                 AS Ciudad,
                direccion              AS Direccion,
                logo_url               AS LogoUrl,
                color_primario         AS ColorPrimario,
                color_secundario       AS ColorSecundario,
                plan_suscripcion       AS PlanSuscripcion,
                estado                 AS Estado,
                moneda_principal       AS MonedaPrincipal,
                zona_horaria           AS ZonaHoraria,
                idioma                 AS Idioma,
                formato_fecha          AS FormatoFecha,
                prefijo_embarque       AS PrefijoEmbarque,
                consecutivo_embarque   AS ConsecutivoEmbarque,
                prefijo_orden          AS PrefijoOrden,
                consecutivo_orden      AS ConsecutivoOrden,
                prefijo_carta_porte    AS PrefijoCartaPorte,
                consecutivo_carta_porte AS ConsecutivoCartaPorte,
                plan_id                AS PlanId,
                onboarding_paso_actual AS OnboardingPasoActual,
                onboarding_completado  AS OnboardingCompletado,
                activo                 AS Activo,
                fecha_creacion         AS FechaCreacion,
                fecha_modificacion     AS FechaModificacion
            FROM empresas
            WHERE id = @Id
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Empresa>(sql, new { Id = id });
    }

    /// <summary>
    /// Obtiene una empresa por el email de su administrador.
    /// HU-001 CA-06: valida unicidad global — NO filtra por activo para que
    /// el email de una empresa desactivada siga bloqueado (no reutilizable).
    /// </summary>
    public async Task<Empresa?> GetByEmailAdminAsync(string emailAdmin)
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                nombre                 AS Nombre,
                ruc_nit                AS RucNit,
                email_admin            AS EmailAdmin,
                telefono               AS Telefono,
                pais                   AS Pais,
                ciudad                 AS Ciudad,
                direccion              AS Direccion,
                logo_url               AS LogoUrl,
                color_primario         AS ColorPrimario,
                color_secundario       AS ColorSecundario,
                plan_suscripcion       AS PlanSuscripcion,
                estado                 AS Estado,
                moneda_principal       AS MonedaPrincipal,
                zona_horaria           AS ZonaHoraria,
                idioma                 AS Idioma,
                formato_fecha          AS FormatoFecha,
                prefijo_embarque       AS PrefijoEmbarque,
                consecutivo_embarque   AS ConsecutivoEmbarque,
                prefijo_orden          AS PrefijoOrden,
                consecutivo_orden      AS ConsecutivoOrden,
                prefijo_carta_porte    AS PrefijoCartaPorte,
                consecutivo_carta_porte AS ConsecutivoCartaPorte,
                plan_id                AS PlanId,
                onboarding_paso_actual AS OnboardingPasoActual,
                onboarding_completado  AS OnboardingCompletado,
                activo                 AS Activo,
                fecha_creacion         AS FechaCreacion,
                fecha_modificacion     AS FechaModificacion
            FROM empresas
            WHERE LOWER(email_admin) = LOWER(@EmailAdmin)
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Empresa>(sql, new { EmailAdmin = emailAdmin });
    }

    /// <summary>Obtiene todas las empresas activas (panel Super Admin), ordenadas por nombre.</summary>
    public async Task<IEnumerable<Empresa>> GetAllAsync()
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                nombre                 AS Nombre,
                ruc_nit                AS RucNit,
                email_admin            AS EmailAdmin,
                telefono               AS Telefono,
                pais                   AS Pais,
                ciudad                 AS Ciudad,
                direccion              AS Direccion,
                logo_url               AS LogoUrl,
                color_primario         AS ColorPrimario,
                color_secundario       AS ColorSecundario,
                plan_suscripcion       AS PlanSuscripcion,
                estado                 AS Estado,
                moneda_principal       AS MonedaPrincipal,
                zona_horaria           AS ZonaHoraria,
                idioma                 AS Idioma,
                formato_fecha          AS FormatoFecha,
                prefijo_embarque       AS PrefijoEmbarque,
                consecutivo_embarque   AS ConsecutivoEmbarque,
                prefijo_orden          AS PrefijoOrden,
                consecutivo_orden      AS ConsecutivoOrden,
                prefijo_carta_porte    AS PrefijoCartaPorte,
                consecutivo_carta_porte AS ConsecutivoCartaPorte,
                plan_id                AS PlanId,
                onboarding_paso_actual AS OnboardingPasoActual,
                onboarding_completado  AS OnboardingCompletado,
                activo                 AS Activo,
                fecha_creacion         AS FechaCreacion,
                fecha_modificacion     AS FechaModificacion
            FROM empresas
            WHERE activo = true
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<Empresa>(sql);
    }

    /// <summary>Insertar nuevo tenant. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Empresa empresa)
    {
        const string sql = @"
            INSERT INTO empresas (
                nombre,
                ruc_nit,
                email_admin,
                telefono,
                pais,
                ciudad,
                direccion,
                logo_url,
                color_primario,
                color_secundario,
                plan_suscripcion,
                estado,
                moneda_principal,
                zona_horaria,
                idioma,
                formato_fecha,
                prefijo_embarque,
                consecutivo_embarque,
                prefijo_orden,
                consecutivo_orden
            ) VALUES (
                @Nombre,
                @RucNit,
                @EmailAdmin,
                @Telefono,
                @Pais,
                @Ciudad,
                @Direccion,
                @LogoUrl,
                @ColorPrimario,
                @ColorSecundario,
                @PlanSuscripcion,
                @Estado,
                @MonedaPrincipal,
                @ZonaHoraria,
                @Idioma,
                @FormatoFecha,
                @PrefijoEmbarque,
                @ConsecutivoEmbarque,
                @PrefijoOrden,
                @ConsecutivoOrden
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, empresa);
    }

    /// <summary>
    /// Actualiza los campos editables del tenant (HU-001).
    /// No se actualizan: id, email_admin (clave de unicidad global), activo,
    /// fecha_creacion, fecha_modificacion (trigger).
    /// </summary>
    public async Task<bool> UpdateAsync(Empresa empresa)
    {
        const string sql = @"
            UPDATE empresas SET
                nombre               = @Nombre,
                ruc_nit              = @RucNit,
                telefono             = @Telefono,
                pais                 = @Pais,
                ciudad               = @Ciudad,
                direccion            = @Direccion,
                logo_url             = @LogoUrl,
                color_primario       = @ColorPrimario,
                color_secundario     = @ColorSecundario,
                plan_suscripcion     = @PlanSuscripcion,
                estado               = @Estado,
                moneda_principal     = @MonedaPrincipal,
                zona_horaria         = @ZonaHoraria,
                idioma               = @Idioma,
                formato_fecha        = @FormatoFecha,
                prefijo_embarque     = @PrefijoEmbarque,
                consecutivo_embarque = @ConsecutivoEmbarque,
                prefijo_orden        = @PrefijoOrden,
                consecutivo_orden    = @ConsecutivoOrden
            WHERE id = @Id
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, empresa);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false WHERE id = @Id. Nunca se borra físicamente.</summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        const string sql = @"
            UPDATE empresas
            SET activo = false
            WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}