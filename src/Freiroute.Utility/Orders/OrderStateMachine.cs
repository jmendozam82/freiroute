using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;

namespace Freiroute.Utility.Orders;

/// <summary>
/// Máquina de estados finitos (FSM) para las órdenes de transporte (ADR-019).
/// Es una clase estática pura sin dependencias de infraestructura, y es la
/// **única fuente de verdad** para las transiciones válidas del sistema.
/// La lógica de guards de rol vive en OrderService, no aquí.
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        [OrdenEstado.Draft]           = [OrdenEstado.Confirmed, OrdenEstado.Cancelled],

        [OrdenEstado.Confirmed]       = [OrdenEstado.Assigned, OrdenEstado.OnHold,
                                         OrdenEstado.PartiallySplit, OrdenEstado.Cancelled],

        [OrdenEstado.Assigned]        = [OrdenEstado.PickupScheduled, OrdenEstado.OnHold,
                                         OrdenEstado.Cancelled],

        [OrdenEstado.PickupScheduled] = [OrdenEstado.InTransit, OrdenEstado.OnHold],

        [OrdenEstado.InTransit]       = [OrdenEstado.Delivered, OrdenEstado.FailedDelivery,
                                         OrdenEstado.OnHold],

        [OrdenEstado.Delivered]       = [OrdenEstado.Invoiced],

        [OrdenEstado.Invoiced]        = [OrdenEstado.Closed],

        [OrdenEstado.OnHold]          = [OrdenEstado.Confirmed],

        [OrdenEstado.FailedDelivery]  = [OrdenEstado.InTransit],

        [OrdenEstado.PartiallySplit]  = [],  // Las sub-órdenes siguen su propio ciclo FSM

        [OrdenEstado.Closed]          = [],  // Estado terminal — inmutable

        [OrdenEstado.Cancelled]       = [],  // Estado terminal — inmutable
    };

    /// <summary>
    /// Determina si la transición desde <paramref name="from"/> hacia
    /// <paramref name="to"/> es válida según la FSM (ADR-019).
    /// </summary>
    public static bool CanTransition(string from, string to) =>
        ValidTransitions.TryGetValue(from, out var set) && set.Contains(to);

    /// <summary>
    /// Retorna el conjunto de estados a los que se puede transicionar
    /// desde el estado dado. Útil para la UI: solo mostrar botones de
    /// acción correspondientes a transiciones disponibles.
    /// </summary>
    public static IReadOnlySet<string> GetNextStates(string from) =>
        ValidTransitions.TryGetValue(from, out var set)
            ? set
            : (IReadOnlySet<string>)new HashSet<string>();

    /// <summary>
    /// Valida la transición y lanza BusinessException(422) si no es válida.
    /// Debe llamarse antes de cualquier escritura en BD.
    /// </summary>
    /// <param name="from">Estado actual de la orden.</param>
    /// <param name="to">Estado destino deseado.</param>
    /// <exception cref="BusinessException">
    /// Si la transición <paramref name="from"/> → <paramref name="to"/>
    /// no está permitida por la FSM.
    /// </exception>
    public static void AssertTransition(string from, string to)
    {
        if (!CanTransition(from, to))
        {
            throw new BusinessException(
                $"Transición de estado inválida: " +
                $"{OrdenEstado.GetLabel(from)} → {OrdenEstado.GetLabel(to)}",
                "ORDEN_TRANSICION_INVALIDA");
        }
    }
}
