using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Payload opcional de la re-entrega (HU-030 CA-04). Solo contiene
/// instrucciones especiales — el resto de campos (cliente, origen,
/// destino, tipo mercancía, unidad de medida) se heredan de la orden
/// original (CA-05).
/// </summary>
[SwaggerSchema(Description = "Datos opcionales para crear una re-entrega")]
public class ReEntregaRequestDto
{
    [SwaggerSchema(Description = "Instrucciones especiales de la re-entrega (opcional)")]
    public string? Instrucciones { get; set; }
}