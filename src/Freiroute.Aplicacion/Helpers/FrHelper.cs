namespace Freiroute.Aplicacion.Helpers;

/// <summary>
/// Helper estático para vistas Razor que mapea estados del TMS a clases CSS de badge.
/// Mantiene consistencia visual: cada estado siempre usa el mismo color en todas las vistas.
/// Referencia: Design System Freiroute v1.0 — Sección 10 Badges de Estado TMS.
/// </summary>
public static class FrHelper
{
    /// <summary>
    /// Retorna la clase CSS del badge correspondiente al estado operacional del TMS.
    /// Se usa en vistas con: class="fr-badge @FrHelper.BadgeClase(item.Estado)"
    /// Nunca colores hardcodeados — solo clases del Design System.
    /// </summary>
    public static string BadgeClase(string? estado) => estado switch
    {
        // Estados de embarque / operación
        "DRAFT"            => "fr-badge-neutral",
        "CONFIRMED"        => "fr-badge-info",
        "ASSIGNED"         => "fr-badge-info",
        "PICKUP_SCHEDULED" => "fr-badge-info",
        "IN_TRANSIT"       => "fr-badge-warning",
        "DELIVERED"        => "fr-badge-success",
        "INVOICED"         => "fr-badge-success",
        "CLOSED"           => "fr-badge-neutral",
        "CANCELLED"        => "fr-badge-danger",
        "ON_HOLD"          => "fr-badge-warning",
        "FAILED_DELIVERY"  => "fr-badge-danger",

        // Estados de empresa / tenant
        "ACTIVE"           => "fr-badge-success",
        "SUSPENDED"        => "fr-badge-warning",

        // Estados de usuario
        "PENDING"          => "fr-badge-warning",
        "LOCKED"           => "fr-badge-danger",

        // Estados de documentos
        "VIGENTE"          => "fr-badge-success",
        "POR_VENCER"       => "fr-badge-warning",
        "VENCIDO"          => "fr-badge-danger",

        _                  => "fr-badge-neutral"
    };

    /// <summary>
    /// Retorna el label en español de un estado dado.
    /// Útil para mostrar el nombre legible del estado en la UI.
    /// </summary>
    public static string EstadoLabel(string? estado) => estado switch
    {
        "DRAFT"            => "Borrador",
        "CONFIRMED"        => "Confirmado",
        "ASSIGNED"         => "Asignado",
        "PICKUP_SCHEDULED" => "Pickup programado",
        "IN_TRANSIT"       => "En tránsito",
        "DELIVERED"        => "Entregado",
        "INVOICED"         => "Facturado",
        "CLOSED"           => "Cerrado",
        "CANCELLED"        => "Cancelado",
        "ON_HOLD"          => "En espera",
        "FAILED_DELIVERY"  => "Entrega fallida",

        "ACTIVE"           => "Activa",
        "SUSPENDED"        => "Suspendida",

        "PENDING"          => "Pendiente",
        "LOCKED"           => "Bloqueado",

        "VIGENTE"          => "Vigente",
        "POR_VENCER"       => "Por vencer",
        "VENCIDO"          => "Vencido",

        _                  => estado ?? "Sin estado"
    };

    /// <summary>
    /// Retorna la clase CSS del badge de un plan de suscripción.
    /// STARTER=neutral, PROFESSIONAL=info, ENTERPRISE=success.
    /// </summary>
    public static string PlanBadgeClase(string? plan) => plan switch
    {
        "ENTERPRISE"    => "fr-badge-success",
        "PROFESSIONAL"  => "fr-badge-info",
        "STARTER"       => "fr-badge-neutral",
        _               => "fr-badge-neutral"
    };
}
