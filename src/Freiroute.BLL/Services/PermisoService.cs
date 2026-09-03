using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Permiso;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de permisos por perfil (HU-006, ADR-009).
/// ReemplazarPermisosAsync es una operación transaccional de reemplazo total
/// (DELETE físico + reinsert — ver repositorio): verifica que el perfil exista
/// y que NO sea SUPER_ADMIN (sus permisos son inmutables por seguridad).
/// Flag activo por módulo: lee/crea/actualiza (no existe DELETE en RRHH — CA-02).
/// </summary>
public class PermisoService : IPermisoService
{
    private readonly IPermisoRepository _permisoRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IValidator<PermisoRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<PermisoService> _logger;

    public PermisoService(
        IPermisoRepository permisoRepository,
        IPerfilRepository perfilRepository,
        IValidator<PermisoRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<PermisoService> logger)
    {
        _permisoRepository = permisoRepository;
        _perfilRepository = perfilRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene los permisos activos de un perfil del tenant (GET /api/perfiles/{id}/permisos).</summary>
    public async Task<IEnumerable<PermisoResponseDto>> GetByPerfilAsync(Guid perfilId, Guid empresaId)
    {
        await ValidarPerfilAsync(empresaId, perfilId);

        var permisos = await _permisoRepository.GetByPerfilAsync(perfilId, empresaId);
        return permisos.Select(MapToResponseDto);
    }

    /// <summary>
    /// Reemplaza TODO el set de permisos de un perfil (HU-006 CA-05).
    /// Devuelve true si el reemplazo se completó (PUT /api/perfiles/{id}/permisos).
    /// El SUPER_ADMIN (adicionalmente a ser es_sistema) está blindado por
    /// IdsSistema.PerfilSuperAdminId.
    /// </summary>
    public async Task<bool> ReemplazarPermisosAsync(Guid perfilId, PermisoRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await ValidarPerfilAsync(empresaId, perfilId);

        // Blindaje de seguridad: el SUPER_ADMIN tiene acceso total inmutable.
        if (perfilId == IdsSistema.PerfilSuperAdminId)
        {
            throw new BusinessException("No se pueden modificar los permisos del Super Admin.");
        }

        // Mapear el request (módulos + flags) a entidades para la transacción DAL
        // (DELETE físico + reinsert del set completo — ver repositorio).
        var entidades = dto.Modulos.Select(m => new Permiso
        {
            EmpresaId = empresaId,
            PerfilId = perfilId,
            Modulo = m.Modulo,
            PuedeLeer = m.PuedeLeer,
            PuedeCrear = m.PuedeCrear,
            PuedeActualizar = m.PuedeActualizar,
            Activo = true
        });

        await _permisoRepository.ReemplazarPermisosAsync(perfilId, entidades, empresaId);

        // HU-006 CA-06: registrar los cambios (todos los módulos en Detalles).
        await _auditoria.RegistrarAsync(
            "permisos", AccionAuditoria.UPDATE, empresaId, null,
            nameof(Permiso), perfilId,
            new { modulos = dto.Modulos.Select(m => m.Modulo) });

        return true;
    }

    /// <summary>Valida que el perfil exista y esté activo en el tenant.</summary>
    private async Task ValidarPerfilAsync(Guid empresaId, Guid perfilId)
    {
        var perfil = await _perfilRepository.GetByIdAsync(perfilId, empresaId);
        if (perfil is null || !perfil.Activo)
        {
            throw new NotFoundException(nameof(Perfil), perfilId);
        }
    }

    private static PermisoResponseDto MapToResponseDto(Permiso p) => new()
    {
        Id = p.Id,
        PerfilId = p.PerfilId,
        Modulo = p.Modulo,
        PuedeLeer = p.PuedeLeer,
        PuedeCrear = p.PuedeCrear,
        PuedeActualizar = p.PuedeActualizar,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion
    };
}