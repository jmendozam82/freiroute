using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos de entrada para registrar un rechazo de entrega (HU-030 CA-01/CA-02).
/// El motivo es obligatorio y debe ser uno de los valores de TipoRechazo:
/// CLIENTE_AUSENTE | DIRECCION_INCORRECTA | MERCANCIA_DANADA | RECHAZO_CLIENTE | OTRO.
/// </summary>
[SwaggerSchema(Description = "Datos para registrar un rechazo de entrega")]
public class RechazoEntregaRequestDto
{
    [SwaggerSchema(Description = "Motivo del rechazo (obligatorio — ver TipoRechazo)")]
    public string Motivo { get; set; } = null!;

    [SwaggerSchema(Description = "Detalle libre del rechazo (opcional)")]
    public string? Descripcion { get; set; }
}