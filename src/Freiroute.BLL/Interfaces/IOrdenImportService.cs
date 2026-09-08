using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio para importación CSV de órdenes (HU-022).
/// Patrón fail-soft (ADR-017): filas válidas se crean como DRAFT con
/// origen_creacion = 'CSV', filas inválidas se reportan en el resultado.
/// Todo método recibe empresaId extraído del JWT (ADR-003).
/// </summary>
public interface IOrdenImportService
{
    /// <summary>
    /// Importa órdenes desde un stream CSV. Retorna el resultado con
    /// contadores y detalle de errores por fila (HU-022 CA-03 a CA-06).
    /// El CSV debe tener las columnas obligatorias definidas en CA-02.
    /// Importación de 500 filas completa en &lt; 10 segundos (CA-08).
    /// </summary>
    Task<ImportacionOrdenResultDto> ImportarCsvAsync(
        Stream csvStream, string nombreArchivo,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Retorna el HTML/CSS del template de importación CSV con headers
    /// y 3 filas de ejemplo (HU-022 CA-01).
    /// </summary>
    Task<string> ObtenerPlantillaCsvAsync();

    /// <summary>
    /// Retorna el historial de importaciones del tenant (HU-022 CA-07).
    /// </summary>
    Task<IEnumerable<ImportacionOrdenResultDto>> GetHistorialImportacionesAsync(
        Guid empresaId);
}
