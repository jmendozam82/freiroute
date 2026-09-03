namespace Freiroute.Utility.Pagination;

/// <summary>
/// Resultado paginado estándar del sistema (RNF-01.4: 20 registros/página).
/// HasPreviousPage y HasNextPage son propiedades calculadas.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalItems;
}