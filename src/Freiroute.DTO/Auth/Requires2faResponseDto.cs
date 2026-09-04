using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de salida del paso 1 de login cuando el usuario tiene 2FA activo (HU-005).
/// Respuesta HTTP 202 — el cliente debe llamar a POST /api/auth/2fa/verify.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta 202 cuando se requiere 2FA durante el login")]
public class Requires2faResponseDto
{
    [SwaggerSchema(Description = "Siempre false en este contexto (no se fuerza cambio de contraseña aquí)")]
    public bool RequieresCambioPassword { get; set; }

    [SwaggerSchema(Description = "Siempre true — indica que se requiere 2FA")]
    public bool Requires2fa { get; set; } = true;

    [SwaggerSchema(Description = "Token temporal válido por 5 minutos para el paso 2 de 2FA", Nullable = false)]
    public string TempToken { get; set; } = string.Empty;
}
