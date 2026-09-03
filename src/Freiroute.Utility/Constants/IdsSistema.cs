namespace Freiroute.Utility.Constants;

/// <summary>
/// IDs fijos del sistema sembrados en la migración de datos iniciales
/// (20260101000008_datos_iniciales.sql). La empresa raíz del SaaS usa la
/// plataforma Freiroute como tenant propio y sus perfiles base sirven de
/// plantilla (es_sistema = true) para los permisos de cada tenant nuevo (HU-001).
/// </summary>
public static class IdsSistema
{
    /// <summary>Empresa raíz del SaaS: 'Freiroute SaaS Admin' (tenant del Super Admin).</summary>
    public static readonly Guid EmpresaRaizId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>Perfil Super Admin del sistema (acceso total implícito — sin filas en permisos).</summary>
    public static readonly Guid PerfilSuperAdminId = new("00000000-0000-0000-0000-000000000010");

    /// <summary>Perfil plantilla ADMIN (es_sistema) en la empresa raíz.</summary>
    public static readonly Guid PerfilAdminPlantillaId = new("00000000-0000-0000-0000-000000000011");

    /// <summary>
    /// Perfiles base creados automáticamente en TODO tenant nuevo (HU-001 CA-02, HU-006 CA-01).
    /// El SUPER_ADMIN NO se replica a los tenants: sus permisos son implícitos (HU-006 CA-06).
    /// </summary>
    public static readonly string[] PerfilesBaseTenant =
    [
        TipoPerfil.ADMIN,
        TipoPerfil.DISPATCHER,
        TipoPerfil.OPERADOR,
        TipoPerfil.CONDUCTOR,
        TipoPerfil.CLIENTE
    ];
}