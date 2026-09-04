namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un plan de suscripción del SaaS Freiroute TMS.
/// Corresponde a la tabla 'planes'. Es un catálogo GLOBAL — NO tiene empresa_id.
/// Gestionado exclusivamente por el SUPER_ADMIN (ADR-004).
/// </summary>
public class Plan
{
    // ── Identity / Lifecycle ──────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Datos del plan ────────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;                 // VARCHAR(100) NOT NULL
    public string Codigo { get; set; } = string.Empty;                 // VARCHAR(50) NOT NULL UNIQUE
    public string? Descripcion { get; set; }                            // TEXT

    // ── Límites operativos ────────────────────────────────────────
    public int LimiteUsuarios { get; set; } = 5;                      // INTEGER -1 = ilimitado
    public int LimiteEmbarquesMes { get; set; } = 500;                 // INTEGER -1 = ilimitado
    public int LimiteStorageGb { get; set; } = 1;                      // INTEGER NOT NULL

    // ── Precio ────────────────────────────────────────────────────
    public decimal PrecioMensual { get; set; }                         // NUMERIC(10,2)
    public decimal PrecioAnual { get; set; }                           // NUMERIC(10,2)
    public string Moneda { get; set; } = "USD";                        // VARCHAR(10) NOT NULL

    // ── Módulos disponibles ───────────────────────────────────────
    public string[] ModulosDisponibles { get; set; } = [];             // TEXT[] — mapeo de PostgreSQL TEXT[]

    // ── Control de visibilidad ────────────────────────────────────
    public bool EsPublico { get; set; } = true;                        // BOOLEAN — visible en el portal de signup
}
