using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Configuracion;

/// <summary>
/// Datos de salida de la numeración de documentos del tenant (HU-014 CA-05).
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de la numeración de documentos")]
public class NumeracionResponseDto
{
    [SwaggerSchema(Description = "Prefijo de numeración de embarques (ej: FR)")]
    public string PrefijoEmbarque { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Consecutivo actual de embarques")]
    public int ConsecutivoEmbarque { get; set; }

    [SwaggerSchema(Description = "Prefijo de numeración de órdenes (ej: ORD)")]
    public string PrefijoOrden { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Consecutivo actual de órdenes")]
    public int ConsecutivoOrden { get; set; }

    [SwaggerSchema(Description = "Prefijo de numeración de cartas de porte")]
    public string PrefijoCartaPorte { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Consecutivo actual de cartas de porte")]
    public int ConsecutivoCartaPorte { get; set; }
}
