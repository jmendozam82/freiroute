using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Reclamo;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;

namespace Freiroute.BLL.Services;

/// <summary>
/// Gestión de reclamos (HU-032 — Claims Management). CRUD, FSM de estados
/// (EstadoReclamo.Transiciones), historial INSERT-only y reportes.
/// El endpoint PATCH /api/reclamos/{id}/estado exige permiso
/// ordenes:update ([RequirePermission] en el controller — CA-12).
/// </summary>
public class ReclamoService : IReclamoService
{
    private readonly IReclamoRepository _reclamoRepository;
    private readonly IOrdenRepository _ordenRepository;
    private readonly IConfiguracionRepository _configuracionRepository;
    private readonly IAuditoriaService _auditoriaService;

    public ReclamoService(
        IReclamoRepository reclamoRepository,
        IOrdenRepository ordenRepository,
        IConfiguracionRepository configuracionRepository,
        IAuditoriaService auditoriaService)
    {
        _reclamoRepository = reclamoRepository;
        _ordenRepository = ordenRepository;
        _configuracionRepository = configuracionRepository;
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Crea el reclamo en ABIERTO (HU-032 CA-01). Valida la orden en el
    /// mismo tenant (CA-02), genera el número legible (CA-03/CA-11) y
    /// registra el historial inicial (null → ABIERTO).
    /// </summary>
    public async Task<ReclamoResponseDto> CreateAsync(
        ReclamoRequestDto dto, Guid empresaId, Guid usuarioId)
    {
        var orden = await _ordenRepository.GetByIdAsync(dto.OrdenId, empresaId)
            ?? throw new BusinessException("La orden vinculada no existe en la empresa");

        var entity = new Reclamo
        {
            EmpresaId = empresaId,
            OrdenId = dto.OrdenId,
            Tipo = dto.Tipo,
            Descripcion = dto.Descripcion,
            MontoReclamado = dto.MontoReclamado,
            Estado = EstadoReclamo.Abierto,
            ReferenciasEvidencia = dto.ReferenciasEvidencia?.ToArray(),
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = usuarioId,
            ModificadoPor = usuarioId
        };

        var id = await _reclamoRepository.CreateAsync(entity);

        // Historial inicial (espejo de G-17A en órdenes): null → ABIERTO.
        await _reclamoRepository.InsertHistorialAsync(new HistorialEstadoReclamo
        {
            EmpresaId = empresaId,
            ReclamoId = id,
            EstadoAnterior = null,
            EstadoNuevo = EstadoReclamo.Abierto,
            Motivo = "Creación de reclamo",
            UsuarioId = usuarioId,
            FechaCreacion = DateTime.UtcNow
        });

        // Número legible REC-{PREFIJO}-{AÑO}-{NNNN} (ADR-020, Opción B).
        // El repo concatena "REC-" — aquí solo se pasa el prefijo del tenant.
        var config = await _configuracionRepository.GetConfiguracionAsync(empresaId);
        var prefijo = config?.PrefijoOrden ?? "ORD";
        var anio = DateTime.UtcNow.Year;
        var numero = await _reclamoRepository.GenerarNumeroReclamoAsync(empresaId, prefijo, anio);
        await _reclamoRepository.UpdateNumeroReclamoAsync(id, empresaId, numero);

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CREAR_RECLAMO, empresaId, usuarioId,
            nameof(Reclamo), id,
            JsonSerializer.Serialize(new
            {
                reclamoId = id,
                ordenId = dto.OrdenId,
                numeroReclamo = numero,
                tipo = dto.Tipo,
                montoReclamado = dto.MontoReclamado
            }));

        return (await GetByIdAsync(id, empresaId))
            ?? throw new BusinessException("No se pudo recuperar el reclamo creado");
    }

    /// <summary>Obtiene el detalle con historial (HU-032 CA-07).</summary>
    public async Task<ReclamoResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var reclamo = await _reclamoRepository.GetByIdAsync(id, empresaId);
        if (reclamo is null)
        {
            return null;
        }

        var historial = await _reclamoRepository.GetHistorialAsync(id, empresaId);
        return MapToResponseDto(reclamo, historial);
    }

    /// <summary>Listado paginado con filtros (HU-032 CA-06).</summary>
    public async Task<(IEnumerable<ReclamoListDto> Items, int Total)> GetAllAsync(
        Guid empresaId, ReclamoFiltroDto filtro)
    {
        var (items, total) = await _reclamoRepository.GetAllAsync(empresaId, filtro);
        return (items.Select(MapToListDto), total);
    }

    /// <summary>
    /// Transición de estado con motivo obligatorio y validación FSM
    /// (HU-032 CA-04/CA-12). Registra historial y audita CAMBIO_ESTADO_RECLAMO.
    /// </summary>
    public async Task<ReclamoResponseDto> CambiarEstadoAsync(
        Guid id, ReclamoEstadoRequestDto dto, Guid empresaId, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            throw new BusinessException("El motivo de la transición es obligatorio");
        }

        var reclamo = await _reclamoRepository.GetByIdAsync(id, empresaId)
            ?? throw new BusinessException("Reclamo no encontrado");

        if (!EstadoReclamo.Transiciones.TryGetValue(reclamo.Estado, out var permitidas) ||
            !permitidas.Contains(dto.EstadoNuevo))
        {
            throw new BusinessException(
                $"Transición no permitida: {reclamo.Estado} → {dto.EstadoNuevo}");
        }

        var ahora = DateTime.UtcNow;
        await _reclamoRepository.UpdateEstadoAsync(id, empresaId, dto.EstadoNuevo, usuarioId, ahora);

        await _reclamoRepository.InsertHistorialAsync(new HistorialEstadoReclamo
        {
            EmpresaId = empresaId,
            ReclamoId = id,
            EstadoAnterior = reclamo.Estado,
            EstadoNuevo = dto.EstadoNuevo,
            Motivo = dto.Motivo,
            UsuarioId = usuarioId,
            FechaCreacion = ahora
        });

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CAMBIO_ESTADO_RECLAMO, empresaId, usuarioId,
            nameof(Reclamo), id,
            JsonSerializer.Serialize(new
            {
                reclamoId = id,
                estadoAnterior = reclamo.Estado,
                estadoNuevo = dto.EstadoNuevo,
                motivo = dto.Motivo
            }));

        return (await GetByIdAsync(id, empresaId))
            ?? throw new BusinessException("Reclamo no encontrado");
    }

    /// <summary>Reclamos de un cliente del tenant (HU-032 CA-10).</summary>
    public async Task<IEnumerable<ReclamoListDto>> GetByClienteAsync(
        Guid clienteId, Guid empresaId)
    {
        var items = await _reclamoRepository.GetByClienteAsync(clienteId, empresaId);
        return items.Select(MapToListDto);
    }

    /// <summary>Reporte agregado por tipo/estado en el período (HU-032 CA-09).</summary>
    public Task<IEnumerable<ReclamoReporteItemDto>> GetReporteAsync(
        Guid empresaId, DateTime desde, DateTime hasta) =>
        _reclamoRepository.GetReporteAsync(empresaId, desde, hasta);

    private static ReclamoListDto MapToListDto(Reclamo r) => new()
    {
        Id = r.Id,
        NumeroReclamo = r.NumeroReclamo,
        Tipo = r.Tipo,
        Estado = r.Estado,
        MontoReclamado = r.MontoReclamado,
        OrdenNumero = r.OrdenNumero ?? string.Empty,
        ClienteNombre = r.ClienteNombre ?? string.Empty,
        FechaCreacion = r.FechaCreacion
    };

    private static ReclamoResponseDto MapToResponseDto(
        Reclamo r, IEnumerable<HistorialEstadoReclamo> historial) => new()
    {
        Id = r.Id,
        NumeroReclamo = r.NumeroReclamo,
        OrdenId = r.OrdenId,
        OrdenNumero = r.OrdenNumero ?? string.Empty,
        ClienteNombre = r.ClienteNombre ?? string.Empty,
        Tipo = r.Tipo,
        Descripcion = r.Descripcion,
        Estado = r.Estado,
        MontoReclamado = r.MontoReclamado,
        ReferenciasEvidencia = r.ReferenciasEvidencia?.ToList(),
        FechaCreacion = r.FechaCreacion,
        FechaModificacion = r.FechaModificacion,
        Historial = historial.Select(h => new HistorialEstadoReclamoDto
        {
            EstadoAnterior = h.EstadoAnterior,
            EstadoNuevo = h.EstadoNuevo,
            Motivo = h.Motivo,
            UsuarioNombre = h.UsuarioNombre,
            FechaCreacion = h.FechaCreacion
        }).ToList()
    };
}