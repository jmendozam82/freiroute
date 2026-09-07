using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Datos de salida de un recargo de tarifa (HU-020, ADR-015).
/// </summary>
[SwaggerSchema(Description = "Respuesta de un recargo de tarifa")]
public class RecargoTarifaResponseDto
{
    [SwaggerSchema(Description = "ID único del recargo")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Código: COMBUSTIBLE, PEAJE, SEGURO, MANIPULACION, URGENCIA, REFRIGERACION, SOBREDIMENSION")]
    public string CodigoRecargo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre legible del recargo")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tipo de cálculo: PORCENTAJE o MONTO_FIJO")]
    public string TipoCalculo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Valor: % o monto absoluto")]
    public decimal Valor { get; set; }

    [SwaggerSchema(Description = "Si el recargo está activo (soft delete)")]
    public bool Activo { get; set; }
}