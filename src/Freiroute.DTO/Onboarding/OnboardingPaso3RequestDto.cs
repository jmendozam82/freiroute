using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de entrada del Paso 3 del onboarding: configuración operativa (HU-012 CA-04).
/// </summary>
[SwaggerSchema(Description = "DTO del Paso 3 del onboarding — configuración operativa")]
public class OnboardingPaso3RequestDto
{
    [SwaggerSchema(Description = "Moneda principal de la empresa (ISO 4217)")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Zona horaria de la empresa (IANA)")]
    public string ZonaHoraria { get; set; } = "America/Managua";

    [SwaggerSchema(Description = "Formato de fecha (DD/MM/YYYY, MM/DD/YYYY)")]
    public string FormatoFecha { get; set; } = "DD/MM/YYYY";

    [SwaggerSchema(Description = "Códigos de modos de transporte activos (FTL, LTL, AEREO, MARITIMO, etc.)")]
    public List<string> ModosTransporteActivos { get; set; } = [];

    [SwaggerSchema(Description = "Prefijo de numeración de embarques (ej: FR)")]
    public string PrefijoEmbarque { get; set; } = "FR";

    [SwaggerSchema(Description = "Prefijo de numeración de órdenes (ej: ORD)")]
    public string PrefijoOrden { get; set; } = "ORD";
}
