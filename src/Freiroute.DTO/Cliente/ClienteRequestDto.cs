using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Cliente;

/// <summary>
/// Datos de entrada para crear o actualizar un cliente (HU-019).
/// Incluye contactos que se crean junto con el cliente.
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar un cliente")]
public class ClienteRequestDto
{
    [SwaggerSchema(Description = "Nombre o razón social del cliente", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre comercial")]
    public string? NombreComercial { get; set; }

    [SwaggerSchema(Description = "RUC o NIT — único por empresa")]
    public string? RucNit { get; set; }

    [SwaggerSchema(Description = "Tipo de documento: RUC, NIT, CEDULA, PASAPORTE")]
    public string TipoDocumento { get; set; } = "RUC";

    [SwaggerSchema(Description = "Tipo de cliente: REGULAR, VIP, OCASIONAL, CORPORATIVO, GOBIERNO")]
    public string TipoCliente { get; set; } = Freiroute.Utility.Constants.TipoCliente.Regular;

    [SwaggerSchema(Description = "Industria o giro del cliente")]
    public string? Industria { get; set; }

    [SwaggerSchema(Description = "Email de contacto principal")]
    public string? Email { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto principal")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "Sitio web")]
    public string? SitioWeb { get; set; }

    [SwaggerSchema(Description = "Dirección fiscal")]
    public string? DireccionFiscal { get; set; }

    [SwaggerSchema(Description = "País")]
    public string Pais { get; set; } = "Nicaragua";

    [SwaggerSchema(Description = "Departamento / estado")]
    public string? Departamento { get; set; }

    [SwaggerSchema(Description = "Ciudad")]
    public string? Ciudad { get; set; }

    [SwaggerSchema(Description = "Ubicación de despacho por defecto (de HU-015)")]
    public Guid? UbicacionDefectoId { get; set; }

    [SwaggerSchema(Description = "Días de crédito (0 = contado)")]
    public int CreditoDias { get; set; } = 0;

    [SwaggerSchema(Description = "Límite de crédito")]
    public decimal LimiteCredito { get; set; } = 0m;

    [SwaggerSchema(Description = "Moneda de facturación (ISO 4217)")]
    public string Moneda { get; set; } = "USD";

    [SwaggerSchema(Description = "Días máximos de entrega pactados (SLA) — null si no hay SLA")]
    public int? SlaDiasEntrega { get; set; }

    [SwaggerSchema(Description = "Contactos creados junto con el cliente")]
    public List<ContactoClienteRequestDto> Contactos { get; set; } = [];
}