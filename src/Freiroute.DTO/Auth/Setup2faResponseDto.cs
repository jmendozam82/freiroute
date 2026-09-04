using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de salida de la configuración de 2FA (HU-005 CA-01).
/// El secret y los códigos de recuperación solo se muestran UNA vez.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de la configuración 2FA TOTP — secret y QR para vincular app")]
public class Setup2faResponseDto
{
    [SwaggerSchema(Description = "Secret TOTP en base32 — el usuario lo registra en su app autenticadora")]
    public string Secret { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Data URL del código QR para escanear con la app autenticadora")]
    public string QrCodeUrl { get; set; } = string.Empty;

    [SwaggerSchema(Description = "8 códigos de recuperación de un solo uso (en claro — solo se muestran esta vez)")]
    public List<string> CodigosRecuperacion { get; set; } = [];
}
