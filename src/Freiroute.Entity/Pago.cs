namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un pago de suscripción registrado en el sistema.
/// Corresponde a la tabla 'pagos'. Es INMUTABLE — no se edita ni elimina nunca.
/// NO tiene campo 'activo' ni 'fecha_modificacion' (ADR-004).
/// </summary>
public class Pago
{
    // ── Identity ──────────────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()

    // ── Relaciones ────────────────────────────────────────────────
    public Guid EmpresaId { get; set; }                 // FK empresas(id) ON DELETE RESTRICT
    public Guid SuscripcionId { get; set; }             // FK suscripciones(id) ON DELETE RESTRICT

    // ── Datos del pago ────────────────────────────────────────────
    public decimal Monto { get; set; }                  // NUMERIC(10,2) NOT NULL
    public string Moneda { get; set; } = "USD";         // VARCHAR(10) NOT NULL
    public string MetodoPago { get; set; } = "MANUAL"; // MANUAL | STRIPE | PAYPAL | TRANSFERENCIA | EFECTIVO
    public string? Referencia { get; set; }             // VARCHAR(200) — referencia de la transacción
    public string? Notas { get; set; }                  // TEXT — notas del pago

    // ── Estado ────────────────────────────────────────────────────
    public string Estado { get; set; } = "COMPLETED";  // COMPLETED | PENDING | FAILED | REFUNDED

    // ── Período cubierto ──────────────────────────────────────────
    public DateTime PeriodoDesde { get; set; }          // TIMESTAMPTZ — inicio del período pagado
    public DateTime PeriodoHasta { get; set; }          // TIMESTAMPTZ — fin del período pagado

    // ── Auditoría de creación ─────────────────────────────────────
    public Guid? RegistradoPorId { get; set; }          // FK usuarios(id) — quién registró el pago

    // ── Timestamp inmutable ───────────────────────────────────────
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ NOT NULL
    // SIN FechaModificacion — los pagos son inmutables
}
