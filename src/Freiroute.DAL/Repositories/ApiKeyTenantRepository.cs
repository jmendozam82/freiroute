using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de API Keys de tenant para integración REST externa (HU-023).
/// Seguridad por diseño:
///   · GetAllAsync NUNCA selecciona clave_hash — el hash no se expone a la API.
///   · GetByClaveHashAsync NO filtra por empresa_id: el tenant se desconoce
///     hasta validar la key (flujo de autenticación del middleware HU-023).
///   · El valor crudo (frk_live_{uuid}) solo se muestra al momento de crear.
/// ADR-003: resto de métodos filtran por empresa_id. No existe DeleteAsync (ADR-005).
/// </summary>
public class ApiKeyTenantRepository : IApiKeyTenantRepository
{
    private readonly IDbConnection _connection;

    public ApiKeyTenantRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista las API Keys activas de la empresa para la UI de gestión.
    /// ⚠️ NO selecciona clave_hash — nunca exponer el hash a la API.
    /// </summary>
    public async Task<IEnumerable<ApiKeyTenant>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                nombre              AS Nombre,
                activo              AS Activo,
                ultimo_uso          AS UltimoUso,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM api_keys_tenant
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY fecha_creacion DESC";

        return await _connection.QueryAsync<ApiKeyTenant>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene una API Key completa (INCLUYE clave_hash). Uso interno del BLL:
    /// verificación de la key recibida en el request contra el hash almacenado.
    /// </summary>
    public async Task<ApiKeyTenant?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                nombre              AS Nombre,
                clave_hash          AS ClaveHash,
                activo              AS Activo,
                ultimo_uso          AS UltimoUso,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM api_keys_tenant
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<ApiKeyTenant>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Busca una API Key por su hash bcrypt. ⚠️ SIN filtro de empresa_id —
    /// el tenant se desconoce hasta validar la key (el request externo solo
    /// trae el valor crudo). Usado por el middleware de autenticación (HU-023).
    /// </summary>
    public async Task<ApiKeyTenant?> GetByClaveHashAsync(string claveHash)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                nombre              AS Nombre,
                clave_hash          AS ClaveHash,
                activo              AS Activo,
                ultimo_uso          AS UltimoUso,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM api_keys_tenant
            WHERE clave_hash = @ClaveHash
              AND activo = true
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<ApiKeyTenant>(
            sql, new { ClaveHash = claveHash });
    }

    /// <summary>Insertar API Key (solo el hash bcrypt). El UUID lo genera la BD.</summary>
    public async Task<Guid> CreateAsync(ApiKeyTenant entidad)
    {
        const string sql = @"
            INSERT INTO api_keys_tenant (
                empresa_id,
                nombre,
                clave_hash,
                activo
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @ClaveHash,
                @Activo
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Soft delete: SET activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE api_keys_tenant
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Actualiza el timestamp de último uso exitoso (HU-023 CA-10).
    /// Se llama en cada request autenticado por esta key. El trigger
    /// refresh fecha_modificacion automáticamente.
    /// </summary>
    public async Task<bool> ActualizarUltimoUsoAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE api_keys_tenant
            SET ultimo_uso = now(),
                fecha_modificacion = now()
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }
}