using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para dividir una orden en múltiples partes (HU-026).
/// Solo permitido en estados CONFIRMED o ASSIGNED. La suma de cantidad
/// y peso de los splits debe igualar la orden original.
/// </summary>
[SwaggerSchema(Description = "Datos para dividir una orden en sub-órdenes")]
public class SplitOrdenRequestDto
{
    [SwaggerSchema(Description = "Lista de sub-órdenes resultantes (mínimo 2, máximo 10)")]
    public List<SplitItemDto> Splits { get; set; } = [];
}

/// <summary>
/// Elemento individual de un split de orden (HU-026).
/// </summary>
[SwaggerSchema(Description = "Sub-orden resultante de un split")]
public class SplitItemDto
{
    [SwaggerSchema(Description = "Cantidad para esta sub-orden")]
    public decimal Cantidad { get; set; }

    [SwaggerSchema(Description = "Peso en kilogramos para esta sub-orden")]
    public decimal PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen en metros cúbicos (opcional)")]
    public decimal? VolumenM3 { get; set; }

    [SwaggerSchema(Description = "ID del destino para esta sub-orden (puede diferir del original)")]
    public Guid? DestinoId { get; set; }

    [SwaggerSchema(Description = "Instrucciones específicas para esta sub-orden")]
    public string? Instrucciones { get; set; }
}
