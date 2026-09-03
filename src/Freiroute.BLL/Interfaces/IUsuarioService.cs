using Freiroute.DTO.Usuario;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de usuarios por tenant (HU-003, HU-004).
/// Todo método recibe empresaId extraído del JWT — nunca del body del request.
/// </summary>
public interface IUsuarioService
{
    /// <summary>Obtiene los usuarios activos de la empresa.</summary>
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene un usuario por Id dentro de la empresa.</summary>
    Task<UsuarioResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene un usuario activo por email dentro de la empresa (login).</summary>
    Task<UsuarioResponseDto?> GetByEmailAsync(string email, Guid empresaId);

    /// <summary>Crea un usuario nuevo en estado PENDING (debe activar/aceptar invitación).</summary>
    Task<UsuarioResponseDto> CreateAsync(UsuarioRequestDto dto, Guid empresaId);

    /// <summary>Actualiza un usuario activo de la empresa.</summary>
    Task<UsuarioResponseDto> UpdateAsync(Guid id, UsuarioRequestDto dto, Guid empresaId);

    /// <summary>Soft delete de un usuario. Nunca elimina físicamente.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>Invita a un usuario por email con token de expiración de 48 horas.</summary>
    Task InvitarAsync(InvitacionRequestDto dto, Guid empresaId, Guid creadoPorId);

    /// <summary>Acepta una invitación: valida token, asigna el perfil y activa el usuario.</summary>
    Task<UsuarioResponseDto> AceptarInvitacionAsync(string token, string nuevaPassword);
}