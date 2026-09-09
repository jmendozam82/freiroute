using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Datos para crear un reclamo (HU-032 CA-01).
/// Tipo válido: DANO | PERDIDA | RETRASO | OTRO (ver TipoReclamo).
/// Un reclamo se vincula a una orden existente del mismo tenant (CA-02).
/// </summary>
[SwaggerSchema(Description = "Datos para crear un reclamo")]
public class ReclamoRequestDto
{
    [SwaggerSchema(Description = "ID de la orden vinculada (FK — debe existir en el mismo tenant)")]
    public Guid OrdenId { get; set; }

    [SwaggerSchema(Description = "Tipo de reclamo: DANO | PERDIDA | RETRASO | OTRO")]
    public string Tipo { get; set; } = null!;

    [SwaggerSchema(Description = "Descripción del reclamo (obligatoria)")]
    public string Descripcion { get; set; } = null!;

    [SwaggerSchema(Description = "Monto reclamado (opcional)")]
    public decimal? MontoReclamado { get; set; }

    [SwaggerSchema(Description = "Referencias de evidencia: URLs a Supabase Storage (Sprint 11)")]
    public List<string>? ReferenciasEvidencia { get; set; }
}