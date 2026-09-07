using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Mercancia;

/// <summary>
/// Datos de salida de un tipo de mercancía (HU-017).
/// EsHazmat se calcula en la BLL: true si ClasePeligrosidad no es null.
/// </summary>
[SwaggerSchema(Description = "Respuesta de un tipo de mercancía")]
public class TipoMercanciaResponseDto
{
    [SwaggerSchema(Description = "ID único del tipo de mercancía")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la mercancía")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Descripción")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Categoría comercial")]
    public string? Categoria { get; set; }

    [SwaggerSchema(Description = "Clase HAZMAT ONU: 1-9 con subclases")]
    public string? ClasePeligrosidad { get; set; }

    [SwaggerSchema(Description = "Código ONU")]
    public string? CodigoOnu { get; set; }

    [SwaggerSchema(Description = "Código del Sistema Armonizado (HS)")]
    public string? CodigoHs { get; set; }

    [SwaggerSchema(Description = "Peso máximo por unidad en kg")]
    public decimal? PesoMaximoKg { get; set; }

    [SwaggerSchema(Description = "Volumen máximo por unidad en m³")]
    public decimal? VolumenMaximoM3 { get; set; }

    [SwaggerSchema(Description = "Temperatura mínima °C")]
    public decimal? TemperaturaMinC { get; set; }

    [SwaggerSchema(Description = "Temperatura máxima °C")]
    public decimal? TemperaturaMaxC { get; set; }

    [SwaggerSchema(Description = "Requiere cadena de frío")]
    public bool RequiereRefrigeracion { get; set; }

    [SwaggerSchema(Description = "Mercancía frágil")]
    public bool EsFragil { get; set; }

    [SwaggerSchema(Description = "Mercancía peligrosa (HAZMAT)")]
    public bool EsPeligroso { get; set; }

    [SwaggerSchema(Description = "Mercancía perecedera")]
    public bool EsPerecedero { get; set; }

    [SwaggerSchema(Description = "Carga sobredimensionada")]
    public bool EsSobredimensionado { get; set; }

    [SwaggerSchema(Description = "Requiere fumigación")]
    public bool RequiereFumigacion { get; set; }

    // ── Labels calculados (BLL) ────────────────────────────────
    [SwaggerSchema(Description = "true si ClasePeligrosidad no es null (calculado)")]
    public bool EsHazmat { get; set; }

    [SwaggerSchema(Description = "Si el tipo está activo (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}