using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para vincular o actualizar el número de PO/SO de una orden
/// (HU-028 — PATCH /api/ordenes/{id}/po). Ambos campos son opcionales:
/// null limpia el valor, string lo fija.
/// </summary>
[SwaggerSchema(Description = "Datos para vincular el PO/SO de una orden")]
public class OrdenPoRequestDto
{
    [MaxLength(100)]
    [SwaggerSchema(Description = "Número de Purchase Order del cliente (trazabilidad ERP/WMS)")]
    public string? NumeroPo { get; set; }

    [MaxLength(100)]
    [SwaggerSchema(Description = "Número de Sales Order del cliente (independiente del PO)")]
    public string? NumeroSo { get; set; }
}