namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de servicio de transporte (tabla 'tarifas_base.tipo_servicio',
/// 'embarques.tipo_servicio').
/// </summary>
public static class TipoServicioTransporte
{
    public const string Estandar = "ESTANDAR";
    public const string Express = "EXPRESS";
    public const string Programado = "PROGRAMADO";
    public const string Refrigerado = "REFRIGERADO";
    public const string Peligroso = "PELIGROSO";

    /// <summary>Todos los tipos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        Estandar, Express, Programado, Refrigerado, Peligroso
    };
}