using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Configuracion;

/// <summary>
/// Datos de entrada para actualizar la configuración general del tenant (HU-014).
/// </summary>
[SwaggerSchema(Description = "DTO para actualizar la configuración general del tenant")]
public class ConfiguracionRequestDto
{
    [SwaggerSchema(Description = "Nombre legal de la empresa", Nullable = false)]
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

[SwaggerSchema(Description = "Color primario del tema (HEX)")]
public string ColorPrimario { get; set; } = "#1A73E8";

[SwaggerSchema(Description = "Color secundario del tema (HEX)")]
public string ColorSecundario { get; set; } = "#0B2545";

[SwaggerSchema(Description = "Moneda principal (ISO 4217)")]
public string Moneda { get; set; } = "USD";

[SwaggerSchema(Description = "Zona horaria (IANA)")]
public string ZonaHoraria { get; set; } = "America/Managua";

[SwaggerSchema(Description = "Formato de fecha")]
public string FormatoFecha { get; set; } = "DD/MM/YYYY";

    [SwaggerSchema(Description = "Email remitente de las notificaciones del sistema")]
    public string? EmailRemitente { get; set; }

    [SwaggerSchema(Description = "Nombre del remitente de las notificaciones")]
    public string? NombreRemitente { get; set; }
}
