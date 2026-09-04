using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para desactivar el 2FA (HU-005 CA-06).
/// Requiere el código actual (TOTP o email) para confirmar la desactivación.
/// </summary>
[SwaggerSchema(Description = "DTO para desactivar el 2FA — requiere código actual")]
public class Desactivar2faRequestDto
{
    [SwaggerSchema(Description = "Código TOTP o email actual para confirmar la desactivación", Nullable = false)]
    public string Codigo { get; set; } = string.Empty;
}
