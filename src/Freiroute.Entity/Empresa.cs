namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa a una empresa (tenant) del SaaS Freiroute TMS.
/// Corresponde a la tabla raíz 'empresas' — NO tiene empresa_id propio ni RLS.
/// Es gestionada únicamente por el SUPER_ADMIN.
/// </summary>
public class Empresa
{
    // ── Identity / Lifecycle ──────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Datos de identificación del tenant ────────────────────────
    public string Nombre { get; set; } = string.Empty;                 // VARCHAR(200) NOT NULL
    public string? RucNit { get; set; }                                // VARCHAR(50)
    public string EmailAdmin { get; set; } = string.Empty;             // VARCHAR(200) NOT NULL UNIQUE
    public string? Telefono { get; set; }                              // VARCHAR(50)
    public string Pais { get; set; } = "Nicaragua";                    // VARCHAR(100) DEFAULT 'Nicaragua'
    public string? Ciudad { get; set; }                                // VARCHAR(100)
    public string? Direccion { get; set; }                             // TEXT
    public string? LogoUrl { get; set; }                               // TEXT (Supabase Storage)

    // ── Personalización white-label ────────────────────────────────
    public string ColorPrimario { get; set; } = "#1A73E8";             // VARCHAR(7) DEFAULT
    public string ColorSecundario { get; set; } = "#0B2545";           // VARCHAR(7) DEFAULT

    // ── Suscripción y estado del tenant ────────────────────────────
    public string PlanSuscripcion { get; set; } = "STARTER";           // STARTER | PROFESSIONAL | ENTERPRISE
    public string Estado { get; set; } = "ACTIVE";                     // ACTIVE | SUSPENDED | CANCELLED

    // ── Configuración operativa ────────────────────────────────────
    public string MonedaPrincipal { get; set; } = "USD";               // VARCHAR(10) DEFAULT 'USD'
    public string ZonaHoraria { get; set; } = "America/Managua";       // VARCHAR(100) DEFAULT
    public string Idioma { get; set; } = "es";                         // VARCHAR(10) DEFAULT 'es'
    public string FormatoFecha { get; set; } = "DD/MM/YYYY";           // VARCHAR(20) DEFAULT

    // ── Numeración de documentos ───────────────────────────────────
    public string PrefijoEmbarque { get; set; } = "FR";                // VARCHAR(10) DEFAULT 'FR'
    public int ConsecutivoEmbarque { get; set; } = 1;                  // INTEGER DEFAULT 1
    public string PrefijoOrden { get; set; } = "ORD";                  // VARCHAR(10) DEFAULT 'ORD'
    public int ConsecutivoOrden { get; set; } = 1;                     // INTEGER DEFAULT 1
}