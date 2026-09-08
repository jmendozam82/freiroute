namespace Freiroute.Entity;

/// <summary>
/// Línea de detalle de mercancía por orden de transporte (HU-021).
/// Cada orden puede tener múltiples ítems con diferentes descripciones,
/// cantidades y pesos. El ON DELETE CASCADE permite limpiar las líneas
/// al desactivar la orden.
/// Corresponde a la tabla 'lineas_orden'.
/// </summary>
public class LineaOrden
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid OrdenId { get; set; }                   // FK ordenes(id) ON DELETE CASCADE

    // ── Datos de la línea ────────────────────────────────────
    public string Descripcion { get; set; } = string.Empty; // VARCHAR(255) NOT NULL
    public decimal Cantidad { get; set; }               // NUMERIC(12,3) NOT NULL
    public Guid? UnidadMedidaId { get; set; }           // FK unidades_medida(id) — nullable
    public decimal? PesoKg { get; set; }                // NUMERIC(12,3)
    public decimal? VolumenM3 { get; set; }             // NUMERIC(12,3)
    public decimal? ValorUnitario { get; set; }         // NUMERIC(15,2)
    public short NumeroLinea { get; set; } = 1;         // SMALLINT DEFAULT 1

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}
