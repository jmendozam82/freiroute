using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Plan;

/// <summary>
/// Datos de salida de un plan de suscripción (HU-010).
/// Incluye el conteo de empresas activas suscritas a este plan.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de un plan de suscripción")]
public class PlanResponseDto
{
    [SwaggerSchema(Description = "ID único del plan")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre visible del plan")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código único del plan")]
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

    [SwaggerSchema(Description = "Moneda del precio")]
    public string Moneda { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Códigos de módulos disponibles para este plan")]
    public string[] ModulosDisponibles { get; set; } = [];

    [SwaggerSchema(Description = "Si es true, el plan es visible en el portal de signup público")]
    public bool EsPublico { get; set; }

    [SwaggerSchema(Description = "Número de empresas activas suscritas a este plan")]
    public int EmpresasSuscritas { get; set; }

    [SwaggerSchema(Description = "Si el plan está activo")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación del plan")]
    public DateTime FechaCreacion { get; set; }
}
