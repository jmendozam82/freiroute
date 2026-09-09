namespace Freiroute.Entity;

/// <summary>
/// Registro inmutable de auditoría de transiciones de estado de una orden.
/// Cada cambio de estado queda registrado con usuario, timestamp y motivo
/// opcional (ADR-019). Corresponde a la tabla 'historial_estados_orden'.
/// </summary>
public class HistorialEstadoOrden
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid OrdenId { get; set; }                   // FK ordenes(id)

    // ── Transición ───────────────────────────────────────────
    public string? EstadoAnterior { get; set; }         // VARCHAR(30) — NULL en inserción inicial del DRAFT
    public string EstadoNuevo { get; set; } = string.Empty; // VARCHAR(30) NOT NULL
    public string? Motivo { get; set; }                 // TEXT — opcional (requerido en cancelaciones)
    public Guid? UsuarioId { get; set; }                // FK usuarios(id) — quién ejecutó la transición
    public string? UsuarioNombre { get; set; }          // u.nombre vía JOIN — NO persistido (G-17B)

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}
