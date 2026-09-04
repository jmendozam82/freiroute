using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Suscripcion;

/// <summary>
/// Datos de entrada para crear una suscripción nueva (HU-011).
/// Solo el SUPER_ADMIN puede crear suscripciones.
/// </summary>
[SwaggerSchema(Description = "DTO para crear una nueva suscripción de tenant")]
public class SuscripcionRequestDto
{
    [SwaggerSchema(Description = "ID de la empresa (tenant) a suscribir", Nullable = false)]
    public Guid EmpresaId { get; set; }

    [SwaggerSchema(Description = "ID del plan a contratar", Nullable = false)]
    public Guid PlanId { get; set; }

    [SwaggerSchema(Description = "Ciclo de facturación: MENSUAL o ANUAL")]
    public string TipoCiclo { get; set; } = "MENSUAL";

    [SwaggerSchema(Description = "Precio negociado al contratar (puede diferir del precio actual del plan)", Nullable = false)]
    public decimal PrecioPactado { get; set; }

    [SwaggerSchema(Description = "Moneda del precio pactado")]
    public string MonedaPactada { get; set; } = "USD";
}
