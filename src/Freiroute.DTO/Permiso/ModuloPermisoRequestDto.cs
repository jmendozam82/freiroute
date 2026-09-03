namespace Freiroute.DTO.Permiso;

/// <summary>
/// Permiso granular de un solo módulo del TMS con sus 3 niveles.
/// La ausencia de un módulo en la lista enviada por el cliente implica
/// desactivar sus permisos existentes (soft delete).
/// </summary>
public class ModuloPermisoRequestDto
{
    public string Modulo { get; set; } = string.Empty;   // Ej: "embarques", "carriers" — ver Constants.ModuloPermiso
    public bool PuedeLeer { get; set; }                  // READ
    public bool PuedeCrear { get; set; }                 // CREATE
    public bool PuedeActualizar { get; set; }            // UPDATE
}