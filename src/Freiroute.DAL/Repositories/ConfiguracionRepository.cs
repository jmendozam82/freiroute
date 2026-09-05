using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de configuración del tenant (HU-014).
/// NO tiene tabla propia — lee y actualiza un subset de campos de la tabla 'empresas'
/// (configuración general, identidad visual, logo, numeración y email remitente).
/// SIEMPRE filtrar por id = empresaId en lecturas.
/// </summary>
public class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly IDbConnection _connection;

    public ConfiguracionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Obtiene la configuración del tenant (subset de campos de la tabla empresas).
    /// Devuelve la entidad Empresa completa — la BLL proyecta solo los campos relevantes.
    /// </summary>
    public async Task<Empresa?> GetConfiguracionAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                     AS Id,
                nombre                 AS Nombre,
                ruc_nit                AS RucNit,
                email_admin            AS EmailAdmin,
                telefono               AS Telefono,
                direccion              AS Direccion,
                industria              AS Industria,
                sitio_web              AS SitioWeb,
                email_remitente        AS EmailRemitente,
                nombre_remitente       AS NombreRemitente,
                logo_url               AS LogoUrl,
                color_primario         AS ColorPrimario,
                color_secundario       AS ColorSecundario,
                moneda_principal       AS MonedaPrincipal,
                zona_horaria           AS ZonaHoraria,
                formato_fecha          AS FormatoFecha,
                modos_transporte_activos AS ModosTransporteActivos,
                prefijo_embarque       AS PrefijoEmbarque,
                consecutivo_embarque   AS ConsecutivoEmbarque,
                prefijo_orden          AS PrefijoOrden,
                consecutivo_orden      AS ConsecutivoOrden,
                prefijo_carta_porte    AS PrefijoCartaPorte,
                consecutivo_carta_porte AS ConsecutivoCartaPorte,
                onboarding_completado  AS OnboardingCompletado,
                activo                 AS Activo
            FROM empresas
            WHERE id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Empresa>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Actualiza los campos de configuración general de la empresa.
    /// Incluye datos de identidad, identidad visual, operativa y email remitente.
    /// </summary>
    public async Task<bool> UpdateConfiguracionAsync(Guid empresaId,
        string nombre, string? rucNit, string? direccion,
        string? telefono, string? industria, string? sitioWeb,
        string colorPrimario, string colorSecundario,
        string moneda, string zonaHoraria, string formatoFecha,
        string? emailRemitente, string? nombreRemitente)
    {
        const string sql = @"
            UPDATE empresas SET
                nombre           = @Nombre,
                ruc_nit          = @RucNit,
                telefono         = @Telefono,
                direccion        = @Direccion,
                industria        = @Industria,
                sitio_web        = @SitioWeb,
                email_remitente  = @EmailRemitente,
                nombre_remitente = @NombreRemitente,
                color_primario   = @ColorPrimario,
                color_secundario = @ColorSecundario,
                moneda_principal = @Moneda,
                zona_horaria     = @ZonaHoraria,
                formato_fecha    = @FormatoFecha
            WHERE id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            EmpresaId = empresaId,
            Nombre = nombre,
            RucNit = rucNit,
            Telefono = telefono,
            Direccion = direccion,
            Industria = industria,
            SitioWeb = sitioWeb,
            EmailRemitente = emailRemitente,
            NombreRemitente = nombreRemitente,
            ColorPrimario = colorPrimario,
            ColorSecundario = colorSecundario,
            Moneda = moneda,
            ZonaHoraria = zonaHoraria,
            FormatoFecha = formatoFecha
        });
        return rows > 0;
    }

    /// <summary>
    /// Actualiza solo el campo logo_url de la empresa.
    /// La URL se almacena como path en Supabase Storage — la signed URL
    /// se genera on-demand en la BLL (ADR-012).
    /// </summary>
    public async Task<bool> UpdateLogoUrlAsync(Guid empresaId, string? logoUrl)
    {
        const string sql = @"
            UPDATE empresas
            SET logo_url = @LogoUrl
            WHERE id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            EmpresaId = empresaId,
            LogoUrl = logoUrl
        });
        return rows > 0;
    }

    /// <summary>
    /// Actualiza los modos de transporte activos del tenant (TEXT[]).
    /// Dapper mapea el parámetro string[] directamente a TEXT[] de PostgreSQL
    /// (HU-012 CA-04, migración 20260202000001 — Fix re-smoke test).
    /// </summary>
    public async Task<bool> UpdateModosTransporteAsync(
        Guid empresaId, string[] modosActivos)
    {
        const string sql = @"
            UPDATE empresas SET
                modos_transporte_activos = @Modos,
                fecha_modificacion       = NOW()
            WHERE id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            EmpresaId = empresaId,
            Modos = modosActivos
        });
        return rows > 0;
    }

    /// <summary>
    /// Actualiza los prefijos de numeración (embarque, orden, carta de porte).
    /// Los consecutivos no se editan aquí — son autoincrementales (HU-014 CA-05).
    /// Los consecutivos los incrementa el sistema al generar documentos.
    /// </summary>
    public async Task<bool> UpdateNumeracionAsync(Guid empresaId,
        string prefijoEmbarque, string prefijoOrden,
        string prefijoCartaPorte)
    {
        const string sql = @"
            UPDATE empresas SET
                prefijo_embarque     = @PrefijoEmbarque,
                prefijo_orden        = @PrefijoOrden,
                prefijo_carta_porte  = @PrefijoCartaPorte
            WHERE id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            EmpresaId = empresaId,
            PrefijoEmbarque = prefijoEmbarque,
            PrefijoOrden = prefijoOrden,
            PrefijoCartaPorte = prefijoCartaPorte
        });
        return rows > 0;
    }
}
