namespace Freiroute.Entity;

/// <summary>
/// Catálogo de unidades de medida del tenant para peso, volumen,
/// longitud y temperatura. Incluye factor de conversión a la unidad base.
/// Corresponde a la tabla 'unidades_medida' (HU-018).
/// </summary>
public class UnidadMedida
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL
    public string Simbolo { get; set; } = string.Empty; // VARCHAR(20) NOT NULL — UNIQUE (empresa_id, simbolo)
    public string Tipo { get; set; } = string.Empty;    // VARCHAR(50) NOT NULL — PESO, VOLUMEN, LONGITUD, TEMPERATURA

    // ── Conversión ────────────────────────────────────────────
    public decimal FactorConversion { get; set; } = 1m; // NUMERIC(18,8) DEFAULT 1
    public string UnidadBase { get; set; } = string.Empty; // VARCHAR(20) NOT NULL — kg, m3, m, C

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}