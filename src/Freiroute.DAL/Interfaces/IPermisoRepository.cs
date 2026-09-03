using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'permisos' (permisos granulares por perfil y módulo).
/// Solo READ/CREATE/UPDATE — no existe DELETE. Todo método filtra por empresaId (ADR-003).
/// </summary>
public interface IPermisoRepository
{
    /// <summary>Obtiene los permisos activos de una empresa.</summary>
    Task<IEnumerable<Permiso>> GetAllAsync(Guid empresaId);

    /// <summary>
    /// Obtiene los permisos activos de un perfil (GET /api/perfiles/{id}/permisos).
    /// También se usa para construir los claims "modulo:accion" del JWT.
    /// </summary>
    Task<IEnumerable<Permiso>> GetByPerfilAsync(Guid perfilId, Guid empresaId);

    /// <summary>Obtiene un permiso activo por Id dentro de la empresa.</summary>
    Task<Permiso?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Insertar un permiso. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Permiso permiso);

    /// <summary>Actualiza los flags de un permiso activo.</summary>
    Task<bool> UpdateAsync(Permiso permiso);

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Reemplaza en una transacción el set completo de permisos de un perfil:
    /// upsert de los permisos recibidos + desactiva los que ya no están en la lista.
    /// Operación usada por PUT /api/perfiles/{id}/permisos (HU-006).
    /// </summary>
    Task<bool> ReemplazarPermisosAsync(Guid perfilId, IEnumerable<Permiso> permisos, Guid empresaId);
}