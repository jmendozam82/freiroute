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
/// Gestión de rechazos de entrega y re-entregas (HU-030).
/// El registro del rechazo mueve la orden a FAILED_DELIVERY vía FSM
/// (ADR-019 — delegado en IOrdenService.CambiarEstadoAsync que valida la
/// transición y registra el historial). La re-entrega crea una orden nueva
/// CONFIRMED vinculada por orden_origen_id (CA-05/CA-06/CA-07).
/// </summary>
public class RechazoEntregaService : IRechazoEntregaService
{
    private static readonly string[] MotivosValidos =
        { "CLIENTE_AUSENTE", "DIRECCION_INCORRECTA", "MERCANCIA_DANADA", "RECHAZO_CLIENTE", "OTRO" };

    private readonly IOrdenService _ordenService;
    private readonly IOrdenRepository _ordenRepository;
    private readonly IRechazoEntregaRepository _rechazoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAuditoriaService _auditoriaService;

    public RechazoEntregaService(
        IOrdenService ordenService,
        IOrdenRepository ordenRepository,
        IRechazoEntregaRepository rechazoRepository,
        IUsuarioRepository usuarioRepository,
        IAuditoriaService auditoriaService)
    {
        _ordenService = ordenService;
        _ordenRepository = ordenRepository;
        _rechazoRepository = rechazoRepository;
        _usuarioRepository = usuarioRepository;
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Registra el rechazo (HU-030 CA-01/CA-02), mueve la orden a
    /// FAILED_DELIVERY vía la FSM (CA-03) y audita RECHAZO_ENTREGA (CA-11).
    /// </summary>
    public async Task<RechazoEntregaResponseDto> RegistrarRechazoAsync(
        Guid ordenId, RechazoEntregaRequestDto dto, Guid empresaId, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            throw new BusinessException("El motivo del rechazo es obligatorio");
        }

        if (!MotivosValidos.Contains(dto.Motivo))
        {
            throw new BusinessException("Motivo de rechazo inválido");
        }

        var orden = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");

        // FSM (ADR-019): solo estados que admiten FAILED_DELIVERY pasan.
        // El historial y la auditoría CAMBIO_ESTADO los registra OrdenService.
        await _ordenService.CambiarEstadoAsync(ordenId,
            new CambiarEstadoOrdenRequestDto
            {
                EstadoNuevo = OrdenEstado.FailedDelivery,
                Motivo = $"Rechazo de entrega: {dto.Motivo}"
            },
            empresaId, usuarioId);

        var entity = new RechazoEntrega
        {
            EmpresaId = empresaId,
            OrdenId = ordenId,
            Motivo = dto.Motivo,
            Descripcion = dto.Descripcion,
            UsuarioId = usuarioId,
            FechaCreacion = DateTime.UtcNow
        };

        var id = await _rechazoRepository.CreateAsync(entity);

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.RECHAZO_ENTREGA, empresaId, usuarioId,
            nameof(RechazoEntrega), id,
            JsonSerializer.Serialize(new
            {
                rechazoId = id,
                ordenId,
                motivo = dto.Motivo,
                descripcion = dto.Descripcion
            }));

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, empresaId);

        return new RechazoEntregaResponseDto
        {
            Id = id,
            OrdenId = ordenId,
            Motivo = dto.Motivo,
            Descripcion = dto.Descripcion,
            UsuarioNombre = usuario?.NombreCompleto,
            FechaCreacion = entity.FechaCreacion
        };
    }

    /// <summary>
    /// Crea la re-entrega heredando cliente, origen, destino, tipo de
    /// mercancía y unidad de medida de la orden fallida (HU-030 CA-05).
    /// Nace CONFIRMED (CA-07) con orden_origen_id a la orden original (CA-06).
    /// </summary>
    public async Task<OrdenResponseDto> CrearReentregaAsync(
        Guid ordenId, ReEntregaRequestDto? dto, Guid empresaId, Guid usuarioId)
    {
        var original = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");

        if (original.Estado != OrdenEstado.FailedDelivery)
        {
            throw new BusinessException("Solo se puede crear una re-entrega desde una orden en FAILED_DELIVERY");
        }

        var request = new OrdenRequestDto
        {
            ClienteId = original.ClienteId,
            OrigenId = original.OrigenId,
            DestinoId = original.DestinoId,
            TipoMercanciaId = original.TipoMercanciaId,
            UnidadMedidaId = original.UnidadMedidaId,
            TipoEmbalajeId = original.TipoEmbalajeId,
            TarifaId = original.TarifaId,
            Cantidad = original.Cantidad,
            PesoKg = original.PesoKg,
            VolumenM3 = original.VolumenM3,
            ValorDeclarado = original.ValorDeclarado,
            ModoTransporte = original.ModoTransporte,
            NivelServicio = original.NivelServicio,
            Prioridad = original.Prioridad,
            FechaPickupSolicitada = original.FechaPickupSolicitada,
            FechaEntregaRequerida = original.FechaEntregaRequerida,
            ReferenciaCliente = original.ReferenciaCliente,
            Instrucciones = dto?.Instrucciones ?? original.Instrucciones,
            NumeroPo = original.NumeroPo,
            NumeroSo = original.NumeroSo
        };

        // DRAFT + historial inicial (G-17A) — después se vincula y confirma.
        var nueva = await _ordenService.CreateAsync(request, empresaId, usuarioId);

        var entidadNueva = await _ordenRepository.GetByIdAsync(nueva.Id, empresaId)
            ?? throw new BusinessException("No se pudo recuperar la orden re-creada");

        entidadNueva.OrdenOrigenId = original.Id;
        await _ordenRepository.UpdateAsync(entidadNueva);

        // CONFIRMED → genera numero_orden (ADR-020) + historial + auditoría.
        await _ordenService.CambiarEstadoAsync(nueva.Id,
            new CambiarEstadoOrdenRequestDto
            {
                EstadoNuevo = OrdenEstado.Confirmed,
                Motivo = $"Re-entrega de orden fallida {original.NumeroOrden}"
            },
            empresaId, usuarioId);

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CREAR_REENTREGA, empresaId, usuarioId,
            nameof(Orden), nueva.Id,
            JsonSerializer.Serialize(new
            {
                nuevaOrdenId = nueva.Id,
                ordenOrigenId = ordenId,
                numeroOrdenOriginal = original.NumeroOrden
            }));

        return await _ordenService.GetByIdAsync(nueva.Id, empresaId)
            ?? throw new BusinessException("No se pudo recuperar la re-entrega");
    }

    /// <summary>
    /// Historial de re-entregas de una orden (HU-030 CA-10 — GET
    /// /api/ordenes/{id}/re-entregas). El repo consulta por orden_origen_id.
    /// </summary>
    public async Task<IEnumerable<OrdenListDto>> GetReentregasAsync(Guid ordenId, Guid empresaId)
    {
        var ordenes = await _ordenRepository.GetSubOrdenesAsync(ordenId, empresaId);
        return ordenes.Select(OrdenMapper.ToListDto);
    }
}