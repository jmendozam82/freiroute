namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa una suscripción activa de un tenant a un plan.
/// Corresponde a la tabla 'suscripciones'. Una empresa tiene UNA suscripción activa
/// a la vez (constraint UNIQUE(empresa_id, activo)). El precio pactado puede diferir
/// del precio actual del plan — se congela al contratar (ADR-004).
/// </summary>
public class Suscripcion
{
    // ── Identity / Lifecycle ──────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Relaciones ────────────────────────────────────────────────
    public Guid EmpresaId { get; set; }                 // FK empresas(id) ON DELETE RESTRICT
    public Guid PlanId { get; set; }                    // FK planes(id) ON DELETE RESTRICT

    // ── Ciclo de facturación ──────────────────────────────────────
    public string TipoCiclo { get; set; } = "MENSUAL";              // MENSUAL | ANUAL
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaCancelacion { get; set; }

    // ── Estado ────────────────────────────────────────────────────
    public string Estado { get; set; } = "TRIAL";                    // TRIAL | ACTIVE | PAST_DUE | SUSPENDED | CANCELLED

    // ── Precio pactado al contratar ───────────────────────────────
    public decimal PrecioPactado { get; set; }                        // NUMERIC(10,2) — puede diferir del plan actual
    public string MonedaPactada { get; set; } = "USD";               // VARCHAR(10) NOT NULL

    // ── Auditoría de creación ─────────────────────────────────────
    public Guid? CreadoPorId { get; set; }                           // FK usuarios(id) — Super Admin que creó la suscripción
}
