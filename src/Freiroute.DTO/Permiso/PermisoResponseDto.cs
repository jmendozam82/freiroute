namespace Freiroute.DTO.Permiso;

/// <summary>
/// Datos de salida de un permiso por perfil y módulo.
/// Nunca expone el EmpresaId (filtrado por RLS / JWT).
/// </summary>
public class PermisoResponseDto
{
    public Guid Id { get; set; }
    public Guid PerfilId { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public bool PuedeLeer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeActualizar { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}