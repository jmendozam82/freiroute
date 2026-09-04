using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para verificar un código 2FA en el flujo de login (HU-005).
/// </summary>
[SwaggerSchema(Description = "DTO para verificar el código 2FA durante el login")]
public class Verificar2faRequestDto
{
    [SwaggerSchema(Description = "Token temporal emitido en el paso 1 del login (requiere 2FA)", Nullable = false)]
    public string TempToken { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código de 6 dígitos (TOTP o email)", Nullable = false)]
    public string Codigo { get; set; } = string.Empty;
}
