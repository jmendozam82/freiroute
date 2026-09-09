using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Cumplimiento de SLA de un cliente (HU-031 CA-03).
/// Porcentaje de órdenes DELIVERED a tiempo en los últimos 30 días.
/// </summary>
[SwaggerSchema(Description = "Cumplimiento SLA de un cliente")]
public class SlaClienteResponseDto
{
    [SwaggerSchema(Description = "ID del cliente")]
    public Guid ClienteId { get; set; }

    [SwaggerSchema(Description = "Nombre del cliente")]
    public string ClienteNombre { get; set; } = null!;

    [SwaggerSchema(Description = "Total de órdenes en el periodo evaluado")]
    public int TotalOrdenes { get; set; }

    [SwaggerSchema(Description = "Órdenes entregadas a tiempo")]
    public int OrdenesATiempo { get; set; }

    [SwaggerSchema(Description = "Órdenes entregadas tarde")]
    public int OrdenesTardias { get; set; }

    [SwaggerSchema(Description = "Porcentaje de cumplimiento (0-100)")]
    public decimal PorcentajeCumplimiento { get; set; }

    [SwaggerSchema(Description = "SLA configurado del cliente en días")]
    public int SlaDias { get; set; }
}