namespace Freiroute.Utility.Constants;

/// <summary>
/// Módulos del TMS con permiso granular (HU-006 CA-04). Los valores son los
/// nombres exactos almacenados en 'permisos.modulo' y usados en los claims del
/// JWT con formato "modulo:read|create|update" (ej: "embarques:read").
/// </summary>
public static class ModuloPermiso
{
    public const string Ordenes = "ordenes";
    public const string Embarques = "embarques";
    public const string Carriers = "carriers";
    public const string Rutas = "rutas";
    public const string TrackTrace = "track_trace";
    public const string Documentos = "documentos";
    public const string Flota = "flota";
    public const string Analytics = "analytics";
    public const string Facturacion = "facturacion";
    public const string Clientes = "clientes";
    public const string Usuarios = "usuarios";
    public const string Configuracion = "configuracion";

    /// <summary>Lista completa de módulos — usada para seeding de permisos base de perfiles.</summary>
    public static readonly string[] Todos =
    [
        Ordenes, Embarques, Carriers, Rutas, TrackTrace, Documentos,
        Flota, Analytics, Facturacion, Clientes, Usuarios, Configuracion
    ];
}