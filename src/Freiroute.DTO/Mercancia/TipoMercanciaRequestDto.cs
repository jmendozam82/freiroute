using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Mercancia;

/// <summary>
/// Datos de entrada para crear o actualizar un tipo de mercancía (HU-017).
/// Incluye clasificación HAZMAT (clase ONU 1-9), HS Code y flags de manejo.
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar un tipo de mercancía")]
public class TipoMercanciaRequestDto
{
    [SwaggerSchema(Description = "Nombre de la mercancía", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Descripción")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Categoría comercial (alimentos, construcción, etc.)")]
    public string? Categoria { get; set; }

    [SwaggerSchema(Description = "Clase HAZMAT ONU: 1-9 con subclases (1.1, 2.1...)")]
    public string? ClasePeligrosidad { get; set; }

    [SwaggerSchema(Description = "Código ONU (número de identificación de la sustancia)")]
    public string? CodigoOnu { get; set; }

    [SwaggerSchema(Description = "Código del Sistema Armonizado (HS) para aduanas")]
    public string? CodigoHs { get; set; }

    [SwaggerSchema(Description = "Peso máximo por unidad en kg")]
    public decimal? PesoMaximoKg { get; set; }

    [SwaggerSchema(Description = "Volumen máximo por unidad en m³")]
    public decimal? VolumenMaximoM3 { get; set; }

    [SwaggerSchema(Description = "Temperatura mínima °C (requerida si requiere_refrigeracion)")]
    public decimal? TemperaturaMinC { get; set; }

    [SwaggerSchema(Description = "Temperatura máxima °C (requerida si requiere_refrigeracion)")]
    public decimal? TemperaturaMaxC { get; set; }

    [SwaggerSchema(Description = "Requiere cadena de frío")]
    public bool RequiereRefrigeracion { get; set; } = false;

    [SwaggerSchema(Description = "Mercancía frágil")]
    public bool EsFragil { get; set; } = false;

    [SwaggerSchema(Description = "Mercancía peligrosa (HAZMAT)")]
    public bool EsPeligroso { get; set; } = false;

    [SwaggerSchema(Description = "Mercancía perecedera")]
    public bool EsPerecedero { get; set; } = false;

    [SwaggerSchema(Description = "Carga sobredimensionada")]
    public bool EsSobredimensionado { get; set; } = false;

    [SwaggerSchema(Description = "Requiere fumigación")]
    public bool RequiereFumigacion { get; set; } = false;
}