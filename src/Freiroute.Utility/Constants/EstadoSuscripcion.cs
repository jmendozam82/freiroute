namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados de suscripción (tabla 'suscripciones.estado').
/// Controla el ciclo de vida de la facturación de cada tenant (ADR-004).
/// </summary>
public static class EstadoSuscripcion
{
    public const string TRIAL = "TRIAL";
    public const string ACTIVE = "ACTIVE";
    public const string PAST_DUE = "PAST_DUE";
    public const string SUSPENDED = "SUSPENDED";
    public const string CANCELLED = "CANCELLED";
}
