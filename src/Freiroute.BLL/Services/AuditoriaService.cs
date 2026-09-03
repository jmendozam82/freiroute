using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Auditoria;
using Freiroute.Entity;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Servicio transversal de auditoría (HU-008). Se inyecta en TODOS los demás
/// servicios del sistema. El log es inmutable (solo escritura) y NUNCA propaga
/// excepciones: si registrar la auditoría falla, se loguea y la operación de
/// negocio continúa (la auditoría no puede tumbar el negocio — igual que el
/// repositorio, defensa en dos capas).
/// </summary>
public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repository;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(
        IAuditoriaRepository repository,
        ILogger<AuditoriaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Registra una acción de auditoría. NUNCA lanza excepción (try/catch + log).
    /// Detalles se serializa a JSON para la columna JSONB.
    /// </summary>
    public async Task RegistrarAsync(
        string modulo,
        string accion,
        Guid empresaId,
        Guid? usuarioId = null,
        string? entidadTipo = null,
        Guid? entidadId = null,
        object? detalles = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditoria = new AuditoriaActividad
            {
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                Modulo = modulo,
                Accion = accion,
                EntidadTipo = entidadTipo,
                EntidadId = entidadId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Detalles = detalles is null
                    ? null
                    : JsonSerializer.Serialize(detalles),
                FechaCreacion = DateTime.UtcNow
            };

            await _repository.RegistrarAsync(auditoria);
        }
        catch (Exception ex)
        {
            // La auditoría nunca debe tumbar la operación de negocio.
            _logger.LogError(ex,
                "AuditoriaService no pudo registrar la acción. Modulo={Modulo}, Accion={Accion}, EmpresaId={EmpresaId}, UsuarioId={UsuarioId}",
                modulo, accion, empresaId, usuarioId);
        }
    }

    /// <summary>
    /// Consulta paginada del log de auditoría (HU-008 CA-03/04).
    /// Lectura pura — el log es inmutable, no existe Update ni Delete.
    /// </summary>
    public async Task<PagedResult<AuditoriaActivityResponseDto>> GetPagedAsync(
        Guid empresaId,
        string? modulo,
        string? accion,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pageNumber,
        int pageSize)
    {
        var result = await _repository.GetPagedAsync(
            empresaId, modulo, accion, fechaDesde, fechaHasta, pageNumber, pageSize);

        return new PagedResult<AuditoriaActivityResponseDto>
        {
            Items = result.Items.Select(MapToResponseDto),
            TotalItems = result.TotalItems,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    private static AuditoriaActivityResponseDto MapToResponseDto(AuditoriaActividad a) => new()
    {
        Id = a.Id,
        EmpresaId = a.EmpresaId,
        UsuarioId = a.UsuarioId,
        Modulo = a.Modulo,
        Accion = a.Accion,
        EntidadTipo = a.EntidadTipo,
        EntidadId = a.EntidadId,
        IpAddress = a.IpAddress,
        UserAgent = a.UserAgent,
        Detalles = a.Detalles,
        FechaCreacion = a.FechaCreacion
    };
}