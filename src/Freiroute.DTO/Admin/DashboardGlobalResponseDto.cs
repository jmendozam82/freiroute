using Swashbuckle.AspNetCore.Annotations;
using Freiroute.DTO.Suscripcion;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de salida del dashboard global del Super Admin (HU-009).
/// Métricas agregadas de toda la plataforma SaaS.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta del dashboard global de administración SaaS")]
public class DashboardGlobalResponseDto
{
    [SwaggerSchema(Description = "Total de empresas (tenants) activas en la plataforma")]
    public int TotalEmpresasActivas { get; set; }

    [SwaggerSchema(Description = "Empresas registradas en el mes actual")]
    public int NuevasEstesMes { get; set; }

    [SwaggerSchema(Description = "Monthly Recurring Revenue — ingreso mensual recurrente")]
    public decimal Mrr { get; set; }

    [SwaggerSchema(Description = "Annual Recurring Revenue — ingreso anual recurrente")]
    public decimal Arr { get; set; }

    [SwaggerSchema(Description = "Total de embarques registrados hoy en toda la plataforma")]
    public int TotalEmbarquesHoy { get; set; }

    [SwaggerSchema(Description = "Distribución de empresas por estado (TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED)")]
    public Dictionary<string, int> EmpresasPorEstado { get; set; } = [];

    [SwaggerSchema(Description = "Distribución de empresas por plan (STARTER, PROFESSIONAL, ENTERPRISE)")]
    public Dictionary<string, int> EmpresasPorPlan { get; set; } = [];

    [SwaggerSchema(Description = "Suscripciones próximas a vencer (próximos 15 días)")]
    public List<SuscripcionResponseDto> TenantsPorVencer { get; set; } = [];
}
