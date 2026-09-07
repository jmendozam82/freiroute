namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de tarifa base (tabla 'tarifas_base.tipo_tarifa', ADR-015).
/// Determinan la unidad sobre la que se calcula el precio: por kg, por m³,
/// por km, fijo por viaje o por unidad (pallet, caja).
/// </summary>
public static class TipoTarifa
{
    public const string PorKg = "POR_KG";
    public const string PorM3 = "POR_M3";
    public const string PorKm = "POR_KM";
    public const string FijoViaje = "FIJO_VIAJE";
    public const string PorUnidad = "POR_UNIDAD";

    /// <summary>Todos los tipos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        PorKg, PorM3, PorKm, FijoViaje, PorUnidad
    };
}