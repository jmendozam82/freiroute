namespace Freiroute.Entity;

/// <summary>
/// Shipper / cliente del tenant que contrata servicios de transporte.
/// Contiene la configuración comercial, de crédito y SLA por cliente.
/// Corresponde a la tabla 'clientes' (HU-019).
/// </summary>
public class Cliente
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string? NombreComercial { get; set; }        // VARCHAR(200)
    public string? RucNit { get; set; }                 // VARCHAR(50) — UNIQUE por empresa
    public string TipoDocumento { get; set; } = "RUC";  // VARCHAR(20) DEFAULT 'RUC'
    public string TipoCliente { get; set; } = Freiroute.Utility.Constants.TipoCliente.Regular; // VARCHAR(50) DEFAULT 'REGULAR'
    public string? Industria { get; set; }              // VARCHAR(100)

    // ── Contacto principal ────────────────────────────────────
    public string? Email { get; set; }                  // VARCHAR(200)
    public string? Telefono { get; set; }               // VARCHAR(50)
    public string? SitioWeb { get; set; }               // VARCHAR(300)

    // ── Dirección fiscal ──────────────────────────────────────
    public string? DireccionFiscal { get; set; }        // TEXT
    public string Pais { get; set; } = "Nicaragua";     // VARCHAR(100) DEFAULT 'Nicaragua'
    public string? Departamento { get; set; }           // VARCHAR(100)
    public string? Ciudad { get; set; }                 // VARCHAR(100)

    // ── Ubicación de despacho por defecto ─────────────────────
    public Guid? UbicacionDefectoId { get; set; }       // FK ubicaciones(id) ON DELETE SET NULL

    // ── Configuración comercial ───────────────────────────────
    public int CreditoDias { get; set; } = 0;           // INTEGER DEFAULT 0 — 0 = contado
    public decimal LimiteCredito { get; set; } = 0m;    // NUMERIC(18,2) DEFAULT 0
    public string Moneda { get; set; } = "USD";         // VARCHAR(10) DEFAULT 'USD'
    public string EstadoCredito { get; set; } = Freiroute.Utility.Constants.EstadoCredito.AlDia; // VARCHAR(50) DEFAULT 'AL_DIA'

    // ── SLA ───────────────────────────────────────────────────
    public int? SlaDiasEntrega { get; set; }            // INTEGER — NULL si no hay SLA formal
    public TimeOnly? SlaVentanaInicio { get; set; }     // TIME
    public TimeOnly? SlaVentanaFin { get; set; }        // TIME

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}