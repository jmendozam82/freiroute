namespace Freiroute.Aplicacion.Areas.Tenant.Models;

public class ProfileViewModel
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public string PerfilNombre { get; set; } = string.Empty;
    public bool TotpHabilitado { get; set; }
}
