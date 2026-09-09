using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Filtros para la búsqueda paginada de reclamos (HU-032 CA-06).
/// Todos los campos son opcionales — solo se aplican los no nulos.
/// </summary>
[SwaggerSchema(Description = "Filtros para búsqueda de reclamos")]
public class ReclamoFiltroDto
{
    [SwaggerSchema(Description = "Filtrar por estado: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO")]
    public string? Estado { get; set; }

    [SwaggerSchema(Description = "Filtrar por tipo: DANO | PERDIDA | RETRASO | OTRO")]
    public string? Tipo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación desde (inclusive)")]
    public DateTime? Desde { get; set; }

    [SwaggerSchema(Description = "Fecha de creación hasta (inclusive)")]
    public DateTime? Hasta { get; set; }

    [SwaggerSchema(Description = "Número de página (default: 1)")]
    public int Page { get; set; } = 1;

    [SwaggerSchema(Description = "Tamaño de página (default: 20 — RNF-01.4)")]
    public int PageSize { get; set; } = 20;
}