using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Tarifa;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del catálogo de tarifas base y recargos
/// (HU-020, ADR-015). Las tarifas se versionan: UpdateAsync cierra la
/// vigencia de la actual (fecha_vigencia_hasta = ayer) y crea una nueva
/// versión con vigencia desde hoy, copiando los recargos. Nunca modifica
/// el historial. Incluye el simulador de costo (CA-04).
/// </summary>
public class TarifaBaseService : ITarifaBaseService
{
    private readonly ITarifaBaseRepository _tarifaRepository;
    private readonly IZonaEntregaRepository _zonaRepository;
    private readonly IValidator<TarifaBaseRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<TarifaBaseService> _logger;

    public TarifaBaseService(
        ITarifaBaseRepository tarifaRepository,
        IZonaEntregaRepository zonaRepository,
        IValidator<TarifaBaseRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<TarifaBaseService> logger)
    {
        _tarifaRepository = tarifaRepository;
        _zonaRepository = zonaRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista paginada de tarifas con filtros y cálculo de vigencia.</summary>
    public async Task<PagedResult<TarifaBaseResponseDto>> GetAllAsync(
        Guid empresaId, Guid? zonaOrigenId, Guid? zonaDestinoId,
        string? modo, bool? soloVigentes, int page, int pageSize)
    {
        var todas = await _tarifaRepository.GetAllAsync(
            empresaId, zonaOrigenId, zonaDestinoId, modo, soloVigentes);

        var pageNumber = Math.Max(page, 1);
        var size = pageSize <= 0 ? 20 : pageSize;

        var items = new List<TarifaBaseResponseDto>();
        foreach (var tarifa in todas.Skip((pageNumber - 1) * size).Take(size))
        {
            items.Add(await MapToResponse(tarifa, empresaId));
        }

        return new PagedResult<TarifaBaseResponseDto>
        {
            Items = items,
            TotalItems = todas.Count(),
            PageNumber = pageNumber,
            PageSize = size
        };
    }

    /// <summary>Obtiene una tarifa con sus recargos por Id dentro de la empresa.</summary>
    public async Task<TarifaBaseResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var tarifa = await _tarifaRepository.GetByIdAsync(id, empresaId);
        return tarifa is null ? null : await MapToResponse(tarifa, empresaId);
    }

    /// <summary>
    /// Obtiene la tarifa vigente para la combinación zona/modo/servicio
    /// (ADR-015). Retorna null si no existe (CA-05).
    /// </summary>
    public async Task<TarifaBaseResponseDto?> GetVigenteAsync(
        Guid empresaId, Guid? zonaOrigenId, Guid? zonaDestinoId,
        string modo, string tipoServicio, DateOnly fecha)
    {
        var tarifa = await _tarifaRepository.GetVigenteAsync(
            empresaId, zonaOrigenId, zonaDestinoId, modo, tipoServicio, fecha);

        return tarifa is null ? null : await MapToResponse(tarifa, empresaId);
    }

    /// <summary>Crea la tarifa con sus recargos (nueva versión).</summary>
    public async Task<TarifaBaseResponseDto> CreateAsync(
        TarifaBaseRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var tarifa = MapToEntity(dto, empresaId);
        var tarifaId = await _tarifaRepository.CreateAsync(tarifa);

        foreach (var recargoDto in dto.Recargos)
        {
            await _tarifaRepository.CreateRecargoAsync(MapRecargoToEntity(recargoDto, tarifaId, empresaId));
        }

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.CREATE, empresaId, null,
            "TarifaBase", tarifaId, new { nombre = tarifa.Nombre, modo = tarifa.ModoTransporte });

        return await GetByIdAsync(tarifaId, empresaId)
               ?? throw new NotFoundException("tarifas_base", tarifaId);
    }

    /// <summary>
    /// Actualiza creando NUEVA versión (ADR-015, CA-03):
    /// 1) cierra la vigencia de la actual (fecha_vigencia_hasta = ayer),
    /// 2) crea la nueva con vigencia desde hoy,
    /// 3) copia los recargos de la versión anterior.
    /// </summary>
    public async Task<TarifaBaseResponseDto> UpdateAsync(
        Guid id, TarifaBaseRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _tarifaRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("tarifas_base", id);

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // 1) Cierra la vigencia de la actual (ayer), conservando el historial.
        var okCierre = await _tarifaRepository.CerrarVigenciaAsync(
            id, empresaId, hoy.AddDays(-1));
        if (!okCierre)
        {
            throw new NotFoundException("tarifas_base", id);
        }

        // 2) Nueva versión con vigencia desde hoy; ignora fecha_vigencia_desde del DTO.
        var nueva = MapToEntity(dto, empresaId);
        nueva.FechaVigenciaDesde = hoy;
        var nuevaId = await _tarifaRepository.CreateAsync(nueva);

        // 3) Copia los recargos activos de la versión anterior.
        var recargosAnteriores = await _tarifaRepository.GetRecargosAsync(id, empresaId);
        foreach (var recargo in recargosAnteriores.Where(r => r.Activo))
        {
            await _tarifaRepository.CreateRecargoAsync(new RecargoTarifa
            {
                Id = Guid.Empty,
                EmpresaId = empresaId,
                TarifaId = nuevaId,
                CodigoRecargo = recargo.CodigoRecargo,
                Nombre = recargo.Nombre,
                TipoCalculo = recargo.TipoCalculo,
                Valor = recargo.Valor,
                Activo = true
            });
        }

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.UPDATE, empresaId, null,
            "TarifaBase", nuevaId, new { versionAnterior = id, nombre = nueva.Nombre });

        return await GetByIdAsync(nuevaId, empresaId)
               ?? throw new NotFoundException("tarifas_base", nuevaId);
    }

    /// <summary>Soft delete: activo = false — conserva historial (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var tarifa = await _tarifaRepository.GetByIdAsync(id, empresaId)
                     ?? throw new NotFoundException("tarifas_base", id);

        var ok = await _tarifaRepository.DeactivateAsync(id, empresaId);

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.DEACTIVATE, empresaId, null,
            "TarifaBase", id, new { nombre = tarifa.Nombre });

        return ok;
    }

    // ── Recargos ───────────────────────────────────────────────

    /// <summary>Agrega un recargo a la tarifa (se valida contra el codigo unico).</summary>
    public async Task<RecargoTarifaResponseDto> AgregarRecargoAsync(
        Guid tarifaId, RecargoTarifaRequestDto dto, Guid empresaId)
    {
        var tarifa = await _tarifaRepository.GetByIdAsync(tarifaId, empresaId)
                     ?? throw new NotFoundException("tarifas_base", tarifaId);

        ValidarRecargo(dto);

        var recargos = await _tarifaRepository.GetRecargosAsync(tarifaId, empresaId);
        if (recargos.Any(r => r.Activo &&
                              string.Equals(r.CodigoRecargo, dto.CodigoRecargo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException(
                $"La tarifa ya tiene un recargo activo con el código '{dto.CodigoRecargo}'.");
        }

        var entidad = MapRecargoToEntity(dto, tarifaId, empresaId);
        var recargoId = await _tarifaRepository.CreateRecargoAsync(entidad);

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.CREATE, empresaId, null,
            "RecargoTarifa", recargoId, new { tarifaId, codigo = entidad.CodigoRecargo });

        return MapRecargoToResponse(entidad, recargoId);
    }

    /// <summary>Actualiza un recargo de la tarifa.</summary>
    public async Task<RecargoTarifaResponseDto> UpdateRecargoAsync(
        Guid tarifaId, Guid recargoId, RecargoTarifaRequestDto dto, Guid empresaId)
    {
        var tarifa = await _tarifaRepository.GetByIdAsync(tarifaId, empresaId)
                     ?? throw new NotFoundException("tarifas_base", tarifaId);

        ValidarRecargo(dto);

        var recargos = await _tarifaRepository.GetRecargosAsync(tarifaId, empresaId);
        var existente = recargos.FirstOrDefault(r => r.Id == recargoId)
                        ?? throw new NotFoundException("recargos_tarifa", recargoId);

        if (recargos.Any(r => r.Id != recargoId &&
                              r.Activo &&
                              string.Equals(r.CodigoRecargo, dto.CodigoRecargo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException(
                $"La tarifa ya tiene un recargo activo con el código '{dto.CodigoRecargo}'.");
        }

        var entidad = MapRecargoToEntity(dto, tarifaId, empresaId);
        entidad.Id = recargoId;
        entidad.FechaCreacion = existente.FechaCreacion;

        var ok = await _tarifaRepository.UpdateRecargoAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("recargos_tarifa", recargoId);
        }

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.UPDATE, empresaId, null,
            "RecargoTarifa", recargoId, new { tarifaId });

        return MapRecargoToResponse(entidad, recargoId);
    }

    /// <summary>Soft delete de un recargo: activo = false.</summary>
    public async Task<bool> DeactivateRecargoAsync(
        Guid tarifaId, Guid recargoId, Guid empresaId)
    {
        var tarifa = await _tarifaRepository.GetByIdAsync(tarifaId, empresaId)
                     ?? throw new NotFoundException("tarifas_base", tarifaId);

        var ok = await _tarifaRepository.DeactivateRecargoAsync(recargoId, empresaId);
        if (!ok)
        {
            throw new NotFoundException("recargos_tarifa", recargoId);
        }

        await _auditoria.RegistrarAsync(
            "tarifas_base", AccionAuditoria.DEACTIVATE, empresaId, null,
            "RecargoTarifa", recargoId, new { tarifaId });

        return ok;
    }

    // ── Simulador de costo ─────────────────────────────────────

    /// <summary>
    /// Simulador de costo (HU-020 CA-04, ADR-015): aplica la tarifa vigente
    /// y calcula CostoBase + recargos. Respeta PrecioMinimo y el seguro
    /// sobre valor declarado. Si no hay tarifa, TarifaEncontrada=false (CA-05).
    /// </summary>
    public async Task<SimularCostoResponseDto> SimularCostoAsync(
        SimularCostoRequestDto dto, Guid empresaId)
    {
        var fecha = dto.FechaPickup == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : dto.FechaPickup;

        var tarifa = await _tarifaRepository.GetVigenteAsync(
            empresaId, dto.ZonaOrigenId, dto.ZonaDestinoId,
            dto.ModoTransporte, dto.TipoServicio, fecha);

        if (tarifa is null)
        {
            return new SimularCostoResponseDto
            {
                TarifaEncontrada = false,
                MensajeAdvertencia =
                    "No existe tarifa vigente para la combinación de zonas, modo y servicio consultada."
            };
        }

        // Costo base según el modelo de precio (ADR-015).
        var costoBase = tarifa.TipoTarifa switch
        {
            TipoTarifa.PorKg => tarifa.PrecioUnitario * dto.PesoKg,
            TipoTarifa.PorM3 => tarifa.PrecioUnitario * dto.VolumenM3,
            TipoTarifa.PorKm => tarifa.PrecioUnitario * dto.DistanciaKm,
            _ => tarifa.PrecioUnitario // FIJO_VIAJE (y POR_UNIDAD con cantidad 1).
        };

        var advertencias = new List<string>();

        // Precio mínimo aplicable.
        if (tarifa.PrecioMinimo.HasValue && costoBase < tarifa.PrecioMinimo.Value)
        {
            costoBase = tarifa.PrecioMinimo.Value;
            advertencias.Add("Se aplicó el precio mínimo de la tarifa.");
        }

        // Recargos activos.
        var recargos = await _tarifaRepository.GetRecargosAsync(tarifa.Id, empresaId);
        var recargosAplicados = new List<RecargoAplicadoDto>();
        var totalRecargos = 0m;

        foreach (var recargo in recargos.Where(r => r.Activo))
        {
            var monto = CalcularMontoRecargo(
                recargo, costoBase, dto.ValorDeclarado,
                referenciasSeguro: advertencias);

            totalRecargos += monto;
            recargosAplicados.Add(new RecargoAplicadoDto
            {
                Nombre = recargo.Nombre,
                TipoCalculo = recargo.TipoCalculo,
                Valor = recargo.Valor,
                Monto = Math.Round(monto, 4, MidpointRounding.AwayFromZero)
            });
        }

        var costoTotal = Math.Round(costoBase + totalRecargos, 4, MidpointRounding.AwayFromZero);

        return new SimularCostoResponseDto
        {
            TarifaAplicadaId = tarifa.Id,
            TarifaNombre = tarifa.Nombre,
            TipoTarifa = tarifa.TipoTarifa,
            CostoBase = Math.Round(costoBase, 4, MidpointRounding.AwayFromZero),
            Moneda = tarifa.Moneda,
            RecargosAplicados = recargosAplicados,
            TotalRecargos = Math.Round(totalRecargos, 4, MidpointRounding.AwayFromZero),
            CostoTotal = costoTotal,
            TarifaEncontrada = true,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(TarifaBaseRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private static void ValidarRecargo(RecargoTarifaRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoRecargo) ||
            !CodigoRecargo.Todos.Contains(dto.CodigoRecargo))
        {
            throw new BusinessException("El código del recargo no es válido.", "RECARGO_CODIGO_INVALIDO");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new BusinessException("El nombre del recargo es obligatorio.", "RECARGO_NOMBRE_REQUERIDO");
        }

        if (dto.TipoCalculo is not (TipoCalculoRecargo.Porcentaje or TipoCalculoRecargo.MontoFijo))
        {
            throw new BusinessException("El tipo de cálculo debe ser PORCENTAJE o MONTO_FIJO.", "RECARGO_TIPO_INVALIDO");
        }

        if (dto.Valor <= 0)
        {
            throw new BusinessException("El valor del recargo debe ser mayor que cero.", "RECARGO_VALOR_INVALIDO");
        }

        if (dto.TipoCalculo == TipoCalculoRecargo.Porcentaje && dto.Valor > 100)
        {
            throw new BusinessException("Un porcentaje no puede exceder 100%.", "RECARGO_PORCENTAJE_INVALIDO");
        }
    }

    private static decimal CalcularMontoRecargo(
        RecargoTarifa recargo, decimal costoBase, decimal valorDeclarado, List<string> referenciasSeguro)
    {
        if (recargo.TipoCalculo == TipoCalculoRecargo.MontoFijo)
        {
            return recargo.Valor;
        }

        // SEGURO porcentual se calcula sobre el valor declarado de la carga.
        if (string.Equals(recargo.CodigoRecargo, CodigoRecargo.Seguro, StringComparison.OrdinalIgnoreCase) &&
            valorDeclarado > 0)
        {
            referenciasSeguro.Add("El recargo de seguro se calculó sobre el valor declarado de la carga.");
            return valorDeclarado * recargo.Valor / 100m;
        }

        return costoBase * recargo.Valor / 100m;
    }

    private async Task<TarifaBaseResponseDto> MapToResponse(TarifaBase t, Guid empresaId)
    {
        var recargos = await _tarifaRepository.GetRecargosAsync(t.Id, empresaId);
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        return new TarifaBaseResponseDto
        {
            Id = t.Id,
            Nombre = t.Nombre,
            Codigo = t.Codigo,
            ZonaOrigenId = t.ZonaOrigenId,
            ZonaOrigenNombre = await ZonaNombreAsync(t.ZonaOrigenId, empresaId),
            ZonaDestinoId = t.ZonaDestinoId,
            ZonaDestinoNombre = await ZonaNombreAsync(t.ZonaDestinoId, empresaId),
            ModoTransporte = t.ModoTransporte,
            TipoServicio = t.TipoServicio,
            TipoTarifa = t.TipoTarifa,
            TipoTarifaLabel = TipoTarifaLabel(t.TipoTarifa),
            PrecioUnitario = t.PrecioUnitario,
            PrecioMinimo = t.PrecioMinimo,
            Moneda = t.Moneda,
            FechaVigenciaDesde = t.FechaVigenciaDesde,
            FechaVigenciaHasta = t.FechaVigenciaHasta,
            EsVigente = t.Activo &&
                        t.FechaVigenciaDesde <= hoy &&
                        (t.FechaVigenciaHasta is null || t.FechaVigenciaHasta >= hoy),
            EsVencida = t.FechaVigenciaHasta.HasValue && t.FechaVigenciaHasta < hoy,
            Recargos = recargos.Select(r => MapRecargoToResponse(r, r.Id)).ToList(),
            Activo = t.Activo,
            FechaCreacion = t.FechaCreacion
        };
    }

    private async Task<string?> ZonaNombreAsync(Guid? zonaId, Guid empresaId)
    {
        if (!zonaId.HasValue)
        {
            return null;
        }

        var zona = await _zonaRepository.GetByIdAsync(zonaId.Value, empresaId);
        return zona?.Nombre;
    }

    private static TarifaBase MapToEntity(TarifaBaseRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim(),
        ZonaOrigenId = dto.ZonaOrigenId,
        ZonaDestinoId = dto.ZonaDestinoId,
        ModoTransporte = dto.ModoTransporte,
        TipoServicio = dto.TipoServicio,
        TipoTarifa = dto.TipoTarifa,
        PrecioUnitario = dto.PrecioUnitario,
        PrecioMinimo = dto.PrecioMinimo,
        Moneda = dto.Moneda,
        FechaVigenciaDesde = dto.FechaVigenciaDesde,
        FechaVigenciaHasta = dto.FechaVigenciaHasta,
        Activo = true
    };

    private static RecargoTarifa MapRecargoToEntity(
        RecargoTarifaRequestDto dto, Guid tarifaId, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        TarifaId = tarifaId,
        CodigoRecargo = dto.CodigoRecargo,
        Nombre = dto.Nombre,
        TipoCalculo = dto.TipoCalculo,
        Valor = dto.Valor,
        Activo = true
    };

    private static RecargoTarifaResponseDto MapRecargoToResponse(RecargoTarifa r, Guid recargoId) => new()
    {
        Id = recargoId,
        CodigoRecargo = r.CodigoRecargo,
        Nombre = r.Nombre,
        TipoCalculo = r.TipoCalculo,
        Valor = r.Valor,
        Activo = r.Activo
    };

    private static string TipoTarifaLabel(string tipo) => tipo switch
    {
        TipoTarifa.PorKg => "Por kg",
        TipoTarifa.PorM3 => "Por m³",
        TipoTarifa.PorKm => "Por km",
        TipoTarifa.PorUnidad => "Por unidad",
        _ => "Fijo por viaje"
    };
}