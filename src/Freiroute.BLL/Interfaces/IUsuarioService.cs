using Freiroute.DTO.Usuario;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de usuarios por tenant (HU-003, HU-004).
/// Todo método recibe empresaId extraído del JWT — nunca del body del request.
/// </summary>
public interface IUsuarioService
{
    /// <summary>Obtiene los usuarios de la empresa. Por defecto solo los activos.</summary>
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(Guid empresaId, bool incluirInactivos = false);

    /// <summary>Obtiene un usuario por Id dentro de la empresa.</summary>
    Task<UsuarioResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene un usuario activo por email dentro de la empresa (login).</summary>
    Task<UsuarioResponseDto?> GetByEmailAsync(string email, Guid empresaId);

    /// <summary>Crea un usuario nuevo en estado PENDING (debe activar/aceptar invitación).</summary>
    Task<UsuarioResponseDto> CreateAsync(UsuarioRequestDto dto, Guid empresaId);

    /// <summary>Actualiza un usuario activo de la empresa.</summary>
    Task<UsuarioResponseDto> UpdateAsync(Guid id, UsuarioRequestDto dto, Guid empresaId);

    /// <summary>Actualiza solo la URL de la foto de perfil.</summary>
    Task UpdateFotoAsync(Guid id, Guid empresaId, string fotoUrl);

    /// <summary>Soft delete de un usuario. Nunca elimina físicamente.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Reactiva un usuario previamente desactivado (HU-013 CA-07). Verifica el límite
    /// de usuarios del plan (CA-08) antes de reactivar y retorna el DTO actualizado.
    /// </summary>
    Task<UsuarioResponseDto> ReactivarAsync(Guid id, Guid empresaId, Guid reactivadoPorId);

    /// <summary>Invita a un usuario por email con token de expiración de 48 horas.</summary>
    Task InvitarAsync(InvitacionRequestDto dto, Guid empresaId, Guid creadoPorId);

    /// <summary>Acepta una invitación: valida token, asigna el perfil y activa el usuario.</summary>
    Task<UsuarioResponseDto> AceptarInvitacionAsync(string token, string nuevaPassword);

    /// <summary>
    /// Restablece la contraseña de un usuario desde el panel de Administración
    /// (G-06 del Sprint 3): actualiza la contraseña en Supabase Auth con el token
    /// de admin, revoca todas las sesiones activas del usuario y registra auditoría.
    /// </summary>
    Task ResetPasswordAdminAsync(Guid usuarioId, Guid empresaId, Guid adminId);
}