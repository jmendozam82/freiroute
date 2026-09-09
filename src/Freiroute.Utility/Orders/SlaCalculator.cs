using Freiroute.Utility.Constants;

namespace Freiroute.Utility.Orders;

/// <summary>
/// Cálculo puro del estado de SLA de una orden (HU-031 CA-02/CA-04/CA-05/CA-06).
/// Es la única fuente de verdad para los umbrales:
///   · estado final (DELIVERED/CLOSED/CANCELLED) → OK
///   · sin fecha de entrega requerida → OK (sin compromiso formal)
///   · fecha requerida vencida → VENCIDO
///   · ≤ 6 h restantes → CRITICO
///   · ≤ 24 h restantes → EN_RIESGO
///   · resto → OK
/// </summary>
public static class SlaCalculator
{
    public static string Calcular(DateTime? fechaEntregaRequerida, string estadoOrden)
    {
        if (EsEstadoFinal(estadoOrden))
        {
            return SlaStatus.Ok;
        }

        if (fechaEntregaRequerida is null)
        {
            return SlaStatus.Ok;
        }

        var ahora = DateTime.UtcNow;
        var fechaRequerida = fechaEntregaRequerida.Value;

        if (fechaRequerida <= ahora)
        {
            return SlaStatus.Vencido;
        }

        var horasRestantes = (fechaRequerida - ahora).TotalHours;

        if (horasRestantes <= 6)
        {
            return SlaStatus.Critico;
        }

        if (horasRestantes <= 24)
        {
            return SlaStatus.EnRiesgo;
        }

        return SlaStatus.Ok;
    }

    public static bool EsEstadoFinal(string estado) =>
        estado is OrdenEstado.Delivered or OrdenEstado.Closed or OrdenEstado.Cancelled;
}