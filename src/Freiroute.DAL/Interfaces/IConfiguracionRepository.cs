using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a la configuración del tenant (HU-014).
/// NO tiene tabla propia — lee y actualiza un subset de campos de la tabla 'empresas'
/// (configuración general, identidad visual, logo, numeración y email remitente).
/// </summary>
public interface IConfiguracionRepository
{
    /// <summary>
    /// Obtiene la configuración del tenant. Devuelve la entidad Empresa completa
    /// (la BLL proyecta solo los campos de configuración relevantes).
    /// </summary>
    Task<Empresa?> GetConfiguracionAsync(Guid empresaId);

    /// <summary>
    /// Actualiza los campos de configuración general de la empresa.
    /// Incluye datos de identidad, identidad visual, operativa y email remitente.
    /// </summary>
    Task<bool> UpdateConfiguracionAsync(Guid empresaId,
        string nombre, string? rucNit, string? direccion,
        string? telefono, string? industria, string? sitioWeb,
        string colorPrimario, string colorSecundario,
        string moneda, string zonaHoraria, string formatoFecha,
        string? emailRemitente, string? nombreRemitente);

    /// <summary>Actualiza solo el campo logo_url de la empresa.</summary>
    Task<bool> UpdateLogoUrlAsync(Guid empresaId, string? logoUrl);

    /// <summary>
    /// Actualiza los prefijos de numeración (embarque, orden, carta de porte).
    /// Los consecutivos no se editan aquí — son autoincrementales (HU-014 CA-05).
    /// </summary>
    Task<bool> UpdateNumeracionAsync(Guid empresaId,
        string prefijoEmbarque, string prefijoOrden,
        string prefijoCartaPorte);
}
