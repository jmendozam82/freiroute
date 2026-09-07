using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Resultado del simulador de costo de flete (HU-020 CA-04, ADR-015).
/// TarifaEncontrada=false indica que no hay tarifa vigente para la
/// combinación consultada (CA-05: alerta al crear órdenes).
/// </summary>
[SwaggerSchema(Description = "Resultado de la simulación de costo de flete")]
public class SimularCostoResponseDto
{
    [SwaggerSchema(Description = "ID de la tarifa aplicada — null si no se encontró")]
    public Guid? TarifaAplicadaId { get; set; }

    [SwaggerSchema(Description = "Nombre de la tarifa aplicada")]
    public string? TarifaNombre { get; set; }

    [SwaggerSchema(Description = "Tipo de tarifa: POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD")]
    public string TipoTarifa { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Costo base sin recargos")]
    public decimal CostoBase { get; set; }

    [SwaggerSchema(Description = "Moneda del costo")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Recargos aplicados con su monto calculado")]
    public List<RecargoAplicadoDto> RecargosAplicados { get; set; } = [];

    [SwaggerSchema(Description = "Suma de los montos de recargos")]
    public decimal TotalRecargos { get; set; }

    [SwaggerSchema(Description = "Costo total = CostoBase + TotalRecargos (respeta PrecioMinimo)")]
    public decimal CostoTotal { get; set; }

    [SwaggerSchema(Description = "false si no existe tarifa vigente para la combinación (CA-05)")]
    public bool TarifaEncontrada { get; set; }

    [SwaggerSchema(Description = "Advertencia (ej: se aplicó precio mínimo, seguro sobre valor declarado)")]
    public string? MensajeAdvertencia { get; set; }
}