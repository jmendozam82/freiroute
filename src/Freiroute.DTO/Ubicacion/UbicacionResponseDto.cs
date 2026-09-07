using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Ubicacion;

/// <summary>
/// Datos de salida de una ubicación (HU-015). Nunca expone la Entity
/// directamente — proyección controlada (AGENTS.md regla 20).
/// </summary>
[SwaggerSchema(Description = "Respuesta de una ubicación del catálogo")]
public class UbicacionResponseDto
{
    [SwaggerSchema(Description = "ID único de la ubicación")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre de la ubicación")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Tipo: ALMACEN, CLIENTE, PUERTO, AEROPUERTO, TERMINAL, CRUCE_FRONTERA, PUNTO_RECARGA, OTRO")]
    public string Tipo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Etiqueta legible del tipo")]
    public string TipoLabel { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Dirección textual")]
    public string? Direccion { get; set; }

    [SwaggerSchema(Description = "País")]
    public string Pais { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Departamento / estado / provincia")]
    public string? Departamento { get; set; }

    [SwaggerSchema(Description = "Ciudad")]
    public string? Ciudad { get; set; }

    [SwaggerSchema(Description = "Latitud (-90 a 90) — null si no georeferenciada")]
    public double? Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud (-180 a 180) — null si no georeferenciada")]
    public double? Longitud { get; set; }

    [SwaggerSchema(Description = "true si las coordenadas fueron validadas vía geocodificación")]
    public bool Georeferenciada { get; set; }

    [SwaggerSchema(Description = "Dirección normalizada por el proveedor de geocodificación")]
    public string? DireccionNormalizada { get; set; }

    [SwaggerSchema(Description = "Nombre de la persona de contacto en sitio")]
    public string? ContactoNombre { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto en sitio")]
    public string? ContactoTelefono { get; set; }

    [SwaggerSchema(Description = "Tiempo estimado de carga/descarga en minutos")]
    public int TiempoServicioMin { get; set; }

    [SwaggerSchema(Description = "Si la ubicación está activa (soft delete)")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}