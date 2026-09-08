using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos de entrada para una línea de detalle de mercancía dentro de una
/// orden de transporte (HU-021).
/// </summary>
[SwaggerSchema(Description = "Línea de detalle de mercancía de una orden")]
public class LineaOrdenRequestDto
{
    [SwaggerSchema(Description = "Descripción de la mercancía (obligatorio)")]
    public string Descripcion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Cantidad de unidades (debe ser mayor a cero)")]
    public decimal Cantidad { get; set; }

    [SwaggerSchema(Description = "ID de la unidad de medida")]
    public Guid? UnidadMedidaId { get; set; }

    [SwaggerSchema(Description = "Peso por unidad en kilogramos")]
    public decimal? PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen por unidad en metros cúbicos")]
    public decimal? VolumenM3 { get; set; }

    [SwaggerSchema(Description = "Valor unitario de la mercancía")]
    public decimal? ValorUnitario { get; set; }

    [SwaggerSchema(Description = "Número de línea para ordenamiento (default: 1)")]
    public short NumeroLinea { get; set; } = 1;
}
