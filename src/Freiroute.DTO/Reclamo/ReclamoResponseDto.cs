using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Detalle completo de un reclamo con historial de estados (HU-032 CA-07).
/// </summary>
[SwaggerSchema(Description = "Detalle completo de un reclamo")]
public class ReclamoResponseDto
{
    [SwaggerSchema(Description = "ID del reclamo")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Número legible del reclamo")]
    public string? NumeroReclamo { get; set; }

    [SwaggerSchema(Description = "ID de la orden vinculada")]
    public Guid OrdenId { get; set; }

    [SwaggerSchema(Description = "Número de la orden vinculada (calculado)")]
    public string OrdenNumero { get; set; } = null!;

    [SwaggerSchema(Description = "Nombre del cliente (calculado)")]
    public string ClienteNombre { get; set; } = null!;

    [SwaggerSchema(Description = "Tipo de reclamo: DANO | PERDIDA | RETRASO | OTRO")]
    public string Tipo { get; set; } = null!;

    [SwaggerSchema(Description = "Descripción del reclamo")]
    public string Descripcion { get; set; } = null!;

    [SwaggerSchema(Description = "Estado: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO")]
    public string Estado { get; set; } = null!;

    [SwaggerSchema(Description = "Monto reclamado")]
    public decimal? MontoReclamado { get; set; }

    [SwaggerSchema(Description = "Referencias de evidencia: URLs a Supabase Storage")]
    public List<string>? ReferenciasEvidencia { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }

    [SwaggerSchema(Description = "Última fecha de modificación")]
    public DateTime FechaModificacion { get; set; }

    [SwaggerSchema(Description = "Historial de estados del reclamo (ordenado por fecha DESC)")]
    public List<HistorialEstadoReclamoDto> Historial { get; set; } = new();
}