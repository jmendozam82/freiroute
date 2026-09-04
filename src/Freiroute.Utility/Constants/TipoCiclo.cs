namespace Freiroute.Utility.Constants;

/// <summary>
/// Ciclos de facturación de suscripciones (tabla 'suscripciones.tipo_ciclo').
/// MENSUAL: vencimiento cada 30 días desde la fecha de inicio.
/// ANUAL: vencimiento cada 365 días — normalmente con descuento (ADR-004).
/// </summary>
public static class TipoCiclo
{
    public const string MENSUAL = "MENSUAL";
    public const string ANUAL = "ANUAL";
}
