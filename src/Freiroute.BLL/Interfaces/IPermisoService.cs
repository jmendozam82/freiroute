using Freiroute.DTO.Permiso;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de permisos por perfil y módulo (HU-006).
/// Solo READ/CREATE/UPDATE — no existe DELETE. Todo método recibe empresaId del JWT.
/// </summary>
public interface IPermisoService
{
    /// <summary>Obtiene los permisos activos de un perfil (GET /api/perfiles/{id}/permisos).</summary>
    Task<IEnumerable<PermisoResponseDto>> GetByPerfilAsync(Guid perfilId, Guid empresaId);

    /// <summary>
    /// Reemplaza el set completo de permisos de un perfil en una transacción
    /// (PUT /api/perfiles/{id}/permisos). Registra la operación en auditoría.
    /// </summary>
    Task<bool> ReemplazarPermisosAsync(Guid perfilId, PermisoRequestDto dto, Guid empresaId);
}