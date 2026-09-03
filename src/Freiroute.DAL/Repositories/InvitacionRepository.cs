using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'invitaciones' (append-only — ciclo de vida por estado).
/// La tabla existe desde la migración 20260101000006_tabla_invitaciones.sql
/// (@IngenieroDatos). Este repositorio fue agregado por @BackendDev en Fase 3
/// para cubrir los flujos de invitación (HU-003) y recuperación de contraseña
/// (HU-007) que requieren CRUD sobre invitaciones.
/// </summary>
public class InvitacionRepository : IInvitacionRepository
{
    private readonly IDbConnection _connection;

    public InvitacionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Inserta una invitación. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Invitacion invitacion)
    {
        const string sql = @"
            INSERT INTO invitaciones (
                empresa_id,
                email,
                perfil_id,
                token,
                estado,
                fecha_expiracion,
                fecha_aceptacion,
                creado_por_id
            ) VALUES (
                @EmpresaId,
                @Email,
                @PerfilId,
                @Token,
                @Estado,
                @FechaExpiracion,
                @FechaAceptacion,
                @CreadoPorId
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, invitacion);
    }

    /// <summary>Obtiene una invitación por su token (token UNIQUE, un solo uso).</summary>
    public async Task<Invitacion?> GetByTokenAsync(string token)
    {
        const string sql = @"
            SELECT
                id               AS Id,
                empresa_id       AS EmpresaId,
                email            AS Email,
                perfil_id        AS PerfilId,
                token            AS Token,
                estado           AS Estado,
                fecha_expiracion AS FechaExpiracion,
                fecha_aceptacion AS FechaAceptacion,
                creado_por_id    AS CreadoPorId,
                fecha_creacion   AS FechaCreacion
            FROM invitaciones
            WHERE token = @Token
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Invitacion>(
            sql, new { Token = token });
    }

    /// <summary>
    /// Marca una invitación como aceptada: estado = 'ACCEPTED',
    /// fecha_aceptacion = @FechaAceptacion (token de un solo uso, HU-007 CA-04).
    /// </summary>
    public async Task<bool> MarcarAceptadaAsync(Guid id, DateTime fechaAceptacion)
    {
        const string sql = @"
            UPDATE invitaciones
            SET estado          = 'ACCEPTED',
                fecha_aceptacion = @FechaAceptacion
            WHERE id = @Id
              AND estado = 'PENDING'";

        var rows = await _connection.ExecuteAsync(
            sql, new { Id = id, FechaAceptacion = fechaAceptacion });

        return rows > 0;
    }

    /// <summary>Marca una invitación como expirada: estado = 'EXPIRED'.</summary>
    public async Task<bool> MarcarExpiradaAsync(Guid id)
    {
        const string sql = @"
            UPDATE invitaciones
            SET estado = 'EXPIRED'
            WHERE id = @Id
              AND estado = 'PENDING'";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}