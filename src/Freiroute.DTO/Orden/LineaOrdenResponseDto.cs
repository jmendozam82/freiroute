using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Respuesta de una línea de detalle de mercancía de una orden.
/// </summary>
[SwaggerSchema(Description = "Respuesta de una línea de detalle de mercancía")]
public class LineaOrdenResponseDto
{
    [SwaggerSchema(Description = "ID único de la línea")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Descripción de la mercancía")]
    public string Descripcion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Cantidad de unidades")]
    public decimal Cantidad { get; set; }

    [SwaggerSchema(Description = "ID de la unidad de medida")]
    public Guid? UnidadMedidaId { get; set; }

    [SwaggerSchema(Description = "Símbolo de la unidad de medida (calculado)")]
    public string? UnidadMedidaSimbolo { get; set; }

    [SwaggerSchema(Description = "Peso por unidad en kilogramos")]
    public decimal? PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen por unidad en metros cúbicos")]
    public decimal? VolumenM3 { get; set; }

    [SwaggerSchema(Description = "Valor unitario de la mercancía")]
    public decimal? ValorUnitario { get; set; }

    [SwaggerSchema(Description = "Número de línea para ordenamiento")]
    public short NumeroLinea { get; set; }
}
