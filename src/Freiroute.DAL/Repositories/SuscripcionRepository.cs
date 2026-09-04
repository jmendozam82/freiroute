using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'suscripciones'.
/// ADR-004: NO tiene RLS — el Super Admin ve TODAS las suscripciones.
/// NO recibe empresaId en GetAll — la gestión es global (panel Super Admin).
/// </summary>
public class SuscripcionRepository : ISuscripcionRepository
{
    private readonly IDbConnection _connection;

    public SuscripcionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Obtiene las suscripciones paginadas, con filtro opcional por estado.
    /// JOIN con empresas y planes para incluir nombres en el resultado.
    /// Usado por el panel del Super Admin (HU-011).
    /// </summary>
    public async Task<IEnumerable<Suscripcion>> GetAllAsync(
        string? estado = null, int pageNumber = 1, int pageSize = 20)
    {
        var offset = (pageNumber - 1) * pageSize;

        const string sql = @"
            SELECT
                s.id                  AS Id,
                s.empresa_id          AS EmpresaId,
                s.plan_id             AS PlanId,
                s.tipo_ciclo          AS TipoCiclo,
                s.fecha_inicio        AS FechaInicio,
                s.fecha_vencimiento   AS FechaVencimiento,
                s.fecha_cancelacion   AS FechaCancelacion,
                s.estado              AS Estado,
                s.precio_pactado      AS PrecioPactado,
                s.moneda_pactada      AS MonedaPactada,
                s.activo              AS Activo,
                s.fecha_creacion      AS FechaCreacion,
                s.fecha_modificacion  AS FechaModificacion,
                s.creado_por_id       AS CreadoPorId
            FROM suscripciones s
            WHERE (@Estado IS NULL OR s.estado = @Estado)
            ORDER BY s.fecha_vencimiento ASC
            LIMIT @PageSize OFFSET @Offset";

        return await _connection.QueryAsync<Suscripcion>(sql, new
        {
            Estado = estado,
            PageSize = pageSize,
            Offset = offset
        });
    }

    /// <summary>Obtiene una suscripción por su Id.</summary>
    public async Task<Suscripcion?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                plan_id             AS PlanId,
                tipo_ciclo          AS TipoCiclo,
                fecha_inicio        AS FechaInicio,
                fecha_vencimiento   AS FechaVencimiento,
                fecha_cancelacion   AS FechaCancelacion,
                estado              AS Estado,
                precio_pactado      AS PrecioPactado,
                moneda_pactada      AS MonedaPactada,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion,
                creado_por_id       AS CreadoPorId
            FROM suscripciones
            WHERE id = @Id";

        return await _connection.QueryFirstOrDefaultAsync<Suscripcion>(sql, new { Id = id });
    }

    /// <summary>
    /// Obtiene la suscripción ACTIVA de una empresa.
    /// Devuelve la que tiene activo = true y estado != CANCELLED.
    /// Clave para validar límites del plan (HU-013 CA-08).
    /// </summary>
    public async Task<Suscripcion?> GetActivaByEmpresaIdAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                plan_id             AS PlanId,
                tipo_ciclo          AS TipoCiclo,
                fecha_inicio        AS FechaInicio,
                fecha_vencimiento   AS FechaVencimiento,
                fecha_cancelacion   AS FechaCancelacion,
                estado              AS Estado,
                precio_pactado      AS PrecioPactado,
                moneda_pactada      AS MonedaPactada,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion,
                creado_por_id       AS CreadoPorId
            FROM suscripciones
            WHERE empresa_id = @EmpresaId
              AND activo = true
              AND estado NOT IN ('CANCELLED')
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Suscripcion>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Inserta una suscripción nueva. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Suscripcion entidad)
    {
        const string sql = @"
            INSERT INTO suscripciones (
                empresa_id,
                plan_id,
                tipo_ciclo,
                fecha_inicio,
                fecha_vencimiento,
                fecha_cancelacion,
                estado,
                precio_pactado,
                moneda_pactada,
                creado_por_id
            ) VALUES (
                @EmpresaId,
                @PlanId,
                @TipoCiclo,
                @FechaInicio,
                @FechaVencimiento,
                @FechaCancelacion,
                @Estado,
                @PrecioPactado,
                @MonedaPactada,
                @CreadoPorId
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza una suscripción (fecha_vencimiento, estado, etc.).</summary>
    public async Task<bool> UpdateAsync(Suscripcion entidad)
    {
        const string sql = @"
            UPDATE suscripciones SET
                plan_id            = @PlanId,
                tipo_ciclo         = @TipoCiclo,
                fecha_vencimiento  = @FechaVencimiento,
                fecha_cancelacion  = @FechaCancelacion,
                estado             = @Estado,
                precio_pactado     = @PrecioPactado,
                moneda_pactada     = @MonedaPactada
            WHERE id = @Id";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>
    /// Suscripciones activas cuyo vencimiento está dentro de @diasUmbral.
    /// Para alertas de vencimiento al Super Admin (HU-011 CA-04).
    /// </summary>
    public async Task<IEnumerable<Suscripcion>> GetProximasAVencerAsync(int diasUmbral)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                plan_id             AS PlanId,
                tipo_ciclo          AS TipoCiclo,
                fecha_inicio        AS FechaInicio,
                fecha_vencimiento   AS FechaVencimiento,
                fecha_cancelacion   AS FechaCancelacion,
                estado              AS Estado,
                precio_pactado      AS PrecioPactado,
                moneda_pactada      AS MonedaPactada,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion,
                creado_por_id       AS CreadoPorId
            FROM suscripciones
            WHERE estado = 'ACTIVE'
              AND fecha_vencimiento BETWEEN NOW()
              AND NOW() + (@DiasUmbral || ' days')::INTERVAL
              AND activo = true";

        return await _connection.QueryAsync<Suscripcion>(sql, new { DiasUmbral = diasUmbral });
    }

    /// <summary>
    /// Suscripciones en PAST_DUE que llevan más de @diasGracia en ese estado.
    /// Para el job que las pasa a SUSPENDED (HU-011 CA-06).
    /// </summary>
    public async Task<IEnumerable<Suscripcion>> GetVencidasEnGraciaAsync(int diasGracia)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                plan_id             AS PlanId,
                tipo_ciclo          AS TipoCiclo,
                fecha_inicio        AS FechaInicio,
                fecha_vencimiento   AS FechaVencimiento,
                fecha_cancelacion   AS FechaCancelacion,
                estado              AS Estado,
                precio_pactado      AS PrecioPactado,
                moneda_pactada      AS MonedaPactada,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion,
                creado_por_id       AS CreadoPorId
            FROM suscripciones
            WHERE estado = 'PAST_DUE'
              AND fecha_vencimiento < NOW() - (@DiasGracia || ' days')::INTERVAL
              AND activo = true";

        return await _connection.QueryAsync<Suscripcion>(sql, new { DiasGracia = diasGracia });
    }
}
