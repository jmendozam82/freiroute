using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Ítem del reporte de reclamos por período, tipo y resolución
/// (HU-032 CA-09).
/// </summary>
[SwaggerSchema(Description = "Ítem del reporte de reclamos")]
public class ReclamoReporteItemDto
{
    [SwaggerSchema(Description = "Tipo de reclamo: DANO | PERDIDA | RETRASO | OTRO")]
    public string Tipo { get; set; } = null!;

    [SwaggerSchema(Description = "Estado: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO")]
    public string Estado { get; set; } = null!;

    [SwaggerSchema(Description = "Cantidad de reclamos en el grupo")]
    public int Total { get; set; }

    [SwaggerSchema(Description = "Suma de montos reclamados del grupo")]
    public decimal MontoTotal { get; set; }

    [SwaggerSchema(Description = "Monto promedio reclamado del grupo")]
    public decimal MontoPromedio { get; set; }
}