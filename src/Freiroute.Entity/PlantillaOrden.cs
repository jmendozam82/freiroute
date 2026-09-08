namespace Freiroute.Entity;

/// <summary>
/// Plantilla para creación rápida de órdenes y para órdenes recurrentes
/// programadas (HU-027). El campo datos_orden almacena un snapshot JSON
/// de los campos de OrdenRequestDto al momento de guardar.
/// El background job RecurrenciaOrdenesJob procesa las plantillas recurrentes
/// cada día a las 00:05 (patrón ADR-013).
/// Corresponde a la tabla 'plantillas_orden'.
/// </summary>
public class PlantillaOrden
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL
    public string? Descripcion { get; set; }            // TEXT

    // ── Snapshot de la orden ──────────────────────────────────
    public string DatosOrden { get; set; } = "{}";      // JSONB NOT NULL DEFAULT '{}'

    // ── Recurrencia ───────────────────────────────────────────
    public bool EsRecurrente { get; set; } = false;     // BOOLEAN DEFAULT false
    public string? FrecuenciaRecurrencia { get; set; }  // VARCHAR(20) — DIARIA|SEMANAL|QUINCENAL|MENSUAL
    public DateOnly? ProximaEjecucion { get; set; }     // DATE — nullable

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
    public Guid? CreadoPor { get; set; }                // FK usuarios(id)
}
