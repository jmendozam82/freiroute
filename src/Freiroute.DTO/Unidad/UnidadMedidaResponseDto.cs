using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Unidad;

/// <summary>
/// Datos de salida de una unidad de medida (HU-018).
/// TipoLabel se calcula en la BLL.
/// </summary>
[SwaggerSchema(Description = "Respuesta de una unidad de medida")]
public class UnidadMedidaResponseDto
{
    [SwaggerSchema(Description = "ID único de la unidad")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre legible (ej: Kilogramo)")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Símbolo único por empresa (ej: kg)")]
    public string Simbolo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo: PESO, VOLUMEN, LONGITUD, TEMPERATURA")]
    public string Tipo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del tipo (calculado)")]
    public string TipoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Factor de conversión a la unidad base")]
    public decimal FactorConversion { get; set; }

    [SwaggerSchema(Description = "Unidad base del tipo: kg, m3, m, C")]
    public string UnidadBase { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Si la unidad está activa (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}