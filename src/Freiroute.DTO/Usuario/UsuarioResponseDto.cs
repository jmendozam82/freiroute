namespace Freiroute.DTO.Usuario;

/// <summary>
/// Datos de salida de un usuario. Nunca expone campos sensibles:
/// supabase_user_id, intentos_fallidos, bloqueado_hasta ni EmpresaId.
/// </summary>
public class UsuarioResponseDto
{
    public Guid Id { get; set; }
    public Guid PerfilId { get; set; }
    public string? PerfilNombre { get; set; }            // Label del perfil (join) para la UI
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public string TipoUsuario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;    // PENDING | ACTIVE | SUSPENDED | LOCKED
    public DateTime? UltimoAcceso { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}