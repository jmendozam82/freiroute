namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de cliente (tabla 'clientes.tipo_cliente', HU-019).
/// </summary>
public static class TipoCliente
{
    public const string Regular = "REGULAR";
    public const string Vip = "VIP";
    public const string Ocasional = "OCASIONAL";
    public const string Corporativo = "CORPORATIVO";
    public const string Gobierno = "GOBIERNO";

    /// <summary>Todos los tipos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        Regular, Vip, Ocasional, Corporativo, Gobierno
    };
}