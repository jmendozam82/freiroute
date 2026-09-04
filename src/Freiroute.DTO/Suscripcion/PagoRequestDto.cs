using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Suscripcion;

/// <summary>
/// Datos de entrada para registrar un pago manual de suscripción (HU-011).
/// Solo el SUPER_ADMIN puede registrar pagos. El pago es inmutable.
/// </summary>
[SwaggerSchema(Description = "DTO para registrar un pago manual de suscripción")]
public class PagoRequestDto
{
    [SwaggerSchema(Description = "ID de la suscripción a la que corresponde el pago", Nullable = false)]
    public Guid SuscripcionId { get; set; }

    [SwaggerSchema(Description = "Monto del pago", Nullable = false)]
    public decimal Monto { get; set; }

    [SwaggerSchema(Description = "Moneda del pago (ISO 4217)")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Método de pago: MANUAL, STRIPE, PAYPAL, TRANSFERENCIA, EFECTIVO")]
    public string MetodoPago { get; set; } = "MANUAL";

    [SwaggerSchema(Description = "Referencia de la transacción")]
    public string? Referencia { get; set; }

    [SwaggerSchema(Description = "Notas adicionales del pago")]
    public string? Notas { get; set; }

    [SwaggerSchema(Description = "Inicio del período cubierto por el pago", Nullable = false)]
    public DateTime PeriodoDesde { get; set; }

    [SwaggerSchema(Description = "Fin del período cubierto por el pago", Nullable = false)]
    public DateTime PeriodoHasta { get; set; }
}
