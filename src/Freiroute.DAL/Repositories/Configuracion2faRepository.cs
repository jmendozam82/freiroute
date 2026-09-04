using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la configuración 2FA y códigos temporales (HU-005).
/// ADR-011: El totp_secret se almacena cifrado con AES-256-GCM.
/// ADR-013: Los códigos expirados se purgan con DELETE físico (autorizado).
/// SIEMPRE filtrar por empresa_id en configuración_2fa (RLS + capa de código).
/// </summary>
public class Configuracion2faRepository : IConfiguracion2faRepository
{
    private readonly IDbConnection _connection;

    public Configuracion2faRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    // ── Configuración 2FA ─────────────────────────────────────────

    /// <summary>
    /// Obtiene la configuración 2FA de un usuario dentro de su empresa.
    /// SIEMPRE filtra por empresa_id + usuario_id.
    /// </summary>
    public async Task<Configuracion2fa?> GetByUsuarioIdAsync(Guid usuarioId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                    AS Id,
                empresa_id            AS EmpresaId,
                usuario_id            AS UsuarioId,
                totp_secret           AS TotpSecret,
                totp_habilitado       AS TotpHabilitado,
                email_habilitado      AS EmailHabilitado,
                codigos_recuperacion  AS CodigosRecuperacion,
                activo                AS Activo,
                fecha_creacion        AS FechaCreacion,
                fecha_modificacion    AS FechaModificacion
            FROM configuracion_2fa
            WHERE usuario_id = @UsuarioId
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Configuracion2fa>(
            sql, new { UsuarioId = usuarioId, EmpresaId = empresaId });
    }

    /// <summary>Inserta la configuración 2FA de un usuario. El UUID lo genera la BD.</summary>
    public async Task<Guid> CreateAsync(Configuracion2fa entidad)
    {
        const string sql = @"
            INSERT INTO configuracion_2fa (
                empresa_id,
                usuario_id,
                totp_secret,
                totp_habilitado,
                email_habilitado,
                codigos_recuperacion
            ) VALUES (
                @EmpresaId,
                @UsuarioId,
                @TotpSecret,
                @TotpHabilitado,
                @EmailHabilitado,
                @CodigosRecuperacion
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Actualiza la configuración 2FA (secret cifrado, flags de habilitación,
    /// códigos de recuperación hasheados).
    /// </summary>
    public async Task<bool> UpdateAsync(Configuracion2fa entidad)
    {
        const string sql = @"
            UPDATE configuracion_2fa SET
                totp_secret           = @TotpSecret,
                totp_habilitado       = @TotpHabilitado,
                email_habilitado      = @EmailHabilitado,
                codigos_recuperacion  = @CodigosRecuperacion
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>
    /// Desactiva el 2FA de un usuario: SET activo = false.
    /// Se ejecuta cuando el usuario desactiva su 2FA tras verificar el código actual.
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid usuarioId, Guid empresaId)
    {
        const string sql = @"
            UPDATE configuracion_2fa
            SET activo = false
            WHERE usuario_id = @UsuarioId
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId
        });
        return rows > 0;
    }

    // ── Códigos temporales ────────────────────────────────────────

    /// <summary>
    /// Crea un código temporal de 2FA enviado por email.
    /// El UUID lo genera la BD.
    /// </summary>
    public async Task<Guid> CrearCodigoTemporalAsync(Codigo2faTempora entidad)
    {
        const string sql = @"
            INSERT INTO codigos_2fa_temporales (
                usuario_id,
                codigo_hash,
                tipo,
                usado,
                fecha_expiracion
            ) VALUES (
                @UsuarioId,
                @CodigoHash,
                @Tipo,
                @Usado,
                @FechaExpiracion
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Obtiene un código temporal válido (no usado, no expirado) por usuario y hash.
    /// Devuelve null si no es válido.
    /// </summary>
    public async Task<Codigo2faTempora?> GetCodigoTemporalValidoAsync(Guid usuarioId, string codigoHash)
    {
        const string sql = @"
            SELECT
                id               AS Id,
                usuario_id       AS UsuarioId,
                codigo_hash      AS CodigoHash,
                tipo             AS Tipo,
                usado            AS Usado,
                fecha_expiracion AS FechaExpiracion,
                fecha_creacion   AS FechaCreacion
            FROM codigos_2fa_temporales
            WHERE usuario_id = @UsuarioId
              AND codigo_hash = @CodigoHash
              AND usado = false
              AND fecha_expiracion > NOW()
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Codigo2faTempora>(
            sql, new { UsuarioId = usuarioId, CodigoHash = codigoHash });
    }

    /// <summary>
    /// Marca un código temporal como usado (un solo uso).
    /// </summary>
    public async Task<bool> MarcarCodigoUsadoAsync(Guid codigoId)
    {
        const string sql = @"
            UPDATE codigos_2fa_temporales
            SET usado = true
            WHERE id = @CodigoId
              AND usado = false";

        var rows = await _connection.ExecuteAsync(sql, new { CodigoId = codigoId });
        return rows > 0;
    }

    /// <summary>
    /// Elimina los códigos temporales vencidos o ya usados.
    /// Llamado por el job de vencimientos (ADR-013).
    ///
    /// IMPORTANTE: Este DELETE físico está AUTORIZADO por ADR-013.
    /// Es el único DELETE permitido fuera de la regla de soft-delete.
    /// La tabla codigos_2fa_temporales no tiene campo 'activo' — se controla
    /// por fecha_expiracion y el flag 'usado'.
    /// </summary>
    public async Task PurgarCodigosExpiradosAsync()
    {
        // DELETE físico autorizado por ADR-013 (background job de vencimientos).
        // La tabla no tiene 'activo' — los códigos se purgan por expiración o uso.
        const string sql = @"
            DELETE FROM codigos_2fa_temporales
            WHERE fecha_expiracion < NOW()
               OR usado = true";

        await _connection.ExecuteAsync(sql);
    }
}
