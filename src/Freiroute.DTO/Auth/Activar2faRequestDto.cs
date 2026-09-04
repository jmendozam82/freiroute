using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para activar 2FA con el primer código válido (HU-005 CA-01).
/// </summary>
[SwaggerSchema(Description = "DTO para activar 2FA con verificación del primer código")]
public class Activar2faRequestDto
{
    [SwaggerSchema(Description = "Tipo de 2FA a activar: 'TOTP' o 'EMAIL'", Nullable = false)]
    public string Tipo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código de verificación (el primer código válido generado)", Nullable = false)]
    public string Codigo { get; set; } = string.Empty;
}
