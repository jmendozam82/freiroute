using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Resumen de un reclamo para listados paginados (HU-032 CA-06).
/// Incluye JOINs con ordenes y clientes para nombres legibles.
/// </summary>
[SwaggerSchema(Description = "Resumen de un reclamo para listados")]
public class ReclamoListDto
{
    [SwaggerSchema(Description = "ID del reclamo")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Número legible del reclamo (REC-{PREFIX}-{AÑO}-{SECUENCIA})")]
    public string? NumeroReclamo { get; set; }

    [SwaggerSchema(Description = "Tipo de reclamo: DANO | PERDIDA | RETRASO | OTRO")]
    public string Tipo { get; set; } = null!;

    [SwaggerSchema(Description = "Estado: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO")]
    public string Estado { get; set; } = null!;

    [SwaggerSchema(Description = "Monto reclamado")]
    public decimal? MontoReclamado { get; set; }

    [SwaggerSchema(Description = "Número de la orden vinculada (calculado)")]
    public string OrdenNumero { get; set; } = null!;

    [SwaggerSchema(Description = "Nombre del cliente (calculado)")]
    public string ClienteNombre { get; set; } = null!;

    [SwaggerSchema(Description = "Fecha de creación del reclamo")]
    public DateTime FechaCreacion { get; set; }
}