using Swashbuckle.AspNetCore.Annotations;
using Freiroute.DTO.Usuario;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de salida del estado actual del wizard de onboarding (HU-012).
/// Devuelve el paso actual, el porcentaje completado y datos guardados para pre-llenar.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta del estado del wizard de onboarding")]
public class OnboardingEstadoResponseDto
{
    [SwaggerSchema(Description = "Paso actual del wizard (1-5). 5 = completado.")]
    public int PasoActual { get; set; }

    [SwaggerSchema(Description = "Porcentaje completado del wizard (0-100)")]
    public int PorcentajeCompletado { get; set; }

    [SwaggerSchema(Description = "Si el onboarding está completado")]
    public bool Completado { get; set; }

    [SwaggerSchema(Description = "Datos del paso 1 (empresa) guardados — para pre-llenar si se retoma")]
    public object? DatosPaso1 { get; set; }

    [SwaggerSchema(Description = "Datos del paso 3 (operativo) guardados — para pre-llenar si se retoma")]
    public object? DatosPaso3 { get; set; }
}
