using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Ubicacion;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints del catálogo de ubicaciones (HU-015, ADR-014).
/// El empresa_id SIEMPRE viene del JWT (o header X-Empresa-Id), nunca del body.
/// La creación/actualización con dirección dispara geocodificación fail-soft.
/// </summary>
[ApiController]
[Route("api/ubicaciones")]
[Authorize]
public class UbicacionesController : ControllerBase
{
    private readonly IUbicacionService _ubicacionService;

    public UbicacionesController(IUbicacionService ubicacionService)
    {
        _ubicacionService = ubicacionService;
    }

    /// <summary>Lista paginada de ubicaciones con filtros por tipo y texto.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UbicacionResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UbicacionResponseDto>>>> GetAll(
        [FromQuery] string? tipo,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.GetAllAsync(empresaId, tipo, q, page, pageSize);
        return Ok(ApiResponse<PagedResult<UbicacionResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene una ubicación por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<UbicacionResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Ubicación con id '{id}' no encontrada."))
            : Ok(ApiResponse<UbicacionResponseDto>.Ok(data));
    }

    /// <summary>Ubicaciones georreferenciadas para el mapa (payload mínimo, CA-04).</summary>
    [HttpGet("mapa")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UbicacionMapaDto>>>> GetParaMapa()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.GetParaMapaAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<UbicacionMapaDto>>.Ok(data));
    }

    /// <summary>Crea una ubicación (geocodifica la dirección en background, CA-02).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<UbicacionResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UbicacionResponseDto>>> Create(UbicacionRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<UbicacionResponseDto>.Ok(data, "Ubicación creada"));
    }

    /// <summary>Actualiza una ubicación (re-geocodifica si cambió la dirección).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<UbicacionResponseDto>>> Update(Guid id, UbicacionRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<UbicacionResponseDto>.Ok(data, "Ubicación actualizada"));
    }

    /// <summary>Ajuste manual de coordenadas (CA-05) tras geocodificación inexacta.</summary>
    [HttpPatch("{id:guid}/coordenadas")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<UbicacionResponseDto>>> ActualizarCoordenadas(
        Guid id, ActualizarCoordenadasDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _ubicacionService.ActualizarCoordenadasAsync(id, request, empresaId);
        return Ok(ApiResponse<UbicacionResponseDto>.Ok(data, "Coordenadas actualizadas"));
    }

    /// <summary>Soft delete de una ubicación (ADR-005). Nunca se elimina físicamente.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _ubicacionService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Ubicación desactivada"));
    }

    /// <summary>Importación masiva CSV (CA-06, ADR-017): filas inválidas al log y continúa.</summary>
    [HttpPost("importar")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportarCsv([FromForm] IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(ApiResponse<int>.Fail("Debe adjuntar el archivo CSV."));
        }

        var empresaId = User.GetTenantEfectivo(HttpContext);
        using var stream = archivo.OpenReadStream();
        var importados = await _ubicacionService.ImportarCsvAsync(stream, empresaId);
        return Ok(ApiResponse<int>.Ok(importados, $"{importados} ubicaciones importadas"));
    }
}