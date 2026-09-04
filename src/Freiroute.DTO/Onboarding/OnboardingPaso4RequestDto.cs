using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de entrada del Paso 4 del onboarding: primer administrador (HU-012 CA-05).
/// </summary>
[SwaggerSchema(Description = "DTO del Paso 4 del onboarding — primer administrador")]
public class OnboardingPaso4RequestDto
{
    [SwaggerSchema(Description = "Nombre completo del administrador", Nullable = false)]
    public string NombreCompleto { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Teléfono de contacto del administrador")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "Si el administrador desea cambiar su contraseña")]
    public bool CambiarPassword { get; set; }

    [SwaggerSchema(Description = "Nueva contraseña — requerida si CambiarPassword = true")]
    public string? NuevoPassword { get; set; }
}
