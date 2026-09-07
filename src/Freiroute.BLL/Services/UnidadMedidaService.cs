using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Unidad;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del catálogo de unidades de medida (HU-018).
/// El simulador de conversión (CA-05) convierte entre símbolos del
/// mismo tipo usando el factor de conversión a la unidad base.
/// </summary>
public class UnidadMedidaService : IUnidadMedidaService
{
    private readonly IUnidadMedidaRepository _unidadRepository;
    private readonly IValidator<UnidadMedidaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<UnidadMedidaService> _logger;

    public UnidadMedidaService(
        IUnidadMedidaRepository unidadRepository,
        IValidator<UnidadMedidaRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<UnidadMedidaService> logger)
    {
        _unidadRepository = unidadRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista unidades con filtro opcional por tipo.</summary>
    public async Task<IEnumerable<UnidadMedidaResponseDto>> GetAllAsync(
        Guid empresaId, string? tipo = null)
    {
        var unidades = await _unidadRepository.GetAllAsync(empresaId, tipo);
        return unidades.Select(MapToResponse);
    }

    /// <summary>Obtiene una unidad por Id dentro de la empresa.</summary>
    public async Task<UnidadMedidaResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var unidad = await _unidadRepository.GetByIdAsync(id, empresaId);
        return unidad is null ? null : MapToResponse(unidad);
    }

    /// <summary>Crea la unidad de medida (símbolo único por empresa).</summary>
    public async Task<UnidadMedidaResponseDto> CreateAsync(
        UnidadMedidaRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _unidadRepository.GetBySimboloAsync(dto.Simbolo.Trim(), empresaId);
        if (existente is not null)
        {
            throw new ConflictException(
                $"Ya existe una unidad con el símbolo '{dto.Simbolo}' en esta empresa.");
        }

        var entidad = MapToEntity(dto, empresaId);
        var id = await _unidadRepository.CreateAsync(entidad);

        await _auditoria.RegistrarAsync(
            "unidades_medida", AccionAuditoria.CREATE, empresaId, null,
            "UnidadMedida", id, new { simbolo = entidad.Simbolo, tipo = entidad.Tipo });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("unidades_medida", id);
    }

    /// <summary>Actualiza la unidad de medida (símbolo único excluyéndose a sí misma).</summary>
    public async Task<UnidadMedidaResponseDto> UpdateAsync(
        Guid id, UnidadMedidaRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _unidadRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("unidades_medida", id);

        var porSimbolo = await _unidadRepository.GetBySimboloAsync(dto.Simbolo.Trim(), empresaId);
        if (porSimbolo is not null && porSimbolo.Id != id)
        {
            throw new ConflictException(
                $"Ya existe una unidad con el símbolo '{dto.Simbolo}' en esta empresa.");
        }

        var entidad = MapToEntity(dto, empresaId);
        entidad.Id = id;
        entidad.FechaCreacion = existente.FechaCreacion;

        var ok = await _unidadRepository.UpdateAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("unidades_medida", id);
        }

        await _auditoria.RegistrarAsync(
            "unidades_medida", AccionAuditoria.UPDATE, empresaId, null,
            "UnidadMedida", id, new { simbolo = entidad.Simbolo });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("unidades_medida", id);
    }

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var unidad = await _unidadRepository.GetByIdAsync(id, empresaId)
                     ?? throw new NotFoundException("unidades_medida", id);

        var ok = await _unidadRepository.DeactivateAsync(id, empresaId);

        await _auditoria.RegistrarAsync(
            "unidades_medida", AccionAuditoria.DEACTIVATE, empresaId, null,
            "UnidadMedida", id, new { simbolo = unidad.Simbolo });

        return ok;
    }

    /// <summary>
    /// Simulador de conversión (CA-05): valor × (factor_desde / factor_hacia)
    /// con 4 decimales. Ambos símbolos deben pertenecer al mismo tipo.
    /// </summary>
    public async Task<decimal> ConvertirAsync(
        decimal valor, string simboloDesde, string simboloHacia, Guid empresaId)
    {
        var desde = await _unidadRepository.GetBySimboloAsync(simboloDesde.Trim(), empresaId);
        if (desde is null)
        {
            throw new NotFoundException("unidades_medida", simboloDesde);
        }

        var hacia = await _unidadRepository.GetBySimboloAsync(simboloHacia.Trim(), empresaId);
        if (hacia is null)
        {
            throw new NotFoundException("unidades_medida", simboloHacia);
        }

        if (!string.Equals(desde.Tipo, hacia.Tipo, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "No se puede convertir entre unidades de tipos distintos.",
                "UNIDAD_TIPOS_DISTINTOS");
        }

        var resultado = valor * (desde.FactorConversion / hacia.FactorConversion);
        return Math.Round(resultado, 4, MidpointRounding.AwayFromZero);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(UnidadMedidaRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private static UnidadMedida MapToEntity(UnidadMedidaRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Simbolo = dto.Simbolo.Trim(),
        Tipo = dto.Tipo,
        FactorConversion = dto.FactorConversion,
        UnidadBase = dto.UnidadBase,
        Activo = true
    };

    private static UnidadMedidaResponseDto MapToResponse(UnidadMedida u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Simbolo = u.Simbolo,
        Tipo = u.Tipo,
        TipoLabel = TipoLabel(u.Tipo),
        FactorConversion = u.FactorConversion,
        UnidadBase = u.UnidadBase,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };

    private static string TipoLabel(string tipo) => tipo switch
    {
        TipoMedida.Peso => "Peso",
        TipoMedida.Volumen => "Volumen",
        TipoMedida.Longitud => "Longitud",
        TipoMedida.Temperatura => "Temperatura",
        _ => tipo
    };
}