using Freiroute.Utility.Constants;

namespace Freiroute.Aplicacion.Helpers;

/// <summary>
/// Helpers de UI del módulo de órdenes de transporte (HU-021, HU-024).
/// El mapeo de colores de estado sigue ADR-019 y la nota 7 del QA report
/// del Sprint 4 (badge-fr → fr-badge-*): PARTIALLY_SPLIT → info y
/// CANCELLED/CLOSED → neutral. Independiente de FrHelper.BadgeClase
/// (que no incluye PARTIALLY_SPLIT y mapeaba CANCELLED a danger).
/// </summary>
public static class OrdenUiHelper
{
    private static readonly IReadOnlyDictionary<string, string> EstadoBadge =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrdenEstado.Draft]           = "fr-badge-neutral",
            [OrdenEstado.Confirmed]       = "fr-badge-info",
            [OrdenEstado.Assigned]        = "fr-badge-info",
            [OrdenEstado.PickupScheduled] = "fr-badge-info",
            [OrdenEstado.InTransit]       = "fr-badge-warning",
            [OrdenEstado.Delivered]       = "fr-badge-success",
            [OrdenEstado.Invoiced]        = "fr-badge-success",
            [OrdenEstado.Closed]          = "fr-badge-neutral",
            [OrdenEstado.Cancelled]       = "fr-badge-neutral",
            [OrdenEstado.OnHold]          = "fr-badge-warning",
            [OrdenEstado.FailedDelivery]  = "fr-badge-danger",
            [OrdenEstado.PartiallySplit]  = "fr-badge-info",
        };

    private static readonly IReadOnlyDictionary<string, string> PrioridadBadge =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrdenPrioridad.Critico] = "fr-badge-danger",
            [OrdenPrioridad.Alto]    = "fr-badge-warning",
            [OrdenPrioridad.Normal]  = "fr-badge-neutral",
            [OrdenPrioridad.Bajo]    = "fr-badge-neutral",
        };

    private static readonly IReadOnlyDictionary<string, string> AccionClass =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrdenEstado.Cancelled]      = "fr-btn-danger",
            [OrdenEstado.FailedDelivery] = "fr-btn-danger",
        };

    private static readonly IReadOnlyDictionary<string, string> AccionLabel =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrdenEstado.Confirmed]       = "Confirmar",
            [OrdenEstado.Assigned]        = "Asignar a embarque",
            [OrdenEstado.PickupScheduled] = "Programar recogida",
            [OrdenEstado.InTransit]       = "Iniciar tránsito",
            [OrdenEstado.Delivered]       = "Confirmar entrega",
            [OrdenEstado.Invoiced]        = "Generar factura",
            [OrdenEstado.Closed]          = "Cerrar orden",
            [OrdenEstado.Cancelled]       = "Cancelar",
            [OrdenEstado.OnHold]          = "Poner en espera",
            [OrdenEstado.FailedDelivery]  = "Marcar entrega fallida",
            [OrdenEstado.PartiallySplit]  = "Dividir orden",
        };

    private static readonly IReadOnlyDictionary<string, string> AccionIcon =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrdenEstado.Confirmed]       = "ti-check",
            [OrdenEstado.Assigned]        = "ti-truck",
            [OrdenEstado.PickupScheduled] = "ti-calendar-event",
            [OrdenEstado.InTransit]       = "ti-navigation",
            [OrdenEstado.Delivered]       = "ti-package-check",
            [OrdenEstado.Invoiced]        = "ti-receipt",
            [OrdenEstado.Closed]          = "ti-lock",
            [OrdenEstado.Cancelled]       = "ti-x",
            [OrdenEstado.OnHold]          = "ti-pause",
            [OrdenEstado.FailedDelivery]  = "ti-alert-triangle",
            [OrdenEstado.PartiallySplit]  = "ti-cut",
        };

    /// <summary>CSS class del badge de estado (ADR-019 / QA nota 7).</summary>
    public static string GetEstadoClass(string estado) =>
        EstadoBadge.TryGetValue(estado, out var clase) ? clase : "fr-badge-neutral";

    /// <summary>Etiqueta legible del estado (español).</summary>
    public static string GetEstadoLabel(string estado) => OrdenEstado.GetLabel(estado);

    /// <summary>CSS class del badge de prioridad.</summary>
    public static string GetPrioridadClass(string prioridad) =>
        PrioridadBadge.TryGetValue(prioridad, out var clase) ? clase : "fr-badge-neutral";

    /// <summary>Etiqueta legible de la prioridad (español).</summary>
    public static string GetPrioridadLabel(string prioridad) => OrdenPrioridad.GetLabel(prioridad);

    /// <summary>
    /// Variante de botón para la transición de estado destino.
    /// Las transiciones destructivas (cancelar / entrega fallida) usan
    /// fr-btn-danger; el resto usa fr-btn-secondary (un solo primario por página).
    /// </summary>
    public static string GetAccionClass(string estadoDestino) =>
        AccionClass.TryGetValue(estadoDestino, out var clase) ? clase : "fr-btn-secondary";

    /// <summary>Etiqueta ES de la acción de transición (botón FSM).</summary>
    public static string GetAccionLabel(string estadoDestino) =>
        AccionLabel.TryGetValue(estadoDestino, out var label) ? label : estadoDestino;

    /// <summary>Icono Tabler de la acción de transición.</summary>
    public static string GetAccionIcon(string estadoDestino) =>
        AccionIcon.TryGetValue(estadoDestino, out var icono) ? icono : "ti-arrow-right";
}