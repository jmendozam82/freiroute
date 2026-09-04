using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Suscripcion;

/// <summary>
/// Datos de salida de un pago registrado (HU-011).
/// Los pagos son inmutables — no se pueden editar ni eliminar.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de un pago registrado")]
public class PagoResponseDto
{
    [SwaggerSchema(Description = "ID único del pago")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "ID de la empresa (tenant)")]
    public Guid EmpresaId { get; set; }

    [SwaggerSchema(Description = "Nombre de la empresa")]
    public string EmpresaNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID de la suscripción a la que corresponde")]
    public Guid SuscripcionId { get; set; }

    [SwaggerSchema(Description = "Monto del pago")]
    public decimal Monto { get; set; }

    [SwaggerSchema(Description = "Moneda del pago")]
    public string Moneda { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Método de pago utilizado")]
    public string MetodoPago { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Referencia de la transacción")]
    public string? Referencia { get; set; }

    [SwaggerSchema(Description = "Estado del pago: COMPLETED, PENDING, FAILED, REFUNDED")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Inicio del período cubierto por el pago")]
    public DateTime PeriodoDesde { get; set; }

    [SwaggerSchema(Description = "Fin del período cubierto por el pago")]
    public DateTime PeriodoHasta { get; set; }

    [SwaggerSchema(Description = "Nombre del usuario que registró el pago")]
    public string RegistradoPorNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Fecha de creación del registro (inmutable)")]
    public DateTime FechaCreacion { get; set; }
}
