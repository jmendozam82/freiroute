namespace Freiroute.DTO.Usuario;

/// <summary>
/// Datos de entrada para invitar a un usuario por email (HU-003 / invitaciones).
/// El sistema genera un token con expiración de 48 horas.
/// </summary>
public class InvitacionRequestDto
{
    public string Email { get; set; } = string.Empty;
    public Guid PerfilId { get; set; }
}