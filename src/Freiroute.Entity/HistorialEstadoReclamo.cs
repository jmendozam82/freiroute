namespace Freiroute.Entity;

/// <summary>
/// Registro inmutable de auditoría de transiciones de estado de un
/// reclamo (HU-032). Mismo patrón que 'historial_estados_orden'
/// (ADR-019) — INSERT-only en operación normal.
/// Corresponde a la tabla 'historial_estados_reclamo'.
/// </summary>
public class HistorialEstadoReclamo
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid ReclamoId { get; set; }                 // FK reclamos(id)

    // ── Transición ───────────────────────────────────────────
    public string? EstadoAnterior { get; set; }         // VARCHAR(20) — NULL en inserción inicial
    public string EstadoNuevo { get; set; } = string.Empty; // VARCHAR(20) NOT NULL
    public string? Motivo { get; set; }                 // TEXT — obligatorio en cada transición (CA-04)
    public Guid? UsuarioId { get; set; }                // FK usuarios(id) — quién ejecutó la transición
    public string? UsuarioNombre { get; set; }          // u.nombre_completo vía JOIN — NO persistido

    // ── Control ───────────────────────────────────────────────
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
}