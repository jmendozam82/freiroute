namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados del tenant (tabla 'empresas.estado').
/// Se usa en BLL/API para validar transiciones y construir respuestas.
/// Sprint 2: se agregan TRIAL y PAST_DUE para soporte de facturación (ADR-004).
/// </summary>
public static class EstadoEmpresa
{
    public const string ACTIVE = "ACTIVE";
    public const string SUSPENDED = "SUSPENDED";
    public const string CANCELLED = "CANCELLED";
    public const string TRIAL = "TRIAL";
    public const string PAST_DUE = "PAST_DUE";
}