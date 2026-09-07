namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de cálculo de un recargo de tarifa
/// (tabla 'recargos_tarifa.tipo_calculo', ADR-015).
/// PORCENTAJE se aplica sobre la tarifa base; MONTO_FIJO es un
/// valor absoluto en la moneda de la tarifa.
/// </summary>
public static class TipoCalculoRecargo
{
    public const string Porcentaje = "PORCENTAJE";
    public const string MontoFijo = "MONTO_FIJO";
}