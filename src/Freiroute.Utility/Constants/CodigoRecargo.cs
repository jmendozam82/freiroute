namespace Freiroute.Utility.Constants;

/// <summary>
/// Códigos de recargo configurables por tarifa
/// (tabla 'recargos_tarifa.codigo_recargo', ADR-015).
/// </summary>
public static class CodigoRecargo
{
    public const string Combustible = "COMBUSTIBLE";
    public const string Peaje = "PEAJE";
    public const string Seguro = "SEGURO";
    public const string Manipulacion = "MANIPULACION";
    public const string Urgencia = "URGENCIA";
    public const string Refrigeracion = "REFRIGERACION";
    public const string Sobredimension = "SOBREDIMENSION";

    /// <summary>Todos los códigos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        Combustible, Peaje, Seguro, Manipulacion,
        Urgencia, Refrigeracion, Sobredimension
    };
}