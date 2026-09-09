namespace Freiroute.Entity;

/// <summary>
/// Reclamo formal de un cliente por incidencias de entrega — daño,
/// pérdida o retraso (HU-032). Corresponde a la tabla 'reclamos'.
/// El número legible se genera al crear (formato REC-{PREFIX}-{AÑO}-{SECUENCIA}).
/// </summary>
public class Reclamo
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid OrdenId { get; set; }                   // FK ordenes(id) NOT NULL

    // ── Identificación legible ───────────────────────────────
    public string? NumeroReclamo { get; set; }          // VARCHAR(30) — REC-{PREFIX}-{AÑO}-{SECUENCIA}

    // ── Detalle del reclamo ──────────────────────────────────
    public string Tipo { get; set; } = string.Empty;            // VARCHAR(20) NOT NULL — ver TipoReclamo (DANO|PERDIDA|RETRASO|OTRO)
    public string Descripcion { get; set; } = string.Empty;     // TEXT NOT NULL
    public decimal? MontoReclamado { get; set; }        // NUMERIC(15,2) — compensación solicitada
    public string Estado { get; set; } = string.Empty;  // VARCHAR(20) NOT NULL DEFAULT 'ABIERTO' — FSM ver EstadoReclamo
    public string[]? ReferenciasEvidencia { get; set; } // TEXT[] — URLs a Supabase Storage (Sprint 11)

    // ── Auditoría estándar ───────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime FechaModificacion { get; set; }     // TIMESTAMPTZ — trigger update_fecha_modificacion()
    public Guid? CreadoPor { get; set; }                // FK usuarios(id)
    public Guid? ModificadoPor { get; set; }            // FK usuarios(id)

    // ── Nombres legibles vía JOIN (NO persistidos) ───────────────
    public string? OrdenNumero { get; set; }            // o.numero_orden (JOIN con ordenes)
    public string? ClienteNombre { get; set; }          // c.nombre (JOIN con clientes)
}