using Freiroute.DTO.Orden;
using Freiroute.Utility.Pagination;

namespace Freiroute.Aplicacion.Areas.Tenant.ViewModels;

/// <summary>
/// ViewModel del listado paginado de órdenes (HU-021 CA-14).
/// Conserva los filtros aplicados para re-renderizar la tabla y la
/// paginación (patrón server-side del área Tenant — Clientes/Index).
/// </summary>
public class OrdenIndexViewModel
{
    public PagedResult<OrdenListDto> Resultados { get; set; } = new();

    public string? Estado { get; set; }

    public string? Prioridad { get; set; }

    public string? Busqueda { get; set; }

    /// <summary>Filtro exact match por número de Purchase Order (HU-028 CA-03).</summary>
    public string? Po { get; set; }
}