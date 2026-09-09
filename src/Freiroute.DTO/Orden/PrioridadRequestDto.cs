using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para el cambio manual de prioridad de una orden (HU-029 CA-09).
/// La prioridad debe ser: CRITICO | ALTO | NORMAL | BAJO.
/// </summary>
[SwaggerSchema(Description = "Datos para cambiar la prioridad de una orden")]
public class PrioridadRequestDto
{
    [SwaggerSchema(Description = "Nueva prioridad: CRITICO | ALTO | NORMAL | BAJO")]
    public string Prioridad { get; set; } = null!;

    [SwaggerSchema(Description = "Motivo del cambio (opcional — se registra en auditoría)")]
    public string? Motivo { get; set; }
}