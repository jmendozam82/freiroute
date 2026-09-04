using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'pagos'.
/// ADR-004: INMUTABLE — solo CreateAsync y consultas.
/// NO existe UpdateAsync ni DeactivateAsync — los pagos se registran y nunca se editan.
/// SIN RLS — el Super Admin gestiona todos los pagos.
/// </summary>
public class PagoRepository : IPagoRepository
{
    private readonly IDbConnection _connection;

    public PagoRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene todos los pagos de una suscripción (historial, HU-011 CA-08).</summary>
    public async Task<IEnumerable<Pago>> GetBySuscripcionIdAsync(Guid suscripcionId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                suscripcion_id     AS SuscripcionId,
                monto              AS Monto,
                moneda             AS Moneda,
                metodo_pago        AS MetodoPago,
                referencia         AS Referencia,
                notas              AS Notas,
                estado             AS Estado,
                periodo_desde      AS PeriodoDesde,
                periodo_hasta      AS PeriodoHasta,
                registrado_por_id  AS RegistradoPorId,
                fecha_creacion     AS FechaCreacion
            FROM pagos
            WHERE suscripcion_id = @SuscripcionId
            ORDER BY fecha_creacion DESC";

        return await _connection.QueryAsync<Pago>(sql, new { SuscripcionId = suscripcionId });
    }

    /// <summary>Obtiene los pagos de una empresa (paginado, panel del Super Admin).</summary>
    public async Task<IEnumerable<Pago>> GetByEmpresaIdAsync(Guid empresaId,
        int pageNumber = 1, int pageSize = 20)
    {
        var offset = (pageNumber - 1) * pageSize;

        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                suscripcion_id     AS SuscripcionId,
                monto              AS Monto,
                moneda             AS Moneda,
                metodo_pago        AS MetodoPago,
                referencia         AS Referencia,
                notas              AS Notas,
                estado             AS Estado,
                periodo_desde      AS PeriodoDesde,
                periodo_hasta      AS PeriodoHasta,
                registrado_por_id  AS RegistradoPorId,
                fecha_creacion     AS FechaCreacion
            FROM pagos
            WHERE empresa_id = @EmpresaId
            ORDER BY fecha_creacion DESC
            LIMIT @PageSize OFFSET @Offset";

        return await _connection.QueryAsync<Pago>(sql, new
        {
            EmpresaId = empresaId,
            PageSize = pageSize,
            Offset = offset
        });
    }

    /// <summary>Registra un pago nuevo. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Pago entidad)
    {
        const string sql = @"
            INSERT INTO pagos (
                empresa_id,
                suscripcion_id,
                monto,
                moneda,
                metodo_pago,
                referencia,
                notas,
                estado,
                periodo_desde,
                periodo_hasta,
                registrado_por_id
            ) VALUES (
                @EmpresaId,
                @SuscripcionId,
                @Monto,
                @Moneda,
                @MetodoPago,
                @Referencia,
                @Notas,
                @Estado,
                @PeriodoDesde,
                @PeriodoHasta,
                @RegistradoPorId
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// MRR — Monthly Recurring Revenue.
    /// Suma de los precios pactados de las suscripciones ACTIVE,
    /// normalizados a mensual (si es anual, divide entre 12).
    /// Para el dashboard financiero (HU-011 CA-09).
    /// </summary>
    public async Task<decimal> GetMrrAsync()
    {
        const string sql = @"
            SELECT COALESCE(SUM(
                CASE tipo_ciclo
                    WHEN 'MENSUAL' THEN s.precio_pactado
                    WHEN 'ANUAL'   THEN s.precio_pactado / 12
                END
            ), 0)
            FROM suscripciones s
            WHERE s.estado = 'ACTIVE'
              AND s.activo = true";

        return await _connection.ExecuteScalarAsync<decimal>(sql);
    }

    /// <summary>
    /// Suma de los pagos COMPLETED registrados en el mes/año indicado.
    /// Para el dashboard financiero (HU-011 CA-09).
    /// </summary>
    public async Task<decimal> GetIngresosDelMesAsync(int año, int mes)
    {
        const string sql = @"
            SELECT COALESCE(SUM(monto), 0)
            FROM pagos
            WHERE estado = 'COMPLETED'
              AND EXTRACT(YEAR FROM fecha_creacion) = @Año
              AND EXTRACT(MONTH FROM fecha_creacion) = @Mes";

        return await _connection.ExecuteScalarAsync<decimal>(sql, new { Año = año, Mes = mes });
    }
}
