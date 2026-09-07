namespace Freiroute.Entity;

/// <summary>
/// Catálogo de tipos de mercancía del tenant. Define cómo se maneja
/// y clasifica la carga para la planificación de embarques.
/// Corresponde a la tabla 'tipos_mercancia' (HU-017).
/// </summary>
public class TipoMercancia
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string? Codigo { get; set; }                 // VARCHAR(50) — UNIQUE (empresa_id, codigo)
    public string? Descripcion { get; set; }            // TEXT
    public string? Categoria { get; set; }              // VARCHAR(100)

    // ── Clasificación HAZMAT (normativa ONU) ──────────────────
    public string? ClasePeligrosidad { get; set; }      // VARCHAR(10) — 1..9 con subclases (1.1, 2.1...)
    public string? CodigoOnu { get; set; }              // VARCHAR(10)
    public string? CodigoHs { get; set; }               // VARCHAR(20) — Sistema Armonizado (aduana)

    // ── Características físicas ───────────────────────────────
    public decimal? PesoMaximoKg { get; set; }          // NUMERIC(10,3)
    public decimal? VolumenMaximoM3 { get; set; }       // NUMERIC(10,3)
    public decimal? TemperaturaMinC { get; set; }       // NUMERIC(5,2)
    public decimal? TemperaturaMaxC { get; set; }       // NUMERIC(5,2)

    // ── Flags de manejo ───────────────────────────────────────
    public bool RequiereRefrigeracion { get; set; } = false; // BOOLEAN DEFAULT false
    public bool EsFragil { get; set; } = false;             // BOOLEAN DEFAULT false
    public bool EsPeligroso { get; set; } = false;          // BOOLEAN DEFAULT false
    public bool EsPerecedero { get; set; } = false;         // BOOLEAN DEFAULT false
    public bool EsSobredimensionado { get; set; } = false;  // BOOLEAN DEFAULT false
    public bool RequiereFumigacion { get; set; } = false;   // BOOLEAN DEFAULT false

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}