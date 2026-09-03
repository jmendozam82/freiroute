namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de usuario (tabla 'usuarios.tipo_usuario' + JWT claim "tipo_usuario").
/// Aplica a la lógica de autorización y al rol del JWT (ADR-007).
/// </summary>
public static class TipoUsuario
{
    public const string SUPER_ADMIN = "SUPER_ADMIN";
    public const string ADMIN = "ADMIN";
    public const string DISPATCHER = "DISPATCHER";
    public const string OPERADOR = "OPERADOR";
    public const string CONDUCTOR = "CONDUCTOR";
    public const string CLIENTE = "CLIENTE";
}