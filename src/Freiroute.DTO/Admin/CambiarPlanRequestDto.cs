using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de entrada para cambiar el plan de un tenant (HU-009 CA-04).
/// Registra auditoría con acción CAMBIAR_PLAN.
/// </summary>
[SwaggerSchema(Description = "DTO para cambiar el plan de suscripción de un tenant")]
public class CambiarPlanRequestDto
{
    [SwaggerSchema(Description = "ID del nuevo plan a asignar", Nullable = false)]
    public Guid PlanId { get; set; }

    [SwaggerSchema(Description = "Motivo del cambio de plan (para auditoría)")]
    public string? Motivo { get; set; }
}
