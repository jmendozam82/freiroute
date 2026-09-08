using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Respuesta completa de una orden con todas sus relaciones y
/// transiciones disponibles (HU-021, HU-024). Los labels se calculan
/// en la BLL desde OrdenEstado, NivelServicio, etc.
/// </summary>
[SwaggerSchema(Description = "Respuesta completa de una orden de transporte")]
public class OrdenResponseDto
{
    // ── Identificación ────────────────────────────────────────
    [SwaggerSchema(Description = "ID único de la orden")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Número legible de la orden (NULL en DRAFT)")]
    public string? NumeroOrden { get; set; }

    // ── Relaciones con maestros ───────────────────────────────
    [SwaggerSchema(Description = "ID del cliente")]
    public Guid ClienteId { get; set; }

    [SwaggerSchema(Description = "Nombre del cliente (calculado)")]
    public string ClienteNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID de la ubicación de origen")]
    public Guid OrigenId { get; set; }

    [SwaggerSchema(Description = "Nombre de la ubicación de origen (calculado)")]
    public string OrigenNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID de la ubicación de destino")]
    public Guid DestinoId { get; set; }

    [SwaggerSchema(Description = "Nombre de la ubicación de destino (calculado)")]
    public string DestinoNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID del tipo de mercancía")]
    public Guid TipoMercanciaId { get; set; }

    [SwaggerSchema(Description = "Nombre del tipo de mercancía (calculado)")]
    public string TipoMercanciaNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID de la unidad de medida")]
    public Guid UnidadMedidaId { get; set; }

    [SwaggerSchema(Description = "Símbolo de la unidad de medida (calculado)")]
    public string UnidadMedidaSimbolo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID del tipo de embalaje")]
    public Guid? TipoEmbalajeId { get; set; }

    [SwaggerSchema(Description = "Nombre del tipo de embalaje (calculado)")]
    public string? TipoEmbalajeNombre { get; set; }

    [SwaggerSchema(Description = "ID de la tarifa base")]
    public Guid? TarifaId { get; set; }

    [SwaggerSchema(Description = "Nombre de la tarifa base (calculado)")]
    public string? TarifaNombre { get; set; }

    // ── Relación con shipment ─────────────────────────────────
    [SwaggerSchema(Description = "ID del shipment asignado")]
    public Guid? ShipmentId { get; set; }

    [SwaggerSchema(Description = "Número del shipment (calculado)")]
    public string? ShipmentNumero { get; set; }

    // ── Datos de carga ────────────────────────────────────────
    [SwaggerSchema(Description = "Cantidad total de unidades")]
    public decimal Cantidad { get; set; }

    [SwaggerSchema(Description = "Peso total en kilogramos")]
    public decimal PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen total en metros cúbicos")]
    public decimal? VolumenM3 { get; set; }

    [SwaggerSchema(Description = "Valor declarado de la mercancía")]
    public decimal? ValorDeclarado { get; set; }

    // ── Datos de servicio ─────────────────────────────────────
    [SwaggerSchema(Description = "Modo de transporte: TERRESTRE, FTL, LTL, etc.")]
    public string ModoTransporte { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del modo de transporte")]
    public string ModoTransporteLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nivel de servicio: ESTANDAR, EXPRESS, PROGRAMADO")]
    public string NivelServicio { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del nivel de servicio")]
    public string NivelServicioLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Prioridad: CRITICO, ALTO, NORMAL, BAJO")]
    public string Prioridad { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible de la prioridad")]
    public string PrioridadLabel { get; set; } = string.Empty;

    // ── Fechas operativas ─────────────────────────────────────
    [SwaggerSchema(Description = "Fecha de recogida solicitada")]
    public DateOnly? FechaPickupSolicitada { get; set; }

    [SwaggerSchema(Description = "Fecha de entrega requerida")]
    public DateOnly? FechaEntregaRequerida { get; set; }

    [SwaggerSchema(Description = "Fecha de confirmación de la orden")]
    public DateTime? FechaConfirmacion { get; set; }

    // ── Referencia y comunicación ─────────────────────────────
    [SwaggerSchema(Description = "Referencia del cliente")]
    public string? ReferenciaCliente { get; set; }

    [SwaggerSchema(Description = "Instrucciones especiales de manejo")]
    public string? Instrucciones { get; set; }

    // ── Estado FSM ────────────────────────────────────────────
    [SwaggerSchema(Description = "Estado actual de la orden (FSM)")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado")]
    public string EstadoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Colores del badge de estado (CSS class)")]
    public string EstadoBadgeClass { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Estados a los que puede transicionar desde el actual")]
    public List<string> TransicionesDisponibles { get; set; } = [];

    // ── Split ─────────────────────────────────────────────────
    [SwaggerSchema(Description = "true si esta orden es resultado de dividir una orden padre")]
    public bool EsSplit { get; set; }

    [SwaggerSchema(Description = "ID de la orden padre (cuando es_split = true)")]
    public Guid? OrdenOrigenId { get; set; }

    [SwaggerSchema(Description = "Lista de sub-órdenes generadas por split")]
    public List<OrdenListDto> SubOrdenes { get; set; } = [];

    // ── Canal de ingreso ──────────────────────────────────────
    [SwaggerSchema(Description = "Canal de creación: MANUAL, CSV, API, RECURRENTE")]
    public string OrigenCreacion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del origen de creación")]
    public string OrigenCreacionLabel { get; set; } = string.Empty;

    // ── Auditoría ─────────────────────────────────────────────
    [SwaggerSchema(Description = "Si la orden está activa")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }

    [SwaggerSchema(Description = "Última fecha de modificación")]
    public DateTime? FechaModificacion { get; set; }

    // ── Líneas de detalle ─────────────────────────────────────
    [SwaggerSchema(Description = "Líneas de detalle de mercancía")]
    public List<LineaOrdenResponseDto> Lineas { get; set; } = [];
}
