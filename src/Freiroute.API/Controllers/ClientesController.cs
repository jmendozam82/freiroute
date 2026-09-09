using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Cliente;
using Freiroute.DTO.Orden;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints del catálogo de clientes (shippers) y sus contactos (HU-019).
/// Módulo de permisos propio 'clientes'. El estado de crédito BLOQUEADO
/// dispara alerta visual en toda la UI.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly ISlaService _slaService;
    private readonly IReclamoService _reclamoService;

    public ClientesController(
        IClienteService clienteService,
        ISlaService slaService,
        IReclamoService reclamoService)
    {
        _clienteService = clienteService;
        _slaService = slaService;
        _reclamoService = reclamoService;
    }

    /// <summary>Lista paginada de clientes con filtros por tipo, estado de crédito y texto.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClienteResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClienteResponseDto>>>> GetAll(
        [FromQuery] string? tipoCliente,
        [FromQuery] string? estadoCredito,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.GetAllAsync(
            empresaId, tipoCliente, estadoCredito, q, page, pageSize);
        return Ok(ApiResponse<PagedResult<ClienteResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene un cliente con sus contactos por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Cliente con id '{id}' no encontrado."))
            : Ok(ApiResponse<ClienteResponseDto>.Ok(data));
    }

    /// <summary>Exporta el directorio de clientes a CSV compatible con Excel (CA-09, ADR-017).</summary>
    [HttpGet("exportar")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Read)]
    public async Task<IActionResult> Exportar()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var bytes = await _clienteService.ExportarExcelAsync(empresaId);
        return File(bytes, "text/csv; charset=utf-8", $"clientes-{DateTime.Today:yyyyMMdd}.csv");
    }

    /// <summary>Crea el cliente + contactos (RUC/NIT único por empresa, CA-08).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> Create(ClienteRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<ClienteResponseDto>.Ok(data, "Cliente creado"));
    }

    /// <summary>Actualiza el cliente + contactos (RUC/NIT único excluyéndose a sí mismo).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> Update(Guid id, ClienteRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<ClienteResponseDto>.Ok(data, "Cliente actualizado"));
    }

    /// <summary>Soft delete de un cliente (ADR-005). Nunca se elimina físicamente.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _clienteService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Cliente desactivado"));
    }

    /// <summary>Cambia el estado de crédito del cliente (CA-05).</summary>
    [HttpPatch("{id:guid}/estado-credito")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> CambiarEstadoCredito(
        Guid id, [FromBody] CambiarEstadoCreditoRequest request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.CambiarEstadoCreditoAsync(id, request.EstadoCredito, empresaId);
        return Ok(ApiResponse<ClienteResponseDto>.Ok(data, "Estado de crédito actualizado"));
    }

    /// <summary>Importación masiva CSV (CA-08, ADR-017): filas inválidas o RUC duplicado al log.</summary>
    [HttpPost("importar")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportarCsv([FromForm] IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(ApiResponse<int>.Fail("Debe adjuntar el archivo CSV."));
        }

        var empresaId = User.GetTenantEfectivo(HttpContext);
        using var stream = archivo.OpenReadStream();
        var importados = await _clienteService.ImportarCsvAsync(stream, empresaId);
        return Ok(ApiResponse<int>.Ok(importados, $"{importados} clientes importados"));
    }

    // ── Contactos ──────────────────────────────────────────────

    /// <summary>Agrega un contacto al cliente.</summary>
    [HttpPost("{id:guid}/contactos")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<ContactoClienteResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ContactoClienteResponseDto>>> AgregarContacto(
        Guid id, ContactoClienteRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.AgregarContactoAsync(id, request, empresaId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ContactoClienteResponseDto>.Ok(data, "Contacto agregado"));
    }

    /// <summary>Actualiza un contacto del cliente.</summary>
    [HttpPut("{id:guid}/contactos/{contactoId:guid}")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<ContactoClienteResponseDto>>> UpdateContacto(
        Guid id, Guid contactoId, ContactoClienteRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _clienteService.UpdateContactoAsync(id, contactoId, request, empresaId);
        return Ok(ApiResponse<ContactoClienteResponseDto>.Ok(data, "Contacto actualizado"));
    }

    /// <summary>Soft delete de un contacto del cliente.</summary>
    [HttpPatch("{id:guid}/contactos/{contactoId:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Update)]
    public async Task<IActionResult> DeactivateContacto(Guid id, Guid contactoId)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _clienteService.DeactivateContactoAsync(id, contactoId, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Contacto desactivado"));
    }

    // ── Sprint 5: SLA y reclamos (HU-031 · HU-032) ─────────────────

    /// <summary>Cumplimiento SLA del cliente en los últimos 30 días (HU-031 CA-03).</summary>
    [HttpGet("{id:guid}/sla-cumplimiento")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<SlaClienteResponseDto>>> GetSlaCumplimiento(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _slaService.GetCumplimientoClienteAsync(id, empresaId);
        return Ok(ApiResponse<SlaClienteResponseDto>.Ok(data));
    }

    /// <summary>Reclamos de un cliente del tenant (HU-032 CA-10).</summary>
    [HttpGet("{id:guid}/reclamos")]
    [RequirePermission(ModuloPermiso.Clientes, PermissionType.Read)]
    public async Task<IActionResult> GetReclamos(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var items = await _reclamoService.GetByClienteAsync(id, empresaId);
        return Ok(ApiResponse<IEnumerable<ReclamoListDto>>.Ok(items));
    }

    /// <summary>DTO de entrada del cambio de estado de crédito (no existe en Freiroute.DTO).</summary>
    public sealed class CambiarEstadoCreditoRequest
    {
        public string EstadoCredito { get; set; } = string.Empty;
    }
}