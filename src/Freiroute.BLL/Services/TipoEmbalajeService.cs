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
/// Lógica de negocio del catálogo de tipos de embalaje (HU-018).
/// El código es único por empresa (PLT, CAJA, TAM, ...).
/// </summary>
public class TipoEmbalajeService : ITipoEmbalajeService
{
    private readonly ITipoEmbalajeRepository _embalajeRepository;
    private readonly IValidator<TipoEmbalajeRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<TipoEmbalajeService> _logger;

    public TipoEmbalajeService(
        ITipoEmbalajeRepository embalajeRepository,
        IValidator<TipoEmbalajeRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<TipoEmbalajeService> logger)
    {
        _embalajeRepository = embalajeRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista todos los embalajes activos de la empresa.</summary>
    public async Task<IEnumerable<TipoEmbalajeResponseDto>> GetAllAsync(Guid empresaId)
    {
        var embalajes = await _embalajeRepository.GetAllAsync(empresaId);
        return embalajes.Select(MapToResponse);
    }

    /// <summary>Obtiene un embalaje por Id dentro de la empresa.</summary>
    public async Task<TipoEmbalajeResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var embalaje = await _embalajeRepository.GetByIdAsync(id, empresaId);
        return embalaje is null ? null : MapToResponse(embalaje);
    }

    /// <summary>Crea el tipo de embalaje (código único por empresa).</summary>
    public async Task<TipoEmbalajeResponseDto> CreateAsync(
        TipoEmbalajeRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var todos = await _embalajeRepository.GetAllAsync(empresaId);
        if (todos.Any(e => string.Equals(e.Codigo, dto.Codigo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException(
                $"Ya existe un embalaje con el código '{dto.Codigo}' en esta empresa.");
        }

        var entidad = MapToEntity(dto, empresaId);
        var id = await _embalajeRepository.CreateAsync(entidad);

        await _auditoria.RegistrarAsync(
            "tipos_embalaje", AccionAuditoria.CREATE, empresaId, null,
            "TipoEmbalaje", id, new { codigo = entidad.Codigo });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("tipos_embalaje", id);
    }

    /// <summary>Actualiza el tipo de embalaje (código único excluyéndose a sí mismo).</summary>
    public async Task<TipoEmbalajeResponseDto> UpdateAsync(
        Guid id, TipoEmbalajeRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _embalajeRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("tipos_embalaje", id);

        var todos = await _embalajeRepository.GetAllAsync(empresaId);
        if (todos.Any(e => e.Id != id &&
                           string.Equals(e.Codigo, dto.Codigo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException(
                $"Ya existe un embalaje con el código '{dto.Codigo}' en esta empresa.");
        }

        var entidad = MapToEntity(dto, empresaId);
        entidad.Id = id;
        entidad.FechaCreacion = existente.FechaCreacion;

        var ok = await _embalajeRepository.UpdateAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("tipos_embalaje", id);
        }

        await _auditoria.RegistrarAsync(
            "tipos_embalaje", AccionAuditoria.UPDATE, empresaId, null,
            "TipoEmbalaje", id, new { codigo = entidad.Codigo });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("tipos_embalaje", id);
    }

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var embalaje = await _embalajeRepository.GetByIdAsync(id, empresaId)
                       ?? throw new NotFoundException("tipos_embalaje", id);

        var ok = await _embalajeRepository.DeactivateAsync(id, empresaId);

        await _auditoria.RegistrarAsync(
            "tipos_embalaje", AccionAuditoria.DEACTIVATE, empresaId, null,
            "TipoEmbalaje", id, new { codigo = embalaje.Codigo });

        return ok;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(TipoEmbalajeRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private static TipoEmbalaje MapToEntity(TipoEmbalajeRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Codigo = dto.Codigo.Trim().ToUpperInvariant(),
        Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
        CapacidadKg = dto.CapacidadKg,
        CapacidadM3 = dto.CapacidadM3,
        Apilable = dto.Apilable,
        Activo = true
    };

    private static TipoEmbalajeResponseDto MapToResponse(TipoEmbalaje e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Codigo = e.Codigo,
        Descripcion = e.Descripcion,
        CapacidadKg = e.CapacidadKg,
        CapacidadM3 = e.CapacidadM3,
        Apilable = e.Apilable,
        Activo = e.Activo,
        FechaCreacion = e.FechaCreacion
    };
}