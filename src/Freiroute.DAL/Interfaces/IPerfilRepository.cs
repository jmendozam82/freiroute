using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'perfiles'.
/// Todo método filtra por empresaId (capa 1 de aislamiento multi-tenant, ver ADR-003).
/// </summary>
public interface IPerfilRepository
{
    /// <summary>Obtiene los perfiles activos de una empresa.</summary>
    Task<IEnumerable<Perfil>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un perfil activo por Id dentro de la empresa.</summary>
    Task<Perfil?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene el perfil base de una empresa por tipo (ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE).
    /// Se usa para asignar el perfil por defecto al crear usuarios (HU-001, HU-003).
    /// </summary>
    Task<Perfil?> GetByTipoAsync(string tipoPerfil, Guid empresaId);

    /// <summary>Insertar un perfil. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Perfil perfil);

    /// <summary>
    /// Cuenta los usuarios activos asignados a un perfil de la empresa
    /// (HU-006 — "UsuariosAsignados" del PerfilResponseDto / lista de perfiles).
    /// </summary>
    Task<int> CountUsuariosAsync(Guid perfilId, Guid empresaId);

    /// <summary>Actualiza un perfil activo de la empresa.</summary>
    Task<bool> UpdateAsync(Perfil perfil);

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
}