namespace Freiroute.Utility.Constants;

/// <summary>
/// Estado calculado de cumplimiento de SLA de una orden (HU-031 CA-04).
/// No viene de BD directamente — se calcula en BLL con
/// <c>ISlaService.CalcularSlaStatus(fechaEntregaRequerida, estadoOrden)</c>.
/// </summary>
public static class SlaStatus
{
    public const string Ok        = "OK";
    public const string EnRiesgo  = "EN_RIESGO";
    public const string Critico   = "CRITICO";
    public const string Vencido   = "VENCIDO";
}