using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Recargo aplicado dentro del resultado de una simulación de costo (ADR-015).
/// </summary>
[SwaggerSchema(Description = "Recargo aplicado con su monto calculado")]
public class RecargoAplicadoDto
{
    [SwaggerSchema(Description = "Nombre del recargo")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de cálculo: PORCENTAJE o MONTO_FIJO")]
    public string TipoCalculo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Valor configurado: % o monto")]
    public decimal Valor { get; set; }

    [SwaggerSchema(Description = "Monto calculado en la moneda de la tarifa")]
    public decimal Monto { get; set; }
}