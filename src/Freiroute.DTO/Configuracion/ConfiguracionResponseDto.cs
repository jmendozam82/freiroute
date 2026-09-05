using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Configuracion;

/// <summary>
/// Datos de salida de la configuración general del tenant (HU-014).
/// Se lee desde la tabla 'empresas' (subset de campos).
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de la configuración general del tenant")]
public class ConfiguracionResponseDto
{
    [SwaggerSchema(Description = "ID de la empresa (tenant)")]
    public Guid EmpresaId { get; set; }

    [SwaggerSchema(Description = "Nombre legal de la empresa")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "RUC o NIT de la empresa")]
    public string? RucNit { get; set; }

    [SwaggerSchema(Description = "Dirección fiscal de la empresa")]
    public string? Direccion { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "Industria o giro de la empresa")]
    public string? Industria { get; set; }

    [SwaggerSchema(Description = "Sitio web de la empresa")]
    public string? SitioWeb { get; set; }

    [SwaggerSchema(Description = "URL del logo — signed URL temporal de Supabase Storage")]
    public string? LogoUrl { get; set; }

    [SwaggerSchema(Description = "Color primario del tema (HEX)")]
    public string ColorPrimario { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Color secundario del tema (HEX)")]
    public string ColorSecundario { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Moneda principal (ISO 4217)")]
    public string Moneda { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Zona horaria (IANA)")]
    public string ZonaHoraria { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Formato de fecha")]
    public string FormatoFecha { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Email remitente de las notificaciones del sistema")]
    public string? EmailRemitente { get; set; }

    [SwaggerSchema(Description = "Nombre del remitente de las notificaciones")]
    public string? NombreRemitente { get; set; }

    [SwaggerSchema(Description = "Modos de transporte activos (FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL)")]
    public List<string> ModosTransporteActivos { get; set; } = [];

    [SwaggerSchema(Description = "Si el tenant completó el wizard de onboarding")]
    public bool OnboardingCompletado { get; set; }
}
