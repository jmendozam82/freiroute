using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Ítem del reporte de cumplimiento SLA por cliente (HU-031 CA-07).
/// Período configurable (desde/hasta).
/// </summary>
[SwaggerSchema(Description = "Ítem del reporte de SLA por cliente")]
public class SlaReporteItemDto
{
    [SwaggerSchema(Description = "ID del cliente")]
    public Guid ClienteId { get; set; }

    [SwaggerSchema(Description = "Nombre del cliente")]
    public string ClienteNombre { get; set; } = null!;

    [SwaggerSchema(Description = "Tipo de cliente: REGULAR | VIP | OCASIONAL | CORPORATIVO | GOBIERNO")]
    public string TipoCliente { get; set; } = null!;

    [SwaggerSchema(Description = "Porcentaje de cumplimiento (0-100)")]
    public decimal PorcentajeCumplimiento { get; set; }

    [SwaggerSchema(Description = "Total de órdenes en el periodo")]
    public int TotalOrdenes { get; set; }

    [SwaggerSchema(Description = "Órdenes entregadas a tiempo")]
    public int OrdenesATiempo { get; set; }

    [SwaggerSchema(Description = "Órdenes entregadas tarde")]
    public int OrdenesTardias { get; set; }
}