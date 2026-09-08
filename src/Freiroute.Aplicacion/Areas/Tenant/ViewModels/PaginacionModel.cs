namespace Freiroute.Aplicacion.Areas.Tenant.ViewModels;

/// <summary>
/// Modelo de la partial compartida _Paginacion.cshtml. No genérica a
/// propósito: PagedResult&lt;T&gt; es invariante y una partial Razor con
/// @model PagedResult&lt;dynamic&gt; no aceptaría PagedResult&lt;OrdenListDto&gt;.
/// El delegado PageUrl preserva los filtros activos en los query strings.
/// </summary>
public class PaginacionModel
{
    public int PaginaActual { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int ItemsEnPagina { get; set; }

    /// <summary>Construye la URL de una página (recibe el número de página).</summary>
    public Func<int, string>? PageUrl { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPreviousPage => PaginaActual > 1;

    public bool HasNextPage => PaginaActual * PageSize < TotalItems;

    public int Desde => ItemsEnPagina == 0 ? 0 : ((PaginaActual - 1) * PageSize) + 1;

    public int Hasta => (PaginaActual - 1) * PageSize + ItemsEnPagina;
}