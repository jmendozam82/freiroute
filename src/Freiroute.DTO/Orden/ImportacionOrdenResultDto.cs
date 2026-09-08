using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Resultado de una importación CSV de órdenes (HU-022).
/// Patrón fail-soft (ADR-017): las filas válidas se crean, las inválidas
/// se reportan sin detener el proceso.
/// </summary>
[SwaggerSchema(Description = "Resultado de una importación CSV de órdenes")]
public class ImportacionOrdenResultDto
{
    [SwaggerSchema(Description = "ID del registro de importación")]
    public Guid ImportacionId { get; set; }

    [SwaggerSchema(Description = "Total de filas procesadas del CSV")]
    public int TotalFilas { get; set; }

    [SwaggerSchema(Description = "Filas procesadas exitosamente")]
    public int FilasOk { get; set; }

    [SwaggerSchema(Description = "Filas con errores (no se creó la orden)")]
    public int FilasError { get; set; }

    [SwaggerSchema(Description = "Detalle de errores por fila")]
    public List<ErrorFilaOrdenDto> DetalleErrores { get; set; } = [];
}

/// <summary>
/// Error individual de una fila del CSV importado (HU-022).
/// </summary>
[SwaggerSchema(Description = "Error de una fila específica del CSV importado")]
public class ErrorFilaOrdenDto
{
    [SwaggerSchema(Description = "Número de la fila en el CSV (1-indexed)")]
    public int Fila { get; set; }

    [SwaggerSchema(Description = "Nombre del campo con error")]
    public string Campo { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Descripción del error")]
    public string Error { get; set; } = string.Empty;
}
