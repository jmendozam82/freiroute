using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Plan;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de planes de suscripción (HU-010).
/// Catálogo GLOBAL del SaaS — el SUPER_ADMIN gestiona los planes (sin empresaId).
/// No se puede desactivar un plan con empresas activas suscritas (CA-04).
/// </summary>
public class PlanService : IPlanService
{
    private readonly IPlanRepository _planRepository;
    private readonly IValidator<PlanRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<PlanService> _logger;

    public PlanService(
        IPlanRepository planRepository,
        IValidator<PlanRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<PlanService> logger)
    {
        _planRepository = planRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene todos los planes (por defecto solo los activos).</summary>
    public async Task<IEnumerable<PlanResponseDto>> GetAllAsync(bool soloActivos = true)
    {
        var planes = await _planRepository.GetAllAsync(soloActivos);
        var result = new List<PlanResponseDto>();

        foreach (var plan in planes)
        {
            var dto = MapToResponseDto(plan);
            dto.EmpresasSuscritas = await _planRepository.CountEmpresasSuscritasAsync(plan.Id);
            result.Add(dto);
        }

        return result;
    }

    /// <summary>Obtiene un plan por su Id.</summary>
    public async Task<PlanResponseDto?> GetByIdAsync(Guid id)
    {
        var plan = await _planRepository.GetByIdAsync(id);
        return plan is null ? null : MapToResponseDto(plan);
    }

    /// <summary>Crea un plan nuevo (HU-010 CA-01).</summary>
    public async Task<PlanResponseDto> CreateAsync(PlanRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        // Unicidad del código (HU-010: cada plan tiene un código único).
        var existente = await _planRepository.GetByCodigoAsync(dto.Codigo);
        if (existente is not null)
        {
            throw new ConflictException("Ya existe un plan con ese código.");
        }

        var plan = MapToEntity(dto);
        var planId = await _planRepository.CreateAsync(plan);

        await _auditoria.RegistrarAsync(
            "planes", AccionAuditoria.CREATE, IdsSistema.EmpresaRaizId, null,
            nameof(Plan), planId, new { dto.Codigo, dto.Nombre });

        var created = await _planRepository.GetByIdAsync(planId);
        return MapToResponseDto(created!);
    }

    /// <summary>Actualiza los datos de un plan existente (HU-010 CA-02).</summary>
    public async Task<PlanResponseDto> UpdateAsync(Guid id, PlanRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var existente = await _planRepository.GetByIdAsync(id);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Plan), id);
        }

        // Verificar que el código no colisione con otro plan.
        var porCodigo = await _planRepository.GetByCodigoAsync(dto.Codigo);
        if (porCodigo is not null && porCodigo.Id != id)
        {
            throw new ConflictException("Ya existe un plan con ese código.");
        }

        var plan = MapToEntity(dto);
        plan.Id = id;

        var ok = await _planRepository.UpdateAsync(plan);
        if (!ok)
        {
            throw new NotFoundException(nameof(Plan), id);
        }

        await _auditoria.RegistrarAsync(
            "planes", AccionAuditoria.UPDATE, IdsSistema.EmpresaRaizId, null,
            nameof(Plan), id, new { dto.Codigo, dto.Nombre });

        var updated = await _planRepository.GetByIdAsync(id);
        return MapToResponseDto(updated!);
    }

    /// <summary>
    /// Desactiva un plan. Lanza BusinessException si el plan tiene empresas
    /// activas suscritas (HU-010 CA-04).
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        var existente = await _planRepository.GetByIdAsync(id);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Plan), id);
        }

        // CA-04: no se puede desactivar un plan con empresas suscritas activas.
        var empresasSuscritas = await _planRepository.CountEmpresasSuscritasAsync(id);
        if (empresasSuscritas > 0)
        {
            throw new BusinessException(
                "No se puede desactivar el plan porque tiene empresas activas suscritas.");
        }

        var ok = await _planRepository.DeactivateAsync(id);
        if (!ok)
        {
            throw new BusinessException("No se pudo desactivar el plan. Verifique que no tenga empresas suscritas.");
        }

        await _auditoria.RegistrarAsync(
            "planes", AccionAuditoria.DEACTIVATE, IdsSistema.EmpresaRaizId, null,
            nameof(Plan), id, new { existente.Codigo });

        return true;
    }

    private static Plan MapToEntity(PlanRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        Codigo = dto.Codigo,
        Descripcion = dto.Descripcion,
        LimiteUsuarios = dto.LimiteUsuarios,
        LimiteEmbarquesMes = dto.LimiteEmbarquesMes,
        LimiteStorageGb = dto.LimiteStorageGb,
        PrecioMensual = dto.PrecioMensual,
        PrecioAnual = dto.PrecioAnual,
        Moneda = dto.Moneda,
        ModulosDisponibles = dto.ModulosDisponibles.ToArray(),
        EsPublico = dto.EsPublico,
        Activo = true
    };

    private static PlanResponseDto MapToResponseDto(Plan p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Codigo = p.Codigo,
        Descripcion = p.Descripcion,
        LimiteUsuarios = p.LimiteUsuarios,
        LimiteEmbarquesMes = p.LimiteEmbarquesMes,
        LimiteStorageGb = p.LimiteStorageGb,
        PrecioMensual = p.PrecioMensual,
        PrecioAnual = p.PrecioAnual,
        Moneda = p.Moneda,
        ModulosDisponibles = p.ModulosDisponibles,
        EsPublico = p.EsPublico,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion
    };
}
