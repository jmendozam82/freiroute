using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio para recepción de órdenes por
/// API REST externa (HU-023). Autenticación via X-Api-Key header.
/// Rate limit: 100 requests/minuto por API Key.
/// Todo el aislamiento de tenant se garantiza por la API Key.
/// </summary>
public interface IOrdenApiExternaService
{
    /// <summary>
    /// Valida una API Key y retorna la empresaId asociada.
    /// Retorna null si la key es inválida o inactiva (HU-023 CA-02, CA-03).
    /// Actualiza ultimo_uso en cada request exitoso (CA-10).
    /// </summary>
    Task<Guid?> ValidarApiKeyAsync(string apiKey);

    /// <summary>
    /// Crea una orden desde el payload de la API externa.
    /// La orden se crea con origen_creacion = 'API' (HU-023 CA-05).
    /// Retorna el número de referencia generado (numero_orden).
    /// </summary>
    Task<OrdenResponseDto> CrearOrdenDesdeApiAsync(
        OrdenRequestDto dto, Guid empresaId);

    /// <summary>
    /// Genera una nueva API Key para el tenant. Retorna el valor
    /// crudo (frk_live_{uuid}) visible una sola vez (HU-023 CA-07).
    /// La clave se almacena como hash bcrypt (CA-08).
    /// </summary>
    Task<ApiKeyResponseDto> GenerarApiKeyAsync(
        ApiKeyOrdenRequestDto dto, Guid empresaId);

    /// <summary>
    /// Lista las API Keys del tenant (sin exponer el hash) (HU-023).
    /// </summary>
    Task<IEnumerable<ApiKeyResponseDto>> GetApiKeysAsync(Guid empresaId);

    /// <summary>
    /// Soft delete de una API Key — la deshabilita inmediatamente
    /// para nuevos requests (HU-023 CA-09).
    /// </summary>
    Task<bool> DesactivarApiKeyAsync(Guid apiKeyId, Guid empresaId);
}
