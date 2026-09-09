using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Mappings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Orders;

namespace Freiroute.BLL.Services;

/// <summary>
/// Integración de órdenes de compra/venta (HU-028 — PO Integration).
/// Una PO puede vincularse a múltiples órdenes de transporte (relación 1:N).
/// La búsqueda por PO es exacta (no ILIKE) — ver OrdenRepository.GetPorPoAsync.
/// </summary>
public class OrdenPoService : IOrdenPoService
{
    private readonly IOrdenRepository _ordenRepository;
    private readonly IOrdenService _ordenService;
    private readonly IAuditoriaService _auditoriaService;

    public OrdenPoService(
        IOrdenRepository ordenRepository,
        IOrdenService ordenService,
        IAuditoriaService auditoriaService)
    {
        _ordenRepository = ordenRepository;
        _ordenService = ordenService;
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Lista todas las órdenes del tenant con un número de PO exacto
    /// (HU-028 CA-05 — GET /api/ordenes/por-po/{numero}).
    /// </summary>
    public async Task<IEnumerable<OrdenListDto>> GetPorPoAsync(string numeroPo, Guid empresaId)
    {
        var ordenes = await _ordenRepository.GetPorPoAsync(numeroPo.Trim(), empresaId);
        return ordenes.Select(OrdenMapper.ToListDto);
    }

    /// <summary>
    /// Vincula o actualiza el PO/SO de una orden (HU-028 — PATCH
    /// /api/ordenes/{id}/po). null limpia el valor, string lo fija.
    /// Registra auditoría VINCULAR_PO con los valores nuevos (CA-09).
    /// </summary>
    public async Task<OrdenResponseDto> VincularPoAsync(
        Guid ordenId, OrdenPoRequestDto dto, Guid empresaId, Guid usuarioId)
    {
        var orden = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");

        orden.NumeroPo = dto.NumeroPo;
        orden.NumeroSo = dto.NumeroSo;

        var ok = await _ordenRepository.UpdateAsync(orden);
        if (!ok)
        {
            throw new BusinessException("No se pudo actualizar la orden");
        }

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.VINCULAR_PO, empresaId, usuarioId,
            nameof(Orden), ordenId,
            JsonSerializer.Serialize(new { ordenId, numeroPo = dto.NumeroPo, numeroSo = dto.NumeroSo }));

        return await _ordenService.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");
    }

    /// <summary>
    /// Auditoría del vínculo/cambio de PO (HU-028 CA-09). Método legado del
    /// contrato — VincularPoAsync ya audita en una sola operación.
    /// </summary>
    public Task AuditarCambioPo(Guid ordenId, string? numeroPo, Guid empresaId, Guid usuarioId) =>
        _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.VINCULAR_PO, empresaId, usuarioId,
            nameof(Orden), ordenId,
            JsonSerializer.Serialize(new { ordenId, numeroPo }));
}