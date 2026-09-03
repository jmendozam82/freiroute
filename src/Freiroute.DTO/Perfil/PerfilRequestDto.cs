namespace Freiroute.DTO.Perfil;

/// <summary>
/// Datos de entrada para crear o actualizar un perfil.
/// El campo EsSistema no se recibe del cliente: lo asigna el sistema
/// únicamente para los perfiles base del tenant (HU-001, HU-006).
/// </summary>
public class PerfilRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoPerfil { get; set; } = "CUSTOM";  // SUPER_ADMIN|ADMIN|DISPATCHER|OPERADOR|CONDUCTOR|CLIENTE|CUSTOM
}