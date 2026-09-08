using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de API Keys para integración externa (HU-023).
/// La clave se almacena como hash bcrypt — nunca en texto plano.
/// El valor crudo (frk_live_{uuid}) se muestra solo una vez al crear.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IApiKeyTenantRepository
{
    /// <summary>Lista todas las API Keys activas de la empresa.</summary>
    Task<IEnumerable<ApiKeyTenant>> GetAllAsync(Guid empresaId);

    /// <summary>Obtiene una API Key por Id dentro de la empresa.</summary>
    Task<ApiKeyTenant?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Obtiene una API Key por su hash. Usado en el middleware de
    /// autenticación para validar requests de la API externa (HU-023).
    /// </summary>
    Task<ApiKeyTenant?> GetByClaveHashAsync(string claveHash);

    /// <summary>Crea la API Key. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateAsync(ApiKeyTenant entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Actualiza la fecha de último uso exitoso de la API Key.
    /// Se llama en cada request autenticado exitoso (HU-023 CA-10).
    /// </summary>
    Task<bool> ActualizarUltimoUsoAsync(Guid id, Guid empresaId);
}
