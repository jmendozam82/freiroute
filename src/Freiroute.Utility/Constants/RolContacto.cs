namespace Freiroute.Utility.Constants;

/// <summary>
/// Roles de contacto de cliente (tabla 'contactos_cliente.rol', HU-019).
/// El contacto principal recibe las notificaciones del sistema.
/// </summary>
public static class RolContacto
{
    public const string General = "GENERAL";
    public const string Logistica = "LOGISTICA";
    public const string Compras = "COMPRAS";
    public const string Finanzas = "FINANZAS";
    public const string Recepcion = "RECEPCION";
    public const string Gerencia = "GERENCIA";

    /// <summary>Todos los roles válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        General, Logistica, Compras, Finanzas, Recepcion, Gerencia
    };
}