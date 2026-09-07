using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Datos de salida de una tarifa base con sus recargos (HU-020, ADR-015).
/// EsVigente y EsVencida se calculan en la BLL contra la fecha actual.
/// </summary>
[SwaggerSchema(Description = "Respuesta de una tarifa base con sus recargos")]
public class TarifaBaseResponseDto
{
    [SwaggerSchema(Description = "ID único de la tarifa")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la tarifa")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Zona de origen")]
    public Guid? ZonaOrigenId { get; set; }

    [SwaggerSchema(Description = "Nombre de la zona de origen (calculado)")]
    public string? ZonaOrigenNombre { get; set; }

    [SwaggerSchema(Description = "Zona de destino")]
    public Guid? ZonaDestinoId { get; set; }

    [SwaggerSchema(Description = "Nombre de la zona de destino (calculado)")]
    public string? ZonaDestinoNombre { get; set; }

    [SwaggerSchema(Description = "Modo de transporte")]
    public string ModoTransporte { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de servicio")]
    public string TipoServicio { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de tarifa: POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD")]
    public string TipoTarifa { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del tipo de tarifa (calculado)")]
    public string TipoTarifaLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Precio unitario")]
    public decimal PrecioUnitario { get; set; }

    [SwaggerSchema(Description = "Precio mínimo aplicable")]
    public decimal? PrecioMinimo { get; set; }

    [SwaggerSchema(Description = "Moneda")]
    public string Moneda { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Inicio de vigencia")]
    public DateOnly FechaVigenciaDesde { get; set; }

    [SwaggerSchema(Description = "Fin de vigencia — null = vigente hasta nuevo aviso")]
    public DateOnly? FechaVigenciaHasta { get; set; }

    [SwaggerSchema(Description = "true si la tarifa está vigente hoy (calculado)")]
    public bool EsVigente { get; set; }

    [SwaggerSchema(Description = "true si fecha_vigencia_hasta está en el pasado (calculado)")]
    public bool EsVencida { get; set; }

    [SwaggerSchema(Description = "Recargos de la tarifa")]
    public List<RecargoTarifaResponseDto> Recargos { get; set; } = [];

    [SwaggerSchema(Description = "Si la tarifa está activa (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}