namespace Freiroute.Entity;

/// <summary>
/// Registro de rechazo de entrega de una orden en campo (HU-030).
/// Corresponde a la tabla 'rechazos_entrega'. El registro del rechazo
/// mueve la orden a FAILED_DELIVERY vía FSM (ADR-019).
/// </summary>
public class RechazoEntrega
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid OrdenId { get; set; }                   // FK ordenes(id) NOT NULL — relación N:1

    // ── Detalle del rechazo ──────────────────────────────────
    public string Motivo { get; set; } = string.Empty;  // VARCHAR(40) NOT NULL — ver TipoRechazo
    public string? Descripcion { get; set; }            // TEXT — detalle libre opcional
    public Guid? UsuarioId { get; set; }                // FK usuarios(id) — quién registra el rechazo

    // ── Auditoría estándar ───────────────────────────────────
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
}