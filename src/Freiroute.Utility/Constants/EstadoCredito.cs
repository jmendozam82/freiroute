namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados de crédito de un cliente (tabla 'clientes.estado_credito', HU-019).
/// Los clientes con estado BLOQUEADO muestran alerta visual en toda la UI.
/// </summary>
public static class EstadoCredito
{
    public const string AlDia = "AL_DIA";
    public const string EnMora = "EN_MORA";
    public const string Bloqueado = "BLOQUEADO";
    public const string SinCredito = "SIN_CREDITO";
}