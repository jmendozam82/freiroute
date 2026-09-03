using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Perfil;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de perfiles/roles del tenant (HU-006).
/// La auditoría SIEMPRE usa la empresa del propio tenant (tenantId) en lugar
/// de la del token (los perfiles de empresa se gestionan a nivel tenant).
/// Los perfiles del sistema (es_sistema=true) NO se pueden desactivar (CA-03).
/// </summary>
public class PerfilService : IPerfilService
{
    private readonly IPerfilRepository _perfilRepository;
    private readonly IValidator<PerfilRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<PerfilService> _logger;

    public PerfilService(
        IPerfilRepository perfilRepository,
        IValidator<PerfilRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<PerfilService> logger)
    {
        _perfilRepository = perfilRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene un perfil por Id dentro de la empresa.</summary>
    public async Task<PerfilResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var perfil = await _perfilRepository.GetByIdAsync(id, empresaId);
        if (perfil is null)
        {
            return null;
        }

        var usuarios = await _perfilRepository.CountUsuariosAsync(id, empresaId);
        return MapToResponseDto(perfil, usuarios);
    }

    /// <summary>Crea un perfil personalizado (CUSTOM — los base ya existen por tenant).</summary>
    public async Task<PerfilResponseDto> CreateAsync(PerfilRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var perfilId = await _perfilRepository.CreateAsync(new Perfil
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoPerfil = string.IsNullOrWhiteSpace(dto.TipoPerfil)
                ? TipoPerfil.CUSTOM
                : dto.TipoPerfil,
            EsSistema = false,
            Activo = true
        });

        // HU-006 CA-06: registrar la acción en auditoría (perfiles de empresa).
        await _auditoria.RegistrarAsync(
            "perfiles", AccionAuditoria.CREATE, empresaId, null,
            nameof(Perfil), perfilId,
            new { nombre = dto.Nombre, tipoPerfil = dto.TipoPerfil });

        var created = await _perfilRepository.GetByIdAsync(perfilId, empresaId);
        return MapToResponseDto(created!, 0);
    }

    /// <summary>Obtiene todos los perfiles del tenant con su conteo de usuarios (HU-006 GET).</summary>
    public async Task<IEnumerable<PerfilResponseDto>> GetAllAsync(Guid empresaId)
    {
        var perfiles = await _perfilRepository.GetAllAsync(empresaId);

        var result = new List<PerfilResponseDto>();
        foreach (var perfil in perfiles)
        {
            var usuarios = await _perfilRepository.CountUsuariosAsync(perfil.Id, empresaId);
            result.Add(MapToResponseDto(perfil, usuarios));
        }

        return result;
    }

    /// <summary>Actualiza los datos de un perfil del tenant (name/description/tipo).</summary>
    public async Task<PerfilResponseDto> UpdateAsync(Guid id, PerfilRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var existente = await _perfilRepository.GetByIdAsync(id, empresaId);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Perfil), id);
        }

        var perfil = new Perfil
        {
            Id = id,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoPerfil = string.IsNullOrWhiteSpace(dto.TipoPerfil)
                ? TipoPerfil.CUSTOM
                : dto.TipoPerfil
        };

        var ok = await _perfilRepository.UpdateAsync(perfil);
        if (!ok)
        {
            throw new NotFoundException(nameof(Perfil), id);
        }

        await _auditoria.RegistrarAsync(
            "perfiles", AccionAuditoria.UPDATE, empresaId, null,
            nameof(Perfil), id, new { nombre = dto.Nombre });

        var updated = await _perfilRepository.GetByIdAsync(id, empresaId);
        var usuarios = await _perfilRepository.CountUsuariosAsync(id, empresaId);
        return MapToResponseDto(updated!, usuarios);
    }

    /// <summary>
    /// Soft delete de un perfil (HU-006). Los perfiles del sistema (es_sistema)
    /// NO se pueden desactivar (CA-02/03 → BusinessException 400).
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid empresaId, Guid id)
    {
        var existente = await _perfilRepository.GetByIdAsync(id, empresaId);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Perfil), id);
        }

        if (existente.EsSistema)
        {
            throw new BusinessException("No se puede desactivar un perfil del sistema.");
        }

        var ok = await _perfilRepository.DeactivateAsync(id, empresaId);
        if (!ok)
        {
            throw new NotFoundException(nameof(Perfil), id);
        }

        await _auditoria.RegistrarAsync(
            "perfiles", AccionAuditoria.DEACTIVATE, empresaId, null,
            nameof(Perfil), id, new { nombre = existente.Nombre });

        return true;
    }

    private static PerfilResponseDto MapToResponseDto(Perfil p, int usuariosAsignados) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        TipoPerfil = p.TipoPerfil,
        EsSistema = p.EsSistema,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        UsuariosAsignados = usuariosAsignados
    };
}