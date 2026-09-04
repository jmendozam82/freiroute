using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de entrada del Paso 1 del onboarding: datos de la empresa (HU-012 CA-02).
/// </summary>
[SwaggerSchema(Description = "DTO del Paso 1 del onboarding — datos de la empresa")]
public class OnboardingPaso1RequestDto
{
    [SwaggerSchema(Description = "Nombre legal de la empresa", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "RUC o NIT de la empresa")]
    public string? RucNit { get; set; }

    [SwaggerSchema(Description = "Dirección fiscal de la empresa")]
    public string? Direccion { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto de la empresa")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "Industria o giro de la empresa")]
    public string? Industria { get; set; }
}
