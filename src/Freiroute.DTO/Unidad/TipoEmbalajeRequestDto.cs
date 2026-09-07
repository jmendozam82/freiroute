using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Unidad;

/// <summary>
/// Datos de entrada para crear o actualizar un tipo de embalaje (HU-018).
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar un tipo de embalaje")]
public class TipoEmbalajeRequestDto
{
    [SwaggerSchema(Description = "Nombre del embalaje (ej: Pallet estándar)", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único (PLT, CAJA, TAM, CTN, GRA, BOB)", Nullable = false)]
    public string Codigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Capacidad máxima en kg")]
    public decimal? CapacidadKg { get; set; }

    [SwaggerSchema(Description = "Capacidad máxima en m³")]
    public decimal? CapacidadM3 { get; set; }

    [SwaggerSchema(Description = "Si el embalaje es apilable")]
    public bool Apilable { get; set; } = true;
}