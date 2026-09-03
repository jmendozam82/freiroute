namespace Freiroute.BLL.Settings;

/// <summary>
/// Opciones generales de la aplicación (sección "App" del appsettings.json).
/// BaseUrl se usa para construir los links de email (activación de invitación,
/// reset de contraseña) — HU-001 CA-03, HU-003, HU-007.
/// </summary>
public class AppSettings
{
    /// <summary>URL base pública de la aplicación (ej: https://localhost:5001).</summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";
}