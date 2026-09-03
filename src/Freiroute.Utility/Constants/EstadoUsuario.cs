namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados del usuario (tabla 'usuarios.estado').
/// PENDING = invitado sin activar; ACTIVE = operativo; SUSPENDED = suspendido por admin;
/// LOCKED = bloqueado por intentos fallidos (5 consecutivos → 30 min).
/// </summary>
public static class EstadoUsuario
{
    public const string PENDING = "PENDING";
    public const string ACTIVE = "ACTIVE";
    public const string SUSPENDED = "SUSPENDED";
    public const string LOCKED = "LOCKED";
}