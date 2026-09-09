using Freiroute.DTO.Reclamo;

namespace Freiroute.Aplicacion.Areas.Tenant.ViewModels;

/// <summary>
/// ViewModel del listado paginado de reclamos (HU-032 CA-06).
/// Conserva los filtros aplicados (estado, tipo, rango de fechas) para
/// re-renderizar la tabla y la paginación. Sigue el patrón server-side
/// del área Tenant (Ordenes/Index con PagedResult).
/// </summary>
public class ReclamoIndexViewModel
{
    /// <summary>Reclamos de la página actual.</summary>
    public IEnumerable<ReclamoListDto> Items { get; set; } = [];

    /// <summary>Total de reclamos que cumplen los filtros.</summary>
    public int Total { get; set; }

    /// <summary>Número de página actual (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Registros por página (RNF-01.4: 20).</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Total de páginas calculado.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    /// <summary>Primer ítem visible del rango "Mostrando X–Y de Z".</summary>
    public int StartItem => Total == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>Último ítem visible del rango "Mostrando X–Y de Z".</summary>
    public int EndItem => (Page - 1) * PageSize + Items.Count();

    /// <summary>Filtro aplicado para conservar en los query strings.</summary>
    public ReclamoFiltroDto? Filtro { get; set; }
}
