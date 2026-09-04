namespace Freiroute.Utility.Constants;

/// <summary>
/// Métodos de pago disponibles (tabla 'pagos.metodo_pago').
/// Sprint 2: solo MANUAL (registro manual por Super Admin).
/// Stripe y PayPal se integran en Sprint 13 (EP-10).
/// </summary>
public static class MetodoPago
{
    public const string MANUAL = "MANUAL";
    public const string STRIPE = "STRIPE";
    public const string PAYPAL = "PAYPAL";
    public const string TRANSFERENCIA = "TRANSFERENCIA";
    public const string EFECTIVO = "EFECTIVO";
}
