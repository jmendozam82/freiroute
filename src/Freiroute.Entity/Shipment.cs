namespace Freiroute.Entity;

/// <summary>
/// Embarque de transporte — esqueleto mínimo creado en Sprint 4 para
/// soportar la FK ordenes.shipment_id (HU-025). El módulo completo de
/// Shipment Planning se implementa en Sprint 7 (EP-06).
/// Corresponde a la tabla 'shipments'.
/// </summary>
public class Shipment
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string? NumeroShipment { get; set; }         // VARCHAR(30) — nullable (se genera en Sprint 7)

    // ── Estado ────────────────────────────────────────────────
    public string Estado { get; set; } = "PLANNED";     // VARCHAR(30) DEFAULT 'PLANNED'

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}
