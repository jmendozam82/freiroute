namespace Freiroute.Utility.Constants;

/// <summary>
/// Acciones estándar registradas en 'auditoria_actividad.accion' (HU-008 CA-01).
/// El log es inmutable y registra automáticamente cada operación del sistema.
/// </summary>
public static class AccionAuditoria
{
    public const string LOGIN = "LOGIN";
    public const string LOGOUT = "LOGOUT";
    public const string LOGIN_FAILED = "LOGIN_FAILED";
    public const string CREATE = "CREATE";
    public const string UPDATE = "UPDATE";
    public const string DEACTIVATE = "DEACTIVATE";
    public const string EXPORT = "EXPORT";
    public const string CAMBIO_ESTADO = "CAMBIO_ESTADO";
}