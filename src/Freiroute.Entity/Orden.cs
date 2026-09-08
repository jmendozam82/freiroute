namespace Freiroute.Entity;

/// <summary>
/// Orden de transporte — entidad central de EP-04 Order Management.
/// Nace en DRAFT y cierra como CLOSED siguiendo la FSM del ADR-019.
/// Cada orden está aislada por empresa_id (multi-tenant, ADR-003).
/// </summary>
public class Orden
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Número legible (ADR-020) ─────────────────────────────
    public string? NumeroOrden { get; set; }            // VARCHAR(30) — NULL hasta DRAFT→CONFIRMED

    // ── Relaciones con maestros (Sprint 3) ───────────────────
    public Guid ClienteId { get; set; }                 // FK clientes(id) NOT NULL
    public Guid OrigenId { get; set; }                  // FK ubicaciones(id) NOT NULL
    public Guid DestinoId { get; set; }                 // FK ubicaciones(id) NOT NULL
    public Guid TipoMercanciaId { get; set; }           // FK tipos_mercancia(id) NOT NULL
    public Guid UnidadMedidaId { get; set; }            // FK unidades_medida(id) NOT NULL
    public Guid? TipoEmbalajeId { get; set; }           // FK tipos_embalaje(id) — nullable
    public Guid? TarifaId { get; set; }                 // FK tarifas_base(id) — nullable

    // ── Relación con shipment ────────────────────────────────
    public Guid? ShipmentId { get; set; }               // FK shipments(id) — se activa en HU-025

    // ── Datos de carga ───────────────────────────────────────
    public decimal Cantidad { get; set; }               // NUMERIC(12,3) NOT NULL
    public decimal PesoKg { get; set; }                 // NUMERIC(12,3) NOT NULL
    public decimal? VolumenM3 { get; set; }             // NUMERIC(12,3)
    public decimal? ValorDeclarado { get; set; }        // NUMERIC(15,2)

    // ── Datos de servicio ────────────────────────────────────
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Terrestre;  // VARCHAR(20) DEFAULT 'TERRESTRE'
    public string NivelServicio { get; set; } = Freiroute.Utility.Constants.NivelServicio.Estandar;    // VARCHAR(20) DEFAULT 'ESTANDAR'
    public string Prioridad { get; set; } = Freiroute.Utility.Constants.OrdenPrioridad.Normal;          // VARCHAR(20) DEFAULT 'NORMAL'

    // ── Fechas operativas ────────────────────────────────────
    public DateOnly? FechaPickupSolicitada { get; set; }    // DATE
    public DateOnly? FechaEntregaRequerida { get; set; }    // DATE
    public DateTime? FechaConfirmacion { get; set; }        // TIMESTAMPTZ

    // ── Referencia y comunicación ────────────────────────────
    public string? ReferenciaCliente { get; set; }      // VARCHAR(100)
    public string? Instrucciones { get; set; }          // TEXT

    // ── Estado FSM (ADR-019) ─────────────────────────────────
    public string Estado { get; set; } = Freiroute.Utility.Constants.OrdenEstado.Draft; // VARCHAR(30) DEFAULT 'DRAFT'

    // ── Split (HU-026) ───────────────────────────────────────
    public bool EsSplit { get; set; } = false;          // BOOLEAN DEFAULT false
    public Guid? OrdenOrigenId { get; set; }            // FK ordenes(id) — referencia a orden padre

    // ── Canal de ingreso (HU-022 / HU-023 / HU-027) ─────────
    public string OrigenCreacion { get; set; } = Freiroute.Utility.Constants.OrigenCreacion.Manual; // VARCHAR(20) DEFAULT 'MANUAL'
    public Guid? ApiKeyId { get; set; }                 // FK api_keys_tenant(id) — nullable

    // ── Auditoría estándar ───────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
    public Guid? CreadoPor { get; set; }                // FK usuarios(id)
    public Guid? ModificadoPor { get; set; }            // FK usuarios(id)
}
