using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Configuracion;

/// <summary>
/// Datos de entrada para actualizar los prefijos de numeración (HU-014 CA-05).
/// Los consecutivos no se editan directamente — son autoincrementales.
/// </summary>
[SwaggerSchema(Description = "DTO para actualizar los prefijos de numeración de documentos")]
public class NumeracionRequestDto
{
    [SwaggerSchema(Description = "Prefijo de numeración de embarques", Nullable = false)]
    public string PrefijoEmbarque { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Prefijo de numeración de órdenes", Nullable = false)]
    public string PrefijoOrden { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Prefijo de numeración de cartas de porte", Nullable = false)]
    public string PrefijoCartaPorte { get; set; } = string.Empty;
}
