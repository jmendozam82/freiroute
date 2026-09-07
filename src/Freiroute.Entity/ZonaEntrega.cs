namespace Freiroute.Entity;

/// <summary>
/// Zona geográfica de cobertura del tenant. Usada en tarifas (HU-020),
/// asignación de carriers y planificación de rutas.
/// Corresponde a la tabla 'zonas_entrega' (HU-016).
/// </summary>
public class ZonaEntrega
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string Codigo { get; set; } = string.Empty;  // VARCHAR(50) NOT NULL — UNIQUE (empresa_id, codigo)
    public string? Descripcion { get; set; }            // TEXT
    public string ColorHex { get; set; } = "#1A73E8";   // VARCHAR(7) DEFAULT '#1A73E8' — color en mapa

    // ── Definición geográfica ─────────────────────────────────
    public string TipoDefinicion { get; set; } = "POLIGONO"; // VARCHAR(20) DEFAULT 'POLIGONO'
    public string? PoligonoGeoJson { get; set; }        // TEXT — GeoJSON Polygon/MultiPolygon
    public string[] CodigosPostales { get; set; } = []; // TEXT[]
    public string[] Ciudades { get; set; } = [];        // TEXT[]
    public string[] Departamentos { get; set; } = [];   // TEXT[]
    public string[] Paises { get; set; } = ["Nicaragua"]; // TEXT[] DEFAULT '{Nicaragua}'

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}