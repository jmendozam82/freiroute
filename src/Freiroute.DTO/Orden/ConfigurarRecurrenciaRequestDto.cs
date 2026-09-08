using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para configurar la recurrencia de una plantilla de orden (HU-027).
/// La fecha de próxima ejecución se calcula según la frecuencia.
/// </summary>
[SwaggerSchema(Description = "Datos para configurar la recurrencia de una plantilla")]
public class ConfigurarRecurrenciaRequestDto
{
    [SwaggerSchema(Description = "true para activar recurrencia, false para desactivar")]
    public bool EsRecurrente { get; set; }

    [SwaggerSchema(Description = "Frecuencia de recurrencia: DIARIA, SEMANAL, QUINCENAL, MENSUAL")]
    public string? FrecuenciaRecurrencia { get; set; }
}
