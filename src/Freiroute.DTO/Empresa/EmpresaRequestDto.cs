namespace Freiroute.DTO.Empresa;

using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// Datos de entrada para registrar un nuevo tenant (HU-001).
/// Solo el SUPER_ADMIN puede crear empresas. El estado del tenant se asigna
/// automáticamente como TRIAL (ADR-004); la numeración de documentos
/// (prefijo/consecutivo) se deriva en BLL.
/// </summary>
public class EmpresaRequestDto
{
    public string Nombre { get; set; } = string.Empty;                 // Obligatorio — prefijo_embarque se deriva de aquí
    public string? RucNit { get; set; }
    public string EmailAdmin { get; set; } = string.Empty;             // Obligatorio — UNIQUE global
    public string? Telefono { get; set; }
    public string Pais { get; set; } = "Nicaragua";
    public string? Ciudad { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public string? ColorPrimario { get; set; }
    public string? ColorSecundario { get; set; }
    public string PlanSuscripcion { get; set; } = "STARTER";           // STARTER | PROFESSIONAL | ENTERPRISE

    /// <summary>
    /// ID del plan de suscripción inicial. Nullable: si no se envía, el
    /// servicio lo resuelve por código (PlanSuscripcion) y defaultea a STARTER.
    /// </summary>
    [SwaggerSchema(Description = "ID del plan de suscripción inicial (opcional — default STARTER)")]
    public Guid? PlanId { get; set; }

    public string? MonedaPrincipal { get; set; }
    public string? ZonaHoraria { get; set; }
    public string? Idioma { get; set; }
    public string? FormatoFecha { get; set; }
}