using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de entrada para cambiar el estado de una empresa (HU-009 CA-04).
/// Estados válidos: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED.
/// </summary>
[SwaggerSchema(Description = "DTO para cambiar el estado de una empresa del SaaS")]
public class CambiarEstadoRequestDto
{
    [SwaggerSchema(Description = "Nuevo estado: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED", Nullable = false)]
    public string NuevoEstado { get; set; } = string.Empty;
}
