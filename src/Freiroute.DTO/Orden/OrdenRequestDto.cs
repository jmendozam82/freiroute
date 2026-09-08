using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos de entrada para crear o actualizar una orden de transporte (HU-021).
/// El empresa_id se extrae del JWT — nunca se acepta en el body (ADR-003).
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una orden de transporte")]
public class OrdenRequestDto
{
    // ── Relaciones con maestros ───────────────────────────────
    [SwaggerSchema(Description = "ID del cliente (shipper) que solicita el transporte")]
    public Guid ClienteId { get; set; }

    [SwaggerSchema(Description = "ID de la ubicación de origen")]
    public Guid OrigenId { get; set; }

    [SwaggerSchema(Description = "ID de la ubicación de destino")]
    public Guid DestinoId { get; set; }

    [SwaggerSchema(Description = "ID del tipo de mercancía")]
    public Guid TipoMercanciaId { get; set; }

    [SwaggerSchema(Description = "ID de la unidad de medida")]
    public Guid UnidadMedidaId { get; set; }

    [SwaggerSchema(Description = "ID del tipo de embalaje (opcional)")]
    public Guid? TipoEmbalajeId { get; set; }

    [SwaggerSchema(Description = "ID de la tarifa base aplicable (opcional)")]
    public Guid? TarifaId { get; set; }

    // ── Datos de carga ────────────────────────────────────────
    [SwaggerSchema(Description = "Cantidad de unidades (debe ser mayor a cero)")]
    public decimal Cantidad { get; set; }

    [SwaggerSchema(Description = "Peso total en kilogramos (debe ser mayor a cero)")]
    public decimal PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen total en metros cúbicos")]
    public decimal? VolumenM3 { get; set; }

    [SwaggerSchema(Description = "Valor declarado de la mercancía")]
    public decimal? ValorDeclarado { get; set; }

    // ── Datos de servicio ─────────────────────────────────────
    [SwaggerSchema(Description = "Modo de transporte: TERRESTRE, FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL")]
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Terrestre;

    [SwaggerSchema(Description = "Nivel de servicio: ESTANDAR, EXPRESS, PROGRAMADO")]
    public string NivelServicio { get; set; } = Freiroute.Utility.Constants.NivelServicio.Estandar;

    [SwaggerSchema(Description = "Prioridad: CRITICO, ALTO, NORMAL, BAJO")]
    public string Prioridad { get; set; } = Freiroute.Utility.Constants.OrdenPrioridad.Normal;

    // ── Fechas operativas ─────────────────────────────────────
    [SwaggerSchema(Description = "Fecha de recogida solicitada (formato YYYY-MM-DD)")]
    public DateOnly? FechaPickupSolicitada { get; set; }

    [SwaggerSchema(Description = "Fecha de entrega requerida (formato YYYY-MM-DD)")]
    public DateOnly? FechaEntregaRequerida { get; set; }

    // ── Referencia y comunicación ─────────────────────────────
    [SwaggerSchema(Description = "Referencia del cliente para esta orden")]
    public string? ReferenciaCliente { get; set; }

    [SwaggerSchema(Description = "Instrucciones especiales de manejo")]
    public string? Instrucciones { get; set; }

    // ── Líneas de detalle ─────────────────────────────────────
    [SwaggerSchema(Description = "Líneas de detalle de mercancía")]
    public List<LineaOrdenRequestDto> Lineas { get; set; } = [];
}
