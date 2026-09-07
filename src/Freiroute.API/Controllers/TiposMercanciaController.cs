using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Mercancia;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints del catálogo de tipos de mercancía (HU-017).
/// Incluye clasificación HAZMAT (clase ONU) y filtro de peligrosas (CA-07).
/// </summary>
[ApiController]
[Route("api/tipos-mercancia")]
[Authorize]
public class TiposMercanciaController : ControllerBase
{
    private readonly ITipoMercanciaService _mercanciaService;

    public TiposMercanciaController(ITipoMercanciaService mercanciaService)
    {
        _mercanciaService = mercanciaService;
    }

    /// <summary>Lista tipos de mercancía con filtro opcional de solo peligrosas (CA-07).</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TipoMercanciaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TipoMercanciaResponseDto>>>> GetAll(
        [FromQuery] bool? soloPeligrosas)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _mercanciaService.GetAllAsync(empresaId, soloPeligrosas);
        return Ok(ApiResponse<IEnumerable<TipoMercanciaResponseDto>>.Ok(data));
    }

    /// <summary>Obtiene un tipo de mercancía por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<TipoMercanciaResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _mercanciaService.GetByIdAsync(id, empresaId);
        return data is null
            ? NotFound(ApiResponse<string>.Fail($"Tipo de mercancía con id '{id}' no encontrado."))
            : Ok(ApiResponse<TipoMercanciaResponseDto>.Ok(data));
    }

    /// <summary>Crea un tipo de mercancía (con validaciones HAZMAT).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<TipoMercanciaResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TipoMercanciaResponseDto>>> Create(TipoMercanciaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _mercanciaService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = data.Id },
            ApiResponse<TipoMercanciaResponseDto>.Ok(data, "Tipo de mercancía creado"));
    }

    /// <summary>Actualiza un tipo de mercancía (con validaciones HAZMAT).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<TipoMercanciaResponseDto>>> Update(Guid id, TipoMercanciaRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var data = await _mercanciaService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<TipoMercanciaResponseDto>.Ok(data, "Tipo de mercancía actualizado"));
    }

    /// <summary>Soft delete de un tipo de mercancía (ADR-005).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _mercanciaService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Tipo de mercancía desactivado"));
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
        var importados = await _mercanciaService.ImportarCsvAsync(stream, empresaId);
        return Ok(ApiResponse<int>.Ok(importados, $"{importados} tipos de mercancía importados"));
    }
}