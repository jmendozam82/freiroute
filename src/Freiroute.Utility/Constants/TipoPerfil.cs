namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de perfil (tabla 'perfiles.tipo_perfil'). Igual a TipoUsuario
/// más el tipo CUSTOM para perfiles personalizados creados por el Admin (HU-006).
/// </summary>
public static class TipoPerfil
{
    public const string SUPER_ADMIN = "SUPER_ADMIN";
    public const string ADMIN = "ADMIN";
    public const string DISPATCHER = "DISPATCHER";
    public const string OPERADOR = "OPERADOR";
    public const string CONDUCTOR = "CONDUCTOR";
    public const string CLIENTE = "CLIENTE";
    public const string CUSTOM = "CUSTOM";
}