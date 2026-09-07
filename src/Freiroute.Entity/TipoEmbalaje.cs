namespace Freiroute.Entity;

/// <summary>
/// Tipos de embalaje del tenant: pallets, cajas, tambores, contenedores.
/// Usados al registrar la carga en las órdenes de transporte.
/// Corresponde a la tabla 'tipos_embalaje' (HU-018).
/// </summary>
public class TipoEmbalaje
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL
    public string Codigo { get; set; } = string.Empty;  // VARCHAR(20) NOT NULL — UNIQUE (empresa_id, codigo): PLT, CAJA, TAM...
    public string? Descripcion { get; set; }            // TEXT

    // ── Capacidad ─────────────────────────────────────────────
    public decimal? CapacidadKg { get; set; }           // NUMERIC(10,3)
    public decimal? CapacidadM3 { get; set; }           // NUMERIC(10,3)
    public bool Apilable { get; set; } = true;          // BOOLEAN DEFAULT true

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}