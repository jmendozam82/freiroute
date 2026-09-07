using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Unidad;

/// <summary>
/// Datos de salida de un tipo de embalaje (HU-018).
/// </summary>
[SwaggerSchema(Description = "Respuesta de un tipo de embalaje")]
public class TipoEmbalajeResponseDto
{
    [SwaggerSchema(Description = "ID único del embalaje")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre del embalaje")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único (PLT, CAJA, TAM, CTN, GRA, BOB)")]
    public string Codigo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción")]
    public string? Descripcion { get; set; }

    [SwaggerSchema(Description = "Capacidad máxima en kg")]
    public decimal? CapacidadKg { get; set; }

    [SwaggerSchema(Description = "Capacidad máxima en m³")]
    public decimal? CapacidadM3 { get; set; }

    [SwaggerSchema(Description = "Si el embalaje es apilable")]
    public bool Apilable { get; set; }

    [SwaggerSchema(Description = "Si el embalaje está activo (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}