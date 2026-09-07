using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de 'tarifas_base' y sus recargos (tabla 'recargos_tarifa',
/// HU-020, ADR-003, ADR-015).
/// ADR-003: todo método filtra por empresaId.
/// VERSIONADO (ADR-015): NO existe UpdateAsync de la tarifa. Al actualizar,
/// la BLL cierra la vigencia de la anterior (CerrarVigenciaAsync) y crea una
/// nueva versión. El UPDATE de la tabla SOLO se usa para cerrar vigencia.
/// GetVigenteAsync es el punto CRÍTICO para el cálculo de costos del
/// Sprint 4: resuelve la tarifa aplicable por zona/modo/servicio y fecha.
/// Los nombres de zonas para los DTOs de respuesta se resuelven en la capa
/// BLL (JOIN con 'zonas_entrega' cuando el DTO lo requiera en Sprint 4).
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class TarifaBaseRepository : ITarifaBaseRepository
{
    // Columnas de 'tarifas_base' mapeadas a la entidad TarifaBase.
    private const string Col = @"
        id                      AS Id,
        empresa_id              AS EmpresaId,
        nombre                  AS Nombre,
        codigo                  AS Codigo,
        zona_origen_id          AS ZonaOrigenId,
        zona_destino_id         AS ZonaDestinoId,
        modo_transporte         AS ModoTransporte,
        tipo_servicio           AS TipoServicio,
        tipo_tarifa             AS TipoTarifa,
        precio_unitario         AS PrecioUnitario,
        precio_minimo           AS PrecioMinimo,
        moneda                  AS Moneda,
        fecha_vigencia_desde    AS FechaVigenciaDesde,
        fecha_vigencia_hasta    AS FechaVigenciaHasta,
        activo                  AS Activo,
        fecha_creacion          AS FechaCreacion,
        fecha_modificacion      AS FechaModificacion";

    // Columnas de 'recargos_tarifa' mapeadas a la entidad RecargoTarifa.
    private const string ColRecargo = @"
        id                  AS Id,
        empresa_id          AS EmpresaId,
        tarifa_id           AS TarifaId,
        codigo_recargo      AS CodigoRecargo,
        nombre              AS Nombre,
        tipo_calculo        AS TipoCalculo,
        valor               AS Valor,
        activo              AS Activo,
        fecha_creacion      AS FechaCreacion,
        fecha_modificacion  AS FechaModificacion";

    private readonly IDbConnection _connection;

    public TarifaBaseRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista tarifas de la empresa con filtros opcionales por zona origen,
    /// zona destino, modo de transporte y soloVigentes (fecha_vigencia_hasta
    /// nula o futura). Por defecto incluye históricas (vigentes y vencidas)
    /// para el historial de precios (HU-020 CA-07).
    /// </summary>
    public async Task<IEnumerable<TarifaBase>> GetAllAsync(
        Guid empresaId, Guid? zonaOrigenId = null, Guid? zonaDestinoId = null,
        string? modo = null, bool? soloVigentes = null)
    {
        var sql = $@"
            SELECT {Col}
            FROM tarifas_base
            WHERE empresa_id = @EmpresaId
              AND activo = true";

        if (zonaOrigenId.HasValue)
        {
            sql += " AND zona_origen_id = @ZonaOrigenId";
        }

        if (zonaDestinoId.HasValue)
        {
            sql += " AND zona_destino_id = @ZonaDestinoId";
        }

        if (!string.IsNullOrWhiteSpace(modo))
        {
            sql += " AND modo_transporte = @Modo";
        }

        if (soloVigentes == true)
        {
            sql += @" AND (
                fecha_vigencia_hasta IS NULL
                OR fecha_vigencia_hasta >= CURRENT_DATE
            )";
        }

        sql += " ORDER BY nombre ASC";

        return await _connection.QueryAsync<TarifaBase>(sql, new
        {
            EmpresaId = empresaId,
            ZonaOrigenId = zonaOrigenId,
            ZonaDestinoId = zonaDestinoId,
            Modo = modo
        });
    }

    /// <summary>Obtiene una tarifa activa por Id dentro de la empresa.</summary>
    public async Task<TarifaBase?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                      AS Id,
                empresa_id              AS EmpresaId,
                nombre                  AS Nombre,
                codigo                  AS Codigo,
                zona_origen_id          AS ZonaOrigenId,
                zona_destino_id         AS ZonaDestinoId,
                modo_transporte         AS ModoTransporte,
                tipo_servicio           AS TipoServicio,
                tipo_tarifa             AS TipoTarifa,
                precio_unitario         AS PrecioUnitario,
                precio_minimo           AS PrecioMinimo,
                moneda                  AS Moneda,
                fecha_vigencia_desde    AS FechaVigenciaDesde,
                fecha_vigencia_hasta    AS FechaVigenciaHasta,
                activo                  AS Activo,
                fecha_creacion          AS FechaCreacion,
                fecha_modificacion      AS FechaModificacion
            FROM tarifas_base
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<TarifaBase>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene la tarifa vigente para la combinación zona-origen/destino,
    /// modo y tipo de servicio en la fecha indicada (ADR-015 — CRÍTICO para
    /// el cálculo de costos del Sprint 4 HU-020 CA-03).
    /// Reglas: activa, fecha_vigencia_desde &lt;= fecha, y
    /// (fecha_vigencia_hasta IS NULL o &gt;= fecha). Una tarifa sin zona
    /// (zona_* = NULL) aplica como genérica a cualquier origen/destino.
    /// Prefiere la tarifa más específica (con zonas definidas) y la más
    /// reciente. LIMIT 1: la BLL retorna la única aplicable.
    /// </summary>
    public async Task<TarifaBase?> GetVigenteAsync(
        Guid empresaId, Guid? zonaOrigenId, Guid? zonaDestinoId,
        string modo, string tipoServicio, DateOnly fecha)
    {
        const string sql = @"
            SELECT
                id                      AS Id,
                empresa_id              AS EmpresaId,
                nombre                  AS Nombre,
                codigo                  AS Codigo,
                zona_origen_id          AS ZonaOrigenId,
                zona_destino_id         AS ZonaDestinoId,
                modo_transporte         AS ModoTransporte,
                tipo_servicio           AS TipoServicio,
                tipo_tarifa             AS TipoTarifa,
                precio_unitario         AS PrecioUnitario,
                precio_minimo           AS PrecioMinimo,
                moneda                  AS Moneda,
                fecha_vigencia_desde    AS FechaVigenciaDesde,
                fecha_vigencia_hasta    AS FechaVigenciaHasta,
                activo                  AS Activo,
                fecha_creacion          AS FechaCreacion,
                fecha_modificacion      AS FechaModificacion
            FROM tarifas_base
            WHERE empresa_id = @EmpresaId
              AND activo = true
              AND (zona_origen_id IS NULL OR zona_origen_id = @ZonaOrigenId)
              AND (zona_destino_id IS NULL OR zona_destino_id = @ZonaDestinoId)
              AND modo_transporte = @Modo
              AND tipo_servicio = @TipoServicio
              AND fecha_vigencia_desde <= @Fecha
              AND (fecha_vigencia_hasta IS NULL OR fecha_vigencia_hasta >= @Fecha)
            ORDER BY
                ((CASE WHEN zona_origen_id IS NOT NULL THEN 1 ELSE 0 END)
               + (CASE WHEN zona_destino_id IS NOT NULL THEN 1 ELSE 0 END)) DESC,
                fecha_vigencia_desde DESC,
                fecha_creacion DESC
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<TarifaBase>(sql, new
        {
            EmpresaId = empresaId,
            ZonaOrigenId = zonaOrigenId,
            ZonaDestinoId = zonaDestinoId,
            Modo = modo,
            TipoServicio = tipoServicio,
            Fecha = fecha
        });
    }

    /// <summary>
    /// Insertar tarifa (nueva versión). El UUID lo genera la BD (gen_random_uuid).
    /// La BLL usa este método al crear UNA tarifa y al crear la SUCESORA
    /// versionada (ADR-015).
    /// </summary>
    public async Task<Guid> CreateAsync(TarifaBase entidad)
    {
        const string sql = @"
            INSERT INTO tarifas_base (
                empresa_id,
                nombre,
                codigo,
                zona_origen_id,
                zona_destino_id,
                modo_transporte,
                tipo_servicio,
                tipo_tarifa,
                precio_unitario,
                precio_minimo,
                moneda,
                fecha_vigencia_desde,
                fecha_vigencia_hasta
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Codigo,
                @ZonaOrigenId,
                @ZonaDestinoId,
                @ModoTransporte,
                @TipoServicio,
                @TipoTarifa,
                @PrecioUnitario,
                @PrecioMinimo,
                @Moneda,
                @FechaVigenciaDesde,
                @FechaVigenciaHasta
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Cierra la vigencia de una tarifa: fija fecha_vigencia_hasta
    /// (la BLL usa ayer al crear la nueva versión, ADR-015). NO modifica
    /// el resto de campos — es el ÚNICO UPDATE de negocio permitido sobre
    /// 'tarifas_base' (el trigger actualiza fecha_modificacion).
    /// </summary>
    public async Task<bool> CerrarVigenciaAsync(Guid id, Guid empresaId,
        DateOnly fechaHasta)
    {
        const string sql = @"
            UPDATE tarifas_base
            SET fecha_vigencia_hasta = @FechaHasta
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId, FechaHasta = fechaHasta });
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false — conserva el historial (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE tarifas_base
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    // ── Recargos de la tarifa (HU-020 CA-02) ─────────────────────

    /// <summary>Lista los recargos activos de una tarifa de la empresa.</summary>
    public async Task<IEnumerable<RecargoTarifa>> GetRecargosAsync(
        Guid tarifaId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                tarifa_id           AS TarifaId,
                codigo_recargo      AS CodigoRecargo,
                nombre              AS Nombre,
                tipo_calculo        AS TipoCalculo,
                valor               AS Valor,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM recargos_tarifa
            WHERE tarifa_id = @TarifaId
              AND empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<RecargoTarifa>(
            sql, new { TarifaId = tarifaId, EmpresaId = empresaId });
    }

    /// <summary>Insertar recargo. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateRecargoAsync(RecargoTarifa entidad)
    {
        const string sql = @"
            INSERT INTO recargos_tarifa (
                empresa_id,
                tarifa_id,
                codigo_recargo,
                nombre,
                tipo_calculo,
                valor
            ) VALUES (
                @EmpresaId,
                @TarifaId,
                @CodigoRecargo,
                @Nombre,
                @TipoCalculo,
                @Valor
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un recargo de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateRecargoAsync(RecargoTarifa entidad)
    {
        const string sql = @"
            UPDATE recargos_tarifa SET
                codigo_recargo = @CodigoRecargo,
                nombre         = @Nombre,
                tipo_calculo   = @TipoCalculo,
                valor          = @Valor
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete de un recargo: SET activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateRecargoAsync(Guid recargoId, Guid empresaId)
    {
        const string sql = @"
            UPDATE recargos_tarifa
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = recargoId, EmpresaId = empresaId });
        return rows > 0;
    }
}