using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de salida del dashboard financiero del Super Admin (HU-011).
/// Métricas de facturación y salud financiera de la plataforma.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta del dashboard financiero de administración")]
public class DashboardFinancieroResponseDto
{
    [SwaggerSchema(Description = "Monthly Recurring Revenue — ingreso mensual recurrente")]
    public decimal Mrr { get; set; }

    [SwaggerSchema(Description = "Annual Recurring Revenue — ingreso anual recurrente")]
    public decimal Arr { get; set; }

    [SwaggerSchema(Description = "Número de suscripciones canceladas este mes (churn)")]
    public int ChurnMes { get; set; }

    [SwaggerSchema(Description = "Número de suscripciones nuevas este mes")]
    public int NuevosMes { get; set; }

    [SwaggerSchema(Description = "Ingresos totales registrados este mes")]
    public decimal IngresosMes { get; set; }

    [SwaggerSchema(Description = "Ingresos totales acumulados en el año")]
    public decimal IngresosAño { get; set; }

    [SwaggerSchema(Description = "Número de pagos pendientes de confirmar")]
    public int PagosPendientes { get; set; }
}
