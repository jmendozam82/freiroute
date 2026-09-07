using Freiroute.Utility.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Datos de entrada de un recargo de tarifa (HU-020, ADR-015).
/// </summary>
[SwaggerSchema(Description = "Datos de un recargo de tarifa")]
public class RecargoTarifaRequestDto
{
    [SwaggerSchema(Description = "Código: COMBUSTIBLE, PEAJE, SEGURO, MANIPULACION, URGENCIA, REFRIGERACION, SOBREDIMENSION", Nullable = false)]
    public string CodigoRecargo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre legible del recargo", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de cálculo: PORCENTAJE (% sobre tarifa base) o MONTO_FIJO")]
    public string TipoCalculo { get; set; } = TipoCalculoRecargo.Porcentaje;

    [SwaggerSchema(Description = "Valor: % (PORCENTAJE) o monto absoluto (MONTO_FIJO)", Nullable = false)]
    public decimal Valor { get; set; }
}