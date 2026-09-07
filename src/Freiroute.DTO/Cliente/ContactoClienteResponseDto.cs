using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Cliente;

/// <summary>
/// Datos de salida de un contacto de cliente (HU-019).
/// RolLabel se calcula en la BLL.
/// </summary>
[SwaggerSchema(Description = "Respuesta de un contacto de cliente")]
public class ContactoClienteResponseDto
{
    [SwaggerSchema(Description = "ID único del contacto")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre completo del contacto")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Cargo en la empresa del cliente")]
    public string? Cargo { get; set; }

    [SwaggerSchema(Description = "Rol: GENERAL, LOGISTICA, COMPRAS, FINANZAS, RECEPCION, GERENCIA")]
    public string Rol { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del rol (calculado)")]
    public string RolLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Email del contacto")]
    public string? Email { get; set; }

    [SwaggerSchema(Description = "Teléfono del contacto")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "true si es el contacto principal")]
    public bool EsPrincipal { get; set; }

    [SwaggerSchema(Description = "Si el contacto está activo (soft delete)")]
    public bool Activo { get; set; }
}