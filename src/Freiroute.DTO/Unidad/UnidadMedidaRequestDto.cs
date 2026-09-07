using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Unidad;

/// <summary>
/// Datos de entrada para crear o actualizar una unidad de medida (HU-018).
/// FactorConversion convierte a la unidad base del tipo (kg, m3, m, C).
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una unidad de medida")]
public class UnidadMedidaRequestDto
{
    [SwaggerSchema(Description = "Nombre legible (ej: Kilogramo)", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Símbolo único por empresa (ej: kg)", Nullable = false)]
    public string Simbolo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo: PESO, VOLUMEN, LONGITUD, TEMPERATURA", Nullable = false)]
    public string Tipo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Factor de conversión a la unidad base (ej: 1 lb = 0.453592 kg)")]
    public decimal FactorConversion { get; set; } = 1m;

    [SwaggerSchema(Description = "Unidad base del tipo: kg, m3, m, C", Nullable = false)]
    public string UnidadBase { get; set; } = string.Empty;
}