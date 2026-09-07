namespace Freiroute.Entity;

/// <summary>
/// Tarifa de flete por zona, modo y tipo de servicio del tenant.
/// Historial completo preservado — no se eliminan registros vencidos.
/// Corresponde a la tabla 'tarifas_base' (HU-020, ADR-015).
/// Las tarifas se versionan: al actualizar se cierra la vigencia de la
/// anterior (CerrarVigenciaAsync) y se crea una nueva versión. NO existe
/// UpdateAsync de la tarifa en el repositorio.
/// </summary>
public class TarifaBase
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string? Codigo { get; set; }                 // VARCHAR(50) — UNIQUE (empresa_id, codigo)

    // ── Aplicación de la tarifa ───────────────────────────────
    public Guid? ZonaOrigenId { get; set; }             // FK zonas_entrega(id) ON DELETE RESTRICT
    public Guid? ZonaDestinoId { get; set; }            // FK zonas_entrega(id) ON DELETE RESTRICT
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Ftl; // VARCHAR(50) DEFAULT 'FTL'
    public string TipoServicio { get; set; } = "ESTANDAR"; // VARCHAR(50) DEFAULT 'ESTANDAR'

    // ── Modelo de precio (ADR-015) ────────────────────────────
    public string TipoTarifa { get; set; } = Freiroute.Utility.Constants.TipoTarifa.FijoViaje; // VARCHAR(30) DEFAULT 'FIJO_VIAJE'
    public decimal PrecioUnitario { get; set; }         // NUMERIC(18,4) NOT NULL
    public decimal? PrecioMinimo { get; set; }          // NUMERIC(18,4) — NULL si no hay mínimo
    public string Moneda { get; set; } = "USD";         // VARCHAR(10) DEFAULT 'USD'

    // ── Vigencia ──────────────────────────────────────────────
    public DateOnly FechaVigenciaDesde { get; set; }    // DATE NOT NULL DEFAULT CURRENT_DATE
    public DateOnly? FechaVigenciaHasta { get; set; }   // DATE — NULL = vigente hasta nuevo aviso

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}