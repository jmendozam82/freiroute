namespace Freiroute.Aplicacion.Helpers;

/// <summary>
/// Helpers de badges del Design System Freiroute para vistas Razor
/// server-side (espejo de FrBadge en freiroute.js). Devuelven la clase
/// CSS .fr-badge-* correspondiente a cada estado/categoría del TMS.
/// </summary>
public static class FrBadges
{
    /// <summary>Badge de tipo de ubicación (HU-015).</summary>
    public static string TipoUbicacion(string tipo) => tipo switch
    {
        "ALMACEN"        => "fr-badge-info",
        "CLIENTE"        => "fr-badge-success",
        "PUERTO"         => "fr-badge-warning",
        "AEROPUERTO"     => "fr-badge-info",
        "TERMINAL"       => "fr-badge-neutral",
        "CRUCE_FRONTERA" => "fr-badge-danger",
        "PUNTO_RECARGA"  => "fr-badge-info",
        _                => "fr-badge-neutral"
    };

    /// <summary>Badge de tipo de cliente (HU-019).</summary>
    public static string TipoCliente(string tipo) => tipo switch
    {
        "VIP"         => "fr-badge-success",
        "CORPORATIVO" => "fr-badge-info",
        "GOBIERNO"    => "fr-badge-info",
        "REGULAR"     => "fr-badge-neutral",
        "OCASIONAL"   => "fr-badge-neutral",
        _             => "fr-badge-neutral"
    };

    /// <summary>Badge de estado de crédito (HU-019 CA-05).</summary>
    public static string EstadoCredito(string estado) => estado switch
    {
        "AL_DIA"      => "fr-badge-success",
        "EN_MORA"     => "fr-badge-warning",
        "BLOQUEADO"   => "fr-badge-danger",
        "SIN_CREDITO" => "fr-badge-neutral",
        _             => "fr-badge-neutral"
    };

    /// <summary>Badge de vigencia de tarifa (HU-020 CA-06).</summary>
    public static string VigenciaTarifa(bool esVigente, bool esVencida) =>
        esVencida ? "fr-badge-neutral" : esVigente ? "fr-badge-success" : "fr-badge-warning";

    /// <summary>Badge de HAZMAT (HU-017 CA-07): peligrosas se resaltan en danger.</summary>
    public static string MercanciaPeligrosa(bool esHazmat) =>
        esHazmat ? "fr-badge-danger" : "fr-badge-neutral";
}