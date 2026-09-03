using System.Text;
using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auditoria;
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de consulta del log de auditoría (HU-008).
/// Solo LECTURA — el log es inmutable (CA-06), no hay Update ni Delete.
/// Permisos sobre el módulo 'configuracion'. Filtros opcionales:
/// modulo, accion, fechaDesde, fechaHasta + paginado (RNF-01.4: 20/página).
/// </summary>
[ApiController]
[Route("api/auditoria")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private const int PageSizePorDefecto = 20;
    private const int MaxPageSizeExport = 10000;

    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Consulta paginada del log con filtros opcionales por módulo, acción y
    /// rango de fechas (HU-008 CA-03/04).
    /// </summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditoriaActivityResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditoriaActivityResponseDto>>>> GetPaged(
        [FromQuery] string? modulo,
        [FromQuery] string? accion,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageSizePorDefecto)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var resultado = await _auditoriaService.GetPagedAsync(
            empresaId, modulo, accion, fechaDesde, fechaHasta, page, pageSize);

        return Ok(ApiResponse<PagedResult<AuditoriaActivityResponseDto>>.Ok(resultado));
    }

    /// <summary>
    /// Exporta el log a CSV con los mismos filtros (HU-008 CA-05).
    /// El acceso se audita con la acción EXPORT.
    /// </summary>
    [HttpGet("export")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? modulo,
        [FromQuery] string? accion,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarioId = User.GetUsuarioId();

        var resultado = await _auditoriaService.GetPagedAsync(
            empresaId, modulo, accion, fechaDesde, fechaHasta, 1, MaxPageSizeExport);

        var csv = BuildCsv(resultado.Items);

        // HU-008 CA-05: todo acceso de exportación queda registrado.
        await _auditoriaService.RegistrarAsync(
            "auditoria", AccionAuditoria.EXPORT, empresaId, usuarioId,
            nameof(AuditoriaActividad), null,
            new { filas = resultado.TotalItems, formato = "csv", modulo, accion });

        var archivo = $"auditoria_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", archivo);
    }

    private static string BuildCsv(IEnumerable<AuditoriaActivityResponseDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id;empresa_id;usuario_id;modulo;accion;entidad_tipo;entidad_id;ip_address;fecha");

        foreach (var item in items)
        {
            // Los valores entre comillas dobles; el ';' es el separador (locale es).
            sb.AppendLine(string.Join(";",
                CsvEscape(item.Id.ToString()),
                CsvEscape(item.EmpresaId?.ToString()),
                CsvEscape(item.UsuarioId?.ToString()),
                CsvEscape(item.Modulo),
                CsvEscape(item.Accion),
                CsvEscape(item.EntidadTipo),
                CsvEscape(item.EntidadId?.ToString()),
                CsvEscape(item.IpAddress),
                CsvEscape(item.FechaCreacion.ToString("s"))));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}