namespace Freiroute.Entity;

/// <summary>
/// Recargo aplicable a una tarifa base: combustible, peaje, seguro,
/// manipulación, urgencia, refrigeración, sobredimensión (ADR-015).
/// Corresponde a la tabla 'recargos_tarifa' (HU-020).
/// </summary>
public class RecargoTarifa
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid TarifaId { get; set; }                  // FK tarifas_base(id) ON DELETE CASCADE

    // ── Definición del recargo ────────────────────────────────
    public string CodigoRecargo { get; set; } = string.Empty; // VARCHAR(50) NOT NULL — UNIQUE (tarifa_id, codigo)
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string TipoCalculo { get; set; } = "PORCENTAJE"; // VARCHAR(30) DEFAULT 'PORCENTAJE'
    public decimal Valor { get; set; }                  // NUMERIC(10,4) NOT NULL — % o monto según tipo_calculo

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}