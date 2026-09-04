using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de entrada del Paso 2 del onboarding: identidad visual (HU-012 CA-03).
/// El logo se sube por separado (upload) — aquí solo se guarda la URL generada.
/// </summary>
[SwaggerSchema(Description = "DTO del Paso 2 del onboarding — identidad visual")]
public class OnboardingPaso2RequestDto
{
    [SwaggerSchema(Description = "Color primario del tema (formato HEX, ej: #1A73E8)", Nullable = false)]
    public string ColorPrimario { get; set; } = "#1A73E8";

    [SwaggerSchema(Description = "Color secundario del tema (formato HEX, ej: #0B2545)", Nullable = false)]
    public string ColorSecundario { get; set; } = "#0B2545";

    [SwaggerSchema(Description = "URL del logo — la genera el upload por separado")]
    public string? LogoUrl { get; set; }
}
