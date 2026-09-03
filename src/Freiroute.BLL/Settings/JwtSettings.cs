namespace Freiroute.BLL.Settings;

/// <summary>
/// Opciones de generación/validación de JWT (sección "Jwt" del appsettings.json).
/// NOTA: el proyecto usa la clave "Key"/"ExpiryHours" (config existente de Fase 1
/// en Program.cs y JwtTestHelper) en lugar de "Secret"/"ExpirationHours" del spec —
/// se mantiene consistencia con la infraestructura ya desplegada.
/// </summary>
public class JwtSettings
{
    /// <summary>Clave simétrica de firma (mínimo 32 caracteres).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Emisor del token: "freiroute-api".</summary>
    public string Issuer { get; set; } = "freiroute-api";

    /// <summary>Audiencia del token: "freiroute-client".</summary>
    public string Audience { get; set; } = "freiroute-client";

    /// <summary>Horas de validez del access token (HU-003 CA-02: 8 h).</summary>
    public int ExpiryHours { get; set; } = 8;

    /// <summary>Días de validez del refresh token (HU-003 CA-02: 30 días).</summary>
    public int RefreshExpirationDays { get; set; } = 30;
}