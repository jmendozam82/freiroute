using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de salida de una empresa (tenant) para el panel de Super Admin (HU-009).
/// Incluye datos de identificación, estado, plan y suscripción.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de una empresa del SaaS")]
public class EmpresaResponseDto
{
    [SwaggerSchema(Description = "ID único de la empresa")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la empresa")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Email del administrador")]
    public string EmailAdmin { get; set; } = string.Empty;

    [SwaggerSchema(Description = "RUC/NIT de la empresa")]
    public string? RucNit { get; set; }

    [SwaggerSchema(Description = "Teléfono de la empresa")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "País")]
    public string Pais { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Industria o giro")]
    public string? Industria { get; set; }

    [SwaggerSchema(Description = "Ciudad de la empresa")]
    public string? Ciudad { get; set; }

    [SwaggerSchema(Description = "Estado del tenant: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED")]
    public string Estado { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código del plan de suscripción actual")]
    public string PlanSuscripcion { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Fecha de registro de la empresa")]
    public DateTime FechaCreacion { get; set; }

    [SwaggerSchema(Description = "Si el onboarding fue completado")]
    public bool OnboardingCompletado { get; set; }

    [SwaggerSchema(Description = "Próximo vencimiento de la suscripción activa")]
    public DateTime? ProximoVencimiento { get; set; }

    [SwaggerSchema(Description = "Si la empresa está activa")]
    public bool Activo { get; set; }
}
