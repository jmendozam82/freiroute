using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Cliente;

/// <summary>
/// Datos de salida de un cliente con sus contactos (HU-019).
/// Los labels se calculan en la BLL.
/// </summary>
[SwaggerSchema(Description = "Respuesta de un cliente con sus contactos")]
public class ClienteResponseDto
{
    [SwaggerSchema(Description = "ID único del cliente")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre o razón social")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Nombre comercial")]
    public string? NombreComercial { get; set; }

    [SwaggerSchema(Description = "RUC o NIT")]
    public string? RucNit { get; set; }

    [SwaggerSchema(Description = "Tipo de cliente: REGULAR, VIP, OCASIONAL, CORPORATIVO, GOBIERNO")]
    public string TipoCliente { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del tipo (calculado)")]
    public string TipoClienteLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Industria o giro")]
    public string? Industria { get; set; }

    [SwaggerSchema(Description = "Email de contacto principal")]
    public string? Email { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto principal")]
    public string? Telefono { get; set; }

    [SwaggerSchema(Description = "Dirección fiscal")]
    public string? DireccionFiscal { get; set; }

    [SwaggerSchema(Description = "País")]
    public string Pais { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Ciudad")]
    public string? Ciudad { get; set; }

    [SwaggerSchema(Description = "Ubicación de despacho por defecto")]
    public Guid? UbicacionDefectoId { get; set; }

    [SwaggerSchema(Description = "Nombre de la ubicación de despacho (calculado)")]
    public string? UbicacionDefectoNombre { get; set; }

    [SwaggerSchema(Description = "Días de crédito (0 = contado)")]
    public int CreditoDias { get; set; }

    [SwaggerSchema(Description = "Límite de crédito")]
    public decimal LimiteCredito { get; set; }

    [SwaggerSchema(Description = "Moneda de facturación")]
    public string Moneda { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Estado de crédito: AL_DIA, EN_MORA, BLOQUEADO, SIN_CREDITO")]
    public string EstadoCredito { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del estado de crédito (calculado)")]
    public string EstadoCreditoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Días máximos de entrega pactados (SLA)")]
    public int? SlaDiasEntrega { get; set; }

    [SwaggerSchema(Description = "Contactos del cliente")]
    public List<ContactoClienteResponseDto> Contactos { get; set; } = [];

    [SwaggerSchema(Description = "Si el cliente está activo (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}