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
/// Controlador para la importación CSV de órdenes (HU-022).
/// Utiliza patrón fail-soft.
/// </summary>
[ApiController]
[Route("api/ordenes")]
[Authorize]
public class ImportacionesOrdenController : ControllerBase
{
    private readonly IOrdenImportService _importService;

    public ImportacionesOrdenController(IOrdenImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("importar")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Create)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ImportacionOrdenResultDto>>> ImportarCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ImportacionOrdenResultDto>.Fail("Archivo CSV es requerido"));
        }

        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();
        
        using var stream = file.OpenReadStream();
        var result = await _importService.ImportarCsvAsync(stream, file.FileName, empresaId, usuarioId);
        
        return Ok(ApiResponse<ImportacionOrdenResultDto>.Ok(result, "Importación procesada"));
    }

    [HttpGet("importar/plantilla")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    [Produces("text/csv")]
    public async Task<IActionResult> ObtenerPlantillaCsv()
    {
        var csvString = await _importService.ObtenerPlantillaCsvAsync();
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvString);
        return File(bytes, "text/csv", "plantilla_importacion_ordenes.csv");
    }

    [HttpGet("importaciones")]
    [RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ImportacionOrdenResultDto>>>> GetImportaciones()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var result = await _importService.GetHistorialImportacionesAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<ImportacionOrdenResultDto>>.Ok(result));
    }
}
