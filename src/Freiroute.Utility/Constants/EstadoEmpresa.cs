namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados del tenant (tabla 'empresas.estado').
/// Se usa en BLL/API para validar transiciones y construir respuestas.
/// </summary>
public static class EstadoEmpresa
{
    public const string ACTIVE = "ACTIVE";
    public const string SUSPENDED = "SUSPENDED";
    public const string CANCELLED = "CANCELLED";
}