namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados de la máquina de vida de una orden de transporte (ADR-019).
/// La tabla 'ordenes' persiste el estado como VARCHAR para facilitar
/// la lectura en SQL y reportes sin joins adicionales.
/// </summary>
public static class OrdenEstado
{
    public const string Draft           = "DRAFT";
    public const string Confirmed       = "CONFIRMED";
    public const string Assigned        = "ASSIGNED";
    public const string PickupScheduled = "PICKUP_SCHEDULED";
    public const string InTransit       = "IN_TRANSIT";
    public const string Delivered       = "DELIVERED";
    public const string Invoiced        = "INVOICED";
    public const string Closed          = "CLOSED";
    public const string Cancelled       = "CANCELLED";
    public const string OnHold          = "ON_HOLD";
    public const string FailedDelivery  = "FAILED_DELIVERY";
    public const string PartiallySplit  = "PARTIALLY_SPLIT";

    /// <summary>Estados finales — no admiten transición de salida.</summary>
    public static readonly IReadOnlySet<string> Terminales =
        new HashSet<string>(StringComparer.Ordinal) { Closed, Cancelled };

    /// <summary>Estados en los que la orden puede ser editada por el operador.</summary>
    public static readonly IReadOnlySet<string> Editables =
        new HashSet<string>(StringComparer.Ordinal) { Draft, Confirmed };

    /// <summary>Estados en los que la orden puede ser desactivada (soft delete).</summary>
    public static readonly IReadOnlySet<string> Desactivables =
        new HashSet<string>(StringComparer.Ordinal) { Draft };

    /// <summary>Etiquetas legibles de cada estado para la UI (idioma español).</summary>
    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Draft]           = "Borrador",
            [Confirmed]       = "Confirmada",
            [Assigned]        = "Asignada",
            [PickupScheduled] = "Recogida programada",
            [InTransit]       = "En tránsito",
            [Delivered]       = "Entregada",
            [Invoiced]        = "Facturada",
            [Closed]          = "Cerrada",
            [Cancelled]       = "Cancelada",
            [OnHold]          = "En espera",
            [FailedDelivery]  = "Entrega fallida",
            [PartiallySplit]  = "Dividida parcialmente",
        };

    /// <summary>Retorna la etiqueta legible del estado, o el código si no existe.</summary>
    public static string GetLabel(string estado) =>
        Labels.TryGetValue(estado, out var label) ? label : estado;
}

/// <summary>
/// Niveles de servicio de una orden de transporte.
/// Corresponde a 'ordenes.nivel_servicio' VARCHAR(20) DEFAULT 'ESTANDAR'.
/// </summary>
public static class NivelServicio
{
    public const string Estandar    = "ESTANDAR";
    public const string Express     = "EXPRESS";
    public const string Programado  = "PROGRAMADO";

    public static readonly string[] Todos = { Estandar, Express, Programado };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Estandar]   = "Estándar",
            [Express]    = "Express",
            [Programado] = "Programado",
        };

    public static string GetLabel(string nivel) =>
        Labels.TryGetValue(nivel, out var label) ? label : nivel;
}

/// <summary>
/// Prioridad de una orden de transporte.
/// Corresponde a 'ordenes.prioridad' VARCHAR(20) DEFAULT 'NORMAL'.
/// </summary>
public static class OrdenPrioridad
{
    public const string Critico = "CRITICO";
    public const string Alto    = "ALTO";
    public const string Normal  = "NORMAL";
    public const string Bajo    = "BAJO";

    public static readonly string[] Todos = { Critico, Alto, Normal, Bajo };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Critico] = "Crítico",
            [Alto]    = "Alto",
            [Normal]  = "Normal",
            [Bajo]    = "Bajo",
        };

    public static string GetLabel(string prioridad) =>
        Labels.TryGetValue(prioridad, out var label) ? label : prioridad;
}

/// <summary>
/// Canal de ingreso de la orden — determina cómo se originó.
/// Corresponde a 'ordenes.origen_creacion' VARCHAR(20) DEFAULT 'MANUAL'.
/// </summary>
public static class OrigenCreacion
{
    public const string Manual     = "MANUAL";
    public const string Csv        = "CSV";
    public const string Api        = "API";
    public const string Recurrente = "RECURRENTE";

    public static readonly string[] Todos = { Manual, Csv, Api, Recurrente };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Manual]     = "Manual",
            [Csv]        = "Importación CSV",
            [Api]        = "API externa",
            [Recurrente] = "Recurrente",
        };

    public static string GetLabel(string origen) =>
        Labels.TryGetValue(origen, out var label) ? label : origen;
}

/// <summary>
/// Frecuencias de recurrencia para plantillas de orden (HU-027).
/// Corresponde a 'plantillas_orden.frecuencia_recurrencia' VARCHAR(20).
/// </summary>
public static class FrecuenciaRecurrencia
{
    public const string Diaria    = "DIARIA";
    public const string Semanal   = "SEMANAL";
    public const string Quincenal = "QUINCENAL";
    public const string Mensual   = "MENSUAL";

    public static readonly string[] Todos = { Diaria, Semanal, Quincenal, Mensual };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Diaria]    = "Diaria",
            [Semanal]   = "Semanal",
            [Quincenal] = "Quincenal",
            [Mensual]   = "Mensual",
        };

    public static string GetLabel(string frecuencia) =>
        Labels.TryGetValue(frecuencia, out var label) ? label : frecuencia;

    public static DateOnly CalcularProximaEjecucion(string frecuencia, DateOnly fechaActual)
    {
        return frecuencia.ToUpper() switch
        {
            Diaria => fechaActual.AddDays(1),
            Semanal => fechaActual.AddDays(7),
            Quincenal => fechaActual.AddDays(15),
            Mensual => fechaActual.AddMonths(1),
            _ => fechaActual.AddDays(1)
        };
    }
}
