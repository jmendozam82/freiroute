using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Suscripcion;

/// <summary>
/// Datos de salida de una suscripción (HU-011).
/// Incluye información resuelta de empresa y plan para display.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de una suscripción")]
public class SuscripcionResponseDto
{
    [SwaggerSchema(Description = "ID único de la suscripción")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "ID de la empresa (tenant)")]
    public Guid EmpresaId { get; set; }

    [SwaggerSchema(Description = "Nombre de la empresa")]
    public string EmpresaNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "ID del plan contratado")]
    public Guid PlanId { get; set; }

    [SwaggerSchema(Description = "Nombre del plan contratado")]
    public string PlanNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código del plan contratado")]
    public string PlanCodigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Ciclo de facturación: MENSUAL o ANUAL")]
    public string TipoCiclo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Fecha de inicio de la suscripción")]
    public DateTime FechaInicio { get; set; }

    [SwaggerSchema(Description = "Fecha de vencimiento de la suscripción")]
    public DateTime FechaVencimiento { get; set; }

    [SwaggerSchema(Description = "Estado actual: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado")]
    public string EstadoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Precio negociado al contratar")]
    public decimal PrecioPactado { get; set; }

    [SwaggerSchema(Description = "Moneda del precio pactado")]
    public string MonedaPactada { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Días restantes hasta el vencimiento (-1 si ya venció)")]
    public int DiasParaVencimiento { get; set; }

    [SwaggerSchema(Description = "Si la suscripción está activa")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación de la suscripción")]
    public DateTime FechaCreacion { get; set; }
}
