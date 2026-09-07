using Freiroute.Utility.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Cliente;

/// <summary>
/// Datos de entrada de un contacto de cliente (HU-019).
/// </summary>
[SwaggerSchema(Description = "Datos de un contacto de cliente")]
public class ContactoClienteRequestDto
{
    [SwaggerSchema(Description = "Nombre completo del contacto", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Cargo en la empresa del cliente")]
    public string? Cargo { get; set; }

    [SwaggerSchema(Description = "Rol: GENERAL, LOGISTICA, COMPRAS, FINANZAS, RECEPCION, GERENCIA")]
    public string Rol { get; set; } = RolContacto.General;

    [SwaggerSchema(Description = "Email del contacto")]
    public string? Email { get; set; }

    [SwaggerSchema(Description = "Teléfono del contacto")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "true si es el contacto principal (recibe notificaciones)")]
    public bool EsPrincipal { get; set; } = false;
}