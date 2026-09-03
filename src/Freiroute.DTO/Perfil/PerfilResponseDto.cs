namespace Freiroute.DTO.Perfil;

/// <summary>
/// Datos de salida de un perfil. Nunca expone el EmpresaId
/// (ya está filtrado por RLS y el tenant proviene del JWT).
/// </summary>
public class PerfilResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoPerfil { get; set; } = string.Empty;
    public bool EsSistema { get; set; }

    /// <summary>Conteo de usuarios activos asignados a este perfil (HU-006 — requerido por el servicio PerfilService.GetAllAsync).</summary>
    public int UsuariosAsignados { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}