namespace Freiroute.Utility.Constants;

/// <summary>
/// Estados de un pago registrado (tabla 'pagos.estado').
/// Los pagos son inmutables — una vez creados no se editan ni eliminan.
/// </summary>
public static class EstadoPago
{
    public const string COMPLETED = "COMPLETED";
    public const string PENDING = "PENDING";
    public const string FAILED = "FAILED";
    public const string REFUNDED = "REFUNDED";
}
