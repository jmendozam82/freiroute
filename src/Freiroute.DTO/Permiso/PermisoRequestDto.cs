namespace Freiroute.DTO.Permiso;

/// <summary>
/// Datos de entrada para reemplazar todos los permisos de un perfil (HU-006).
/// Recibe el perfil objetivo y la lista de módulos con sus 3 flags.
/// La operación es transaccional: los módulos no incluidos se desactivan
/// (nunca se eliminan físicamente).
/// </summary>
public class PermisoRequestDto
{
    public Guid PerfilId { get; set; }
    public List<ModuloPermisoRequestDto> Modulos { get; set; } = [];
}