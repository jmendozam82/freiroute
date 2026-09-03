using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'sesiones' (refresh tokens — HU-003 CA-02, HU-007 CA-06).
/// Migración: 20260101000006_tabla_sesiones.sql (creada por @IngenieroDatos).
/// </summary>
public class SesionRepository : ISesionRepository
{
    private readonly IDbConnection _connection;

    public SesionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Registra una sesión con el hash del refresh token. El UUID lo genera la BD.</summary>
    public async Task<Guid> CreateAsync(Sesion sesion)
    {
        const string sql = @"
            INSERT INTO sesiones (
                empresa_id,
                usuario_id,
                refresh_token_hash,
                fecha_expiracion,
                activa
            ) VALUES (
                @EmpresaId,
                @UsuarioId,
                @RefreshTokenHash,
                @FechaExpiracion,
                @Activa
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, sesion);
    }

    /// <summary>Obtiene una sesión por el hash del refresh token (hash UNIQUE).</summary>
    public async Task<Sesion?> GetByRefreshTokenHashAsync(string refreshTokenHash)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                usuario_id          AS UsuarioId,
                refresh_token_hash  AS RefreshTokenHash,
                fecha_expiracion    AS FechaExpiracion,
                activa              AS Activa,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM sesiones
            WHERE refresh_token_hash = @RefreshTokenHash
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Sesion>(
            sql, new { RefreshTokenHash = refreshTokenHash });
    }

    /// <summary>Revoca una sesión (activa = false) — logout o rotación de refresh token.</summary>
    public async Task<bool> RevocarAsync(Guid id)
    {
        const string sql = @"
            UPDATE sesiones
            SET activa = false
            WHERE id = @Id
              AND activa = true";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    /// <summary>Revoca todas las sesiones activas de un usuario (HU-007 CA-06).</summary>
    public async Task<bool> RevocarTodasPorUsuarioAsync(Guid usuarioId)
    {
        const string sql = @"
            UPDATE sesiones
            SET activa = false
            WHERE usuario_id = @UsuarioId
              AND activa = true";

        var rows = await _connection.ExecuteAsync(sql, new { UsuarioId = usuarioId });
        return rows > 0;
    }
}