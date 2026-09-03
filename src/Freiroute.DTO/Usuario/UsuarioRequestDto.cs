namespace Freiroute.DTO.Usuario;

/// <summary>
/// Datos de entrada para crear o actualizar un usuario.
/// No se reciben del cliente: estado (inicia PENDING), supabase_user_id,
/// intentos_fallidos, bloqueado_hasta ni campos de auditoría — el sistema los gestiona.
/// </summary>
public class UsuarioRequestDto
{
    public Guid PerfilId { get; set; }
    public string? TipoIdentidad { get; set; }
    public string? NumeroIdentidad { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public string TipoUsuario { get; set; } = "OPERADOR";  // SUPER_ADMIN|ADMIN|DISPATCHER|OPERADOR|CONDUCTOR|CLIENTE
}