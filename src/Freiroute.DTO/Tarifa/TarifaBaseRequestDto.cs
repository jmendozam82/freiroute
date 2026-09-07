using Freiroute.Utility.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Datos de entrada para crear o actualizar una tarifa base (HU-020, ADR-015).
/// Al actualizar una tarifa existente, la BLL cierra la vigencia de la
/// anterior y crea una nueva versión (no modifica el historial).
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una tarifa base")]
public class TarifaBaseRequestDto
{
    [SwaggerSchema(Description = "Nombre de la tarifa", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Zona de origen (de HU-016)")]
    public Guid? ZonaOrigenId { get; set; }

    [SwaggerSchema(Description = "Zona de destino (de HU-016)")]
    public Guid? ZonaDestinoId { get; set; }

    [SwaggerSchema(Description = "Modo de transporte: FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL")]
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Ftl;

    [SwaggerSchema(Description = "Tipo de servicio: ESTANDAR, EXPRESS, PROGRAMADO, REFRIGERADO, PELIGROSO")]
    public string TipoServicio { get; set; } = TipoServicioTransporte.Estandar;

    [SwaggerSchema(Description = "Tipo de tarifa: POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD")]
    public string TipoTarifa { get; set; } = Freiroute.Utility.Constants.TipoTarifa.FijoViaje;

    [SwaggerSchema(Description = "Precio unitario según tipo_tarifa", Nullable = false)]
    public decimal PrecioUnitario { get; set; }

    [SwaggerSchema(Description = "Precio mínimo aplicable — null si no hay mínimo")]
    public decimal? PrecioMinimo { get; set; }

    [SwaggerSchema(Description = "Moneda (ISO 4217)")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Inicio de vigencia (requerido)", Nullable = false)]
    public DateOnly FechaVigenciaDesde { get; set; }

    [SwaggerSchema(Description = "Fin de vigencia — null = vigente hasta nuevo aviso")]
    public DateOnly? FechaVigenciaHasta { get; set; }

    [SwaggerSchema(Description = "Recargos creados junto con la tarifa")]
    public List<RecargoTarifaRequestDto> Recargos { get; set; } = [];
}