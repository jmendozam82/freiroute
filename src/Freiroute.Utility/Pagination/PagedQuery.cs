namespace Freiroute.Utility.Pagination;

/// <summary>
/// Parámetros de paginación estándar de consultas (RNF-01.4).
/// PageSize default 20; Offset calculado para las queries SQL (LIMIT/OFFSET).
/// </summary>
public class PagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Desplazamiento calculado: (PageNumber - 1) * PageSize.</summary>
    public int Offset => (PageNumber - 1) * PageSize;
}