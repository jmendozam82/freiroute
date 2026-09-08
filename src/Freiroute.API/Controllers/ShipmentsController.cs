using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Esqueleto del controlador de Shipments (HU-025).
/// En Sprint 4 solo expone la lista de órdenes consolidadas en un shipment.
/// </summary>
[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IOrdenService _ordenService;

    public ShipmentsController(IOrdenService ordenService)
    {
        _ordenService = ordenService;
    }

    [HttpGet("{id:guid}/ordenes")]
    [RequirePermission(ModuloPermiso.Embarques, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrdenListDto>>>> GetOrdenesByShipmentId(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _ordenService.GetByShipmentIdAsync(id, empresaId);
        return Ok(ApiResponse<IEnumerable<OrdenListDto>>.Ok(result));
    }
}
