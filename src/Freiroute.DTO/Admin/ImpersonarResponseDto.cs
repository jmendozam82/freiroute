using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Admin;

/// <summary>
/// Datos de salida de una impersonación de tenant (HU-009 CA-05).
/// El JWT de impersonación incluye el claim "impersonado_por" para trazabilidad.
/// </summary>
[SwaggerSchema(Description = "DTO de respuesta de impersonación de tenant por Super Admin")]
public class ImpersonarResponseDto
{
    [SwaggerSchema(Description = "JWT de acceso para impersonación del tenant", Nullable = false)]
    public string AccessToken { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre de la empresa impersonada")]
    public string EmpresaNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre del Super Admin que realizó la impersonación")]
    public string AdminNombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Tiempo de expiración del token en segundos")]
    public int ExpiraEn { get; set; }
}
