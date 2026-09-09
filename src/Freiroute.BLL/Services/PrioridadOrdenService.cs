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
/// Priorización dinámica de órdenes (HU-029): cambio manual por el
/// dispatcher, consulta de órdenes críticas y elevación automática por
/// reglas del tenant (ejecutada por PrioridadOrdenesJob — ADR-013).
/// </summary>
public class PrioridadOrdenService : IPrioridadOrdenService
{
    private static readonly string[] PrioridadesValidas =
        { OrdenPrioridad.Critico, OrdenPrioridad.Alto, OrdenPrioridad.Normal, OrdenPrioridad.Bajo };

    private readonly IOrdenService _ordenService;
    private readonly IOrdenRepository _ordenRepository;
    private readonly IReglaPrioridadRepository _reglaPrioridadRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IAuditoriaService _auditoriaService;

    public PrioridadOrdenService(
        IOrdenService ordenService,
        IOrdenRepository ordenRepository,
        IReglaPrioridadRepository reglaPrioridadRepository,
        IClienteRepository clienteRepository,
        IAuditoriaService auditoriaService)
    {
        _ordenService = ordenService;
        _ordenRepository = ordenRepository;
        _reglaPrioridadRepository = reglaPrioridadRepository;
        _clienteRepository = clienteRepository;
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Cambio manual de prioridad (HU-029 CA-09). Valida contra
    /// OrdenPrioridad y audita CAMBIO_PRIORIDAD con valores anterior/nuevo.
    /// </summary>
    public async Task<OrdenResponseDto> CambiarPrioridadAsync(
        Guid ordenId, PrioridadRequestDto dto, Guid empresaId, Guid usuarioId)
    {
        if (!PrioridadesValidas.Contains(dto.Prioridad))
        {
            throw new BusinessException("Prioridad inválida. Valores válidos: CRITICO, ALTO, NORMAL, BAJO");
        }

        var orden = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");

        if (orden.Prioridad == dto.Prioridad)
        {
            // Idempotente: no cambia nada, se devuelve el estado actual.
            return await _ordenService.GetByIdAsync(ordenId, empresaId)
                ?? throw new BusinessException("Orden no encontrada");
        }

        var ok = await _ordenRepository.UpdatePrioridadAsync(ordenId, empresaId, dto.Prioridad);
        if (!ok)
        {
            throw new BusinessException("No se pudo actualizar la prioridad de la orden");
        }

        await _auditoriaService.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CAMBIO_PRIORIDAD, empresaId, usuarioId,
            nameof(Orden), ordenId,
            JsonSerializer.Serialize(new
            {
                ordenId,
                prioridadAnterior = orden.Prioridad,
                prioridadNueva = dto.Prioridad,
                motivo = dto.Motivo
            }));

        return await _ordenService.GetByIdAsync(ordenId, empresaId)
            ?? throw new BusinessException("Orden no encontrada");
    }

    /// <summary>
    /// Órdenes CRITICO/ALTO con más de 4 h sin avanzar (HU-029 CA-04 —
    /// GET /api/ordenes/criticas). El criterio lo resuelve la BD.
    /// </summary>
    public async Task<IEnumerable<OrdenListDto>> GetCriticasAsync(Guid empresaId)
    {
        var ordenes = await _ordenRepository.GetCriticasAsync(empresaId);
        return ordenes.Select(OrdenMapper.ToListDto);
    }

    /// <summary>
    /// Elección automática por reglas activas del tenant (HU-029 CA-08).
    /// Retorna el número de órdenes elevadas. Cross-tenant job: el scope
    /// itera TODAS las empresas (PrioridadOrdenesJob).
    /// </summary>
    public async Task<int> ElevarPrioridadesAutomaticasAsync(Guid empresaId)
    {
        var reglas = (await _reglaPrioridadRepository.GetActivasByEmpresaAsync(empresaId)).ToList();
        if (reglas.Count == 0)
        {
            return 0;
        }

        // VIP se consulta una sola vez (solo si alguna regla lo requiere).
        HashSet<Guid>? clientesVip = null;

        var elevadas = 0;

        foreach (var regla in reglas)
        {
            IEnumerable<Orden> candidatas;

            switch (regla.Condicion)
            {
                case "SIN_AVANCE": // Órdenes estancadas (críticas) — 4 h sin avanzar
                    candidatas = await _ordenRepository.GetCriticasAsync(empresaId);
                    break;

                case "ENTREGA_PROXIMA": // Entrega requerida en ≤ 24 h (riesgo SLA)
                    candidatas = await _ordenRepository.GetSlaEnRiesgoAsync(empresaId);
                    break;

                case "CLIENTE_VIP": // Órdenes en riesgo de clientes VIP
                    clientesVip ??= (await _clienteRepository.GetClientesSlaVipAsync(empresaId))
                        .Select(c => c.Id).ToHashSet();
                    candidatas = (await _ordenRepository.GetSlaEnRiesgoAsync(empresaId))
                        .Where(o => clientesVip.Contains(o.ClienteId));
                    break;

                default:
                    continue;
            }

            foreach (var orden in candidatas)
            {
                if (SlaCalculator.EsEstadoFinal(orden.Estado))
                {
                    continue;
                }

                if (RangoPrioridad(orden.Prioridad) >= RangoPrioridad(regla.NivelDestino))
                {
                    continue;
                }

                await _ordenRepository.UpdatePrioridadAsync(orden.Id, empresaId, regla.NivelDestino);

                await _auditoriaService.RegistrarAsync(
                    ModuloPermiso.Ordenes, AccionAuditoria.AUTO_PRIORIDAD, empresaId, null,
                    nameof(Orden), orden.Id,
                    JsonSerializer.Serialize(new
                    {
                        ordenId = orden.Id,
                        prioridadAnterior = orden.Prioridad,
                        prioridadNueva = regla.NivelDestino,
                        regla = regla.Nombre,
                        condicion = regla.Condicion
                    }));

                elevadas++;
            }
        }

        return elevadas;
    }

    private static int RangoPrioridad(string prioridad) => prioridad switch
    {
        OrdenPrioridad.Critico => 4,
        OrdenPrioridad.Alto => 3,
        OrdenPrioridad.Normal => 2,
        OrdenPrioridad.Bajo => 1,
        _ => 0
    };
}