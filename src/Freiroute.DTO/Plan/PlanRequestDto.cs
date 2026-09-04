using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Plan;

/// <summary>
/// Datos de entrada para crear o actualizar un plan de suscripción (HU-010).
/// Solo el SUPER_ADMIN puede gestionar planes.
/// </summary>
[SwaggerSchema(Description = "DTO para crear o actualizar un plan de suscripción del SaaS")]
public class PlanRequestDto
{
    [SwaggerSchema(Description = "Nombre visible del plan", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código único del plan: STARTER, PROFESSIONAL, ENTERPRISE", Nullable = false)]
    public string Codigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción detallada del plan")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Límite de usuarios (-1 = ilimitado)")]
    public int LimiteUsuarios { get; set; }

    [SwaggerSchema(Description = "Límite de embarques por mes (-1 = ilimitado)")]
    public int LimiteEmbarquesMes { get; set; }

    [SwaggerSchema(Description = "Límite de almacenamiento en GB")]
    public int LimiteStorageGb { get; set; }

    [SwaggerSchema(Description = "Precio mensual del plan")]
    public decimal PrecioMensual { get; set; }

    [SwaggerSchema(Description = "Precio anual del plan")]
    public decimal PrecioAnual { get; set; }

    [SwaggerSchema(Description = "Moneda del precio (ISO 4217)")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Códigos de módulos disponibles para este plan")]
    public List<string> ModulosDisponibles { get; set; } = [];

    [SwaggerSchema(Description = "Si es true, el plan es visible en el portal de signup público")]
    public bool EsPublico { get; set; } = true;
}
