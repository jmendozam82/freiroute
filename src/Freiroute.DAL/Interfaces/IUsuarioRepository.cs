using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'usuarios'.
/// Todo método filtra por empresaId (capa 1 de aislamiento multi-tenant, ver ADR-003).
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Obtiene los usuarios activos de una empresa.</summary>
    Task<IEnumerable<Usuario>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un usuario activo por Id dentro de la empresa.</summary>
    Task<Usuario?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene un usuario por Id dentro de la empresa SIN filtrar por activo
    /// (Hu-013 CA-07): permite reactivar un usuario previamente desactivado.
    /// </summary>
    Task<Usuario?> GetByIdIncluyendoInactivosAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene un usuario activo por email dentro de la empresa (login, HU-003).</summary>
    Task<Usuario?> GetByEmailAsync(string email, Guid empresaId);

    /// <summary>
    /// Obtiene un usuario por email SIN filtrar por empresa (HU-003).
    /// EXCEPCIÓN ADR-003 deliberada, igual que GetBySupabaseUserIdAsync: se usa
    /// durante el login para resolver el tenant ANTES de autenticar al usuario.
    /// El email de un usuario es único dentro de su empresa (UNIQUE(email, empresa_id)),
    /// así que el email del login determina la empresa.
    /// Implementado: 20260902 — ver UsuarioRepository.GetByEmailGlobalAsync.
    /// </summary>
    Task<Usuario?> GetByEmailGlobalAsync(string email);

    /// <summary>Obtiene un usuario por su vínculo con Supabase Auth (OAuth / SSO, HU-004).</summary>
    Task<Usuario?> GetBySupabaseUserIdAsync(Guid supabaseUserId);

    /// <summary>Insertar un usuario. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Usuario usuario);

    /// <summary>
    /// Actualiza un usuario activo de la empresa. Incluye actualizaciones de
    /// seguridad de cuenta: ultimo_acceso, intentos_fallidos, bloqueado_hasta, estado.
    /// </summary>
    Task<bool> UpdateAsync(Usuario usuario);

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Reactiva un usuario previamente desactivado: SET activo = true,
    /// Estado = 'ACTIVE', intentos_fallidos = 0 (HU-009 / HU-011 CA-03).
    /// Retorna true si el usuario exist�a (aunque ya estuviera activo).
    /// </summary>
    Task<bool> ReactivarAsync(Guid id, Guid empresaId);

    // ── Seguridad de cuenta (HU-003 CA-04/05/06) ──────────────────

    /// <summary>Actualiza solo el campo ultimo_acceso = NOW() tras un login exitoso.</summary>
    Task ActualizarUltimoAccesoAsync(Guid id);

    /// <summary>Incrementa intentos_fallidos en 1 tras un login fallido.</summary>
    Task IncrementarIntentosFallidosAsync(Guid id);

    /// <summary>Bloquea la cuenta: SET bloqueado_hasta = @BloqueadoHasta (NOW() + 30 min tras 5 intentos).</summary>
    Task BloquearHastaAsync(Guid id, DateTime bloqueadoHasta);

    /// <summary>Resetea intentos_fallidos a 0 tras un login exitoso (además de ultimo_acceso).</summary>
    Task ResetearIntentosFallidosAsync(Guid id);
}