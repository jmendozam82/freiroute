using Freiroute.Utility.Constants;

namespace Freiroute.Entity;

/// <summary>
/// Ubicación georreferenciada del tenant: almacenes, clientes,
/// puertos, aeropuertos y puntos de entrega/recogida.
/// Corresponde a la tabla 'ubicaciones' (HU-015, ADR-014).
/// </summary>
public class Ubicacion
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string? Codigo { get; set; }                 // VARCHAR(50) — UNIQUE (empresa_id, codigo)
    public string Tipo { get; set; } = TipoUbicacion.Otro;   // VARCHAR(50) DEFAULT 'OTRO'

    // ── Dirección ─────────────────────────────────────────────
    public string? Direccion { get; set; }              // TEXT
    public string Pais { get; set; } = "Nicaragua";     // VARCHAR(100) DEFAULT 'Nicaragua'
    public string? Departamento { get; set; }           // VARCHAR(100)
    public string? Ciudad { get; set; }                 // VARCHAR(100)
    public string? CodigoPostal { get; set; }           // VARCHAR(20)

    // ── Geocodificación (ADR-014) ─────────────────────────────
    public double? Latitud { get; set; }                // DOUBLE PRECISION
    public double? Longitud { get; set; }               // DOUBLE PRECISION
    public bool Georeferenciada { get; set; } = false;  // BOOLEAN DEFAULT false
    public string? DireccionNormalizada { get; set; }   // TEXT

    // ── Datos operativos ──────────────────────────────────────
    public string? ContactoNombre { get; set; }         // VARCHAR(200)
    public string? ContactoTelefono { get; set; }       // VARCHAR(50)
    public string? ContactoEmail { get; set; }          // VARCHAR(200)
    public TimeOnly? HorarioApertura { get; set; }      // TIME
    public TimeOnly? HorarioCierre { get; set; }        // TIME
    public int TiempoServicioMin { get; set; } = 30;    // INTEGER DEFAULT 30 — carga/descarga en minutos
    public string? Instrucciones { get; set; }          // TEXT

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}