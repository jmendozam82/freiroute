using System.Data;
using Dapper;
using Freiroute.DTO.Reclamo;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de reclamos (HU-032). Cubre las tablas 'reclamos' e
/// 'historial_estados_reclamo' (INSERT-only, mismo patrón que
/// historial_estados_orden — ADR-019).
/// ADR-003: todo método filtra por empresaId.
/// El número legible se genera con la función generar_numero_reclamo()
/// — patrón ADR-020, Opción B (contadores_reclamo).
/// No existe DeleteAsync — soft delete (ADR-005).
/// </summary>
public class ReclamoRepository : IReclamoRepository
{
    // Columnas de 'reclamos' mapeadas a la entidad Reclamo (PascalCase).
    private const string Col = @"
        r.id                 AS Id,
        r.empresa_id         AS EmpresaId,
        r.orden_id           AS OrdenId,
        r.numero_reclamo     AS NumeroReclamo,
        r.tipo               AS Tipo,
        r.descripcion        AS Descripcion,
        r.monto_reclamado    AS MontoReclamado,
        r.estado             AS Estado,
        r.referencias_evidencia AS ReferenciasEvidencia,
        r.activo             AS Activo,
        r.fecha_creacion     AS FechaCreacion,
        r.fecha_modificacion AS FechaModificacion,
        r.creado_por         AS CreadoPor,
        r.modificado_por     AS ModificadoPor";

    // Columnas de 'historial_estados_reclamo' → HistorialEstadoReclamo (PascalCase).
    private const string ColHistorial = @"
        h.id              AS Id,
        h.empresa_id      AS EmpresaId,
        h.reclamo_id      AS ReclamoId,
        h.estado_anterior AS EstadoAnterior,
        h.estado_nuevo    AS EstadoNuevo,
        h.motivo          AS Motivo,
        h.usuario_id      AS UsuarioId,
        u.nombre_completo AS UsuarioNombre,
        h.fecha_creacion  AS FechaCreacion";

    private readonly IDbConnection _connection;

    public ReclamoRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Crea el reclamo. Estado inicial ABIERTO (lo fija la BD vía DEFAULT;
    /// la BLL lo explicita). referencias_evidencia es TEXT[] — se enlaza
    /// con cast explícito para Npgsql. El UUID lo genera gen_random_uuid().
    /// </summary>
    public async Task<Guid> CreateAsync(Reclamo entity)
    {
        const string sql = @"
            INSERT INTO reclamos (
                empresa_id,
                orden_id,
                numero_reclamo,
                tipo,
                descripcion,
                monto_reclamado,
                estado,
                referencias_evidencia,
                creado_por,
                modificado_por
            ) VALUES (
                @EmpresaId,
                @OrdenId,
                @NumeroReclamo,
                @Tipo,
                @Descripcion,
                @MontoReclamado,
                @Estado,
                @ReferenciasEvidencia::TEXT[],
                @CreadoPor,
                @ModificadoPor
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    /// <summary>
    /// Obtiene un reclamo con JOIN a ordenes y clientes (nombre legible
    /// y referencia del tenant). Los JOINs validan el tenant (empresa_id)
    /// para no cruzar datos entre empresas teniendo en cuenta que RLS
    /// opera por fila — defensa en profundidad (ADR-003).
    /// Devuelve la entidad Reclamo; los nombres de la orden/cliente los
    /// resuelve la BLL al mapear al DTO.
    /// </summary>
    public async Task<Reclamo?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = $@"
            SELECT {Col},
                   o.numero_orden AS OrdenNumero,
                   c.nombre       AS ClienteNombre
            FROM reclamos r
            INNER JOIN ordenes o  ON o.id = r.orden_id
                                  AND o.empresa_id = r.empresa_id
            INNER JOIN clientes c ON c.id = o.cliente_id
                                  AND c.empresa_id = r.empresa_id
            WHERE r.id = @Id
              AND r.empresa_id = @EmpresaId
              AND r.activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Reclamo>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// Listado paginado con filtros opcionales (estado, tipo, rango de
    /// fechas). Usa COUNT(*) OVER() para obtener el total sin segunda query.
    /// Los nombres legibles (OrdenNumero/ClienteNombre) no se mapean a la
    /// entidad — la BLL los resuelve al construir ReclamoListDto.
    /// </summary>
    public async Task<(IEnumerable<Reclamo> Items, int Total)> GetAllAsync(
        Guid empresaId, ReclamoFiltroDto filtro)
    {
        const string select = $@"
            SELECT {Col},
                   o.numero_orden AS OrdenNumero,
                   c.nombre       AS ClienteNombre,
                   COUNT(*) OVER() AS TotalCount
            FROM reclamos r
            INNER JOIN ordenes o  ON o.id = r.orden_id
                                  AND o.empresa_id = r.empresa_id
            INNER JOIN clientes c ON c.id = o.cliente_id
                                  AND c.empresa_id = r.empresa_id
            WHERE r.empresa_id = @EmpresaId
              AND r.activo = true";

        var condiciones = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("EmpresaId", empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            condiciones.Add("r.estado = @Estado");
            parametros.Add("Estado", filtro.Estado);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
        {
            condiciones.Add("r.tipo = @Tipo");
            parametros.Add("Tipo", filtro.Tipo);
        }

        if (filtro.Desde.HasValue)
        {
            condiciones.Add("r.fecha_creacion >= @Desde");
            parametros.Add("Desde", filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            condiciones.Add("r.fecha_creacion <= @Hasta");
            parametros.Add("Hasta", filtro.Hasta.Value);
        }

        var where = condiciones.Count > 0
            ? " AND " + string.Join(" AND ", condiciones)
            : string.Empty;

        var page = filtro.Page < 1 ? 1 : filtro.Page;
        var pageSize = filtro.PageSize < 1 ? 20 : filtro.PageSize;
        var offset = (page - 1) * pageSize;

        var sql = $@"{select}{where}
                     ORDER BY r.fecha_creacion DESC
                     LIMIT @PageSize OFFSET @Offset";

        parametros.Add("PageSize", pageSize);
        parametros.Add("Offset", offset);

        var items = await _connection.QueryAsync<Reclamo>(sql, parametros);

        // COUNT(*) OVER() no mapea a la entidad Reclamo; el total se
        // obtiene con una query COUNT(*) ligera con los mismos filtros.
        // El alias 'r' es obligatorio porque {where} usa prefijos r.* para
        // las condiciones dinámicas.
        var countSql = $@"SELECT COUNT(*)
                          FROM reclamos r
                          WHERE r.empresa_id = @EmpresaId
                            AND r.activo = true{where}";

        var countParams = new DynamicParameters();
        countParams.Add("EmpresaId", empresaId);
        if (!string.IsNullOrWhiteSpace(filtro.Estado)) countParams.Add("Estado", filtro.Estado);
        if (!string.IsNullOrWhiteSpace(filtro.Tipo)) countParams.Add("Tipo", filtro.Tipo);
        if (filtro.Desde.HasValue) countParams.Add("Desde", filtro.Desde.Value);
        if (filtro.Hasta.HasValue) countParams.Add("Hasta", filtro.Hasta.Value);

        var total = await _connection.ExecuteScalarAsync<int>(countSql, countParams);

        return (items, total);
    }

    /// <summary>Transición de estado (FSM validada en BLL antes de llamar).</summary>
    public async Task UpdateEstadoAsync(Guid id, Guid empresaId, string estadoNuevo,
        Guid modificadoPor, DateTime fechaModificacion)
    {
        const string sql = @"
            UPDATE reclamos SET
                estado            = @EstadoNuevo,
                modificado_por    = @ModificadoPor,
                fecha_modificacion = @FechaModificacion
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        await _connection.ExecuteAsync(sql, new
        {
            Id = id,
            EmpresaId = empresaId,
            EstadoNuevo = estadoNuevo,
            ModificadoPor = modificadoPor,
            FechaModificacion = fechaModificacion
        });
    }

    /// <summary>Lista los reclamos activos de un cliente (vía ordenes.cliente_id).</summary>
    public async Task<IEnumerable<Reclamo>> GetByClienteAsync(
        Guid clienteId, Guid empresaId)
    {
        const string sql = $@"
            SELECT {Col},
                   o.numero_orden AS OrdenNumero,
                   c.nombre       AS ClienteNombre
            FROM reclamos r
            INNER JOIN ordenes o ON o.id = r.orden_id
                                 AND o.empresa_id = r.empresa_id
            INNER JOIN clientes c ON c.id = o.cliente_id
                                 AND c.empresa_id = r.empresa_id
            WHERE o.cliente_id = @ClienteId
              AND r.empresa_id = @EmpresaId
              AND r.activo = true
            ORDER BY r.fecha_creacion DESC";

        return await _connection.QueryAsync<Reclamo>(
            sql, new { ClienteId = clienteId, EmpresaId = empresaId });
    }

    /// <summary>
    /// Reporte agregado por tipo y estado en el período (HU-032 CA-09).
    /// Devuelve DTOs listos para la respuesta de la API.
    /// </summary>
    public async Task<IEnumerable<ReclamoReporteItemDto>> GetReporteAsync(
        Guid empresaId, DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT
                tipo   AS Tipo,
                estado AS Estado,
                COUNT(*)                AS Total,
                COALESCE(SUM(monto_reclamado), 0) AS MontoTotal,
                COALESCE(AVG(monto_reclamado), 0) AS MontoPromedio
            FROM reclamos
            WHERE empresa_id = @EmpresaId
              AND activo = true
              AND fecha_creacion BETWEEN @Desde AND @Hasta
            GROUP BY tipo, estado
            ORDER BY tipo ASC, estado ASC";

        return await _connection.QueryAsync<ReclamoReporteItemDto>(
            sql, new { EmpresaId = empresaId, Desde = desde, Hasta = hasta });
    }

    /// <summary>
    /// INSERT-only en 'historial_estados_reclamo'. El Id lo genera la BD
    /// (gen_random_uuid) — mismo patrón que historial_estados_orden.
    /// </summary>
    public async Task InsertHistorialAsync(HistorialEstadoReclamo historial)
    {
        const string sql = @"
            INSERT INTO historial_estados_reclamo (
                empresa_id,
                reclamo_id,
                estado_anterior,
                estado_nuevo,
                motivo,
                usuario_id
            ) VALUES (
                @EmpresaId,
                @ReclamoId,
                @EstadoAnterior,
                @EstadoNuevo,
                @Motivo,
                @UsuarioId
            )";

        await _connection.ExecuteAsync(sql, historial);
    }

    /// <summary>
    /// Historial de estados del reclamo, del más reciente al más antiguo.
    /// LEFT JOIN a usuarios para validar integridad; se devuelven solo los
    /// campos de la entidad (el nombre del usuario lo enriquece la BLL).
    /// </summary>
    public async Task<IEnumerable<HistorialEstadoReclamo>> GetHistorialAsync(
        Guid reclamoId, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColHistorial}
            FROM historial_estados_reclamo h
            LEFT JOIN usuarios u ON u.id = h.usuario_id
            WHERE h.reclamo_id = @ReclamoId
              AND h.empresa_id = @EmpresaId
            ORDER BY h.fecha_creacion DESC";

        return await _connection.QueryAsync<HistorialEstadoReclamo>(
            sql, new { ReclamoId = reclamoId, EmpresaId = empresaId });
    }

    /// <summary>
    /// Número atómico REC-{PREFIX}-{AÑO}-{NNNN} vía función PostgreSQL
    /// generar_numero_reclamo() (ADR-020, Opción B). Soportado para
    /// concurrencia por el lock por fila en contadores_reclamo.
    /// El repo concatena el prefijo "REC-" — la función solo aplica
    /// UPPER(TRIM) sobre el prefijo recibido. El BLL pasa el prefijo
    /// del tenant (ej: "FRT") y aquí se produce "REC-FRT-2026-0001".
    /// </summary>
    public async Task<string> GenerarNumeroReclamoAsync(
        Guid empresaId, string prefijo, int anio)
    {
        const string sql = @"
            SELECT generar_numero_reclamo(
                @EmpresaId::uuid,
                @Prefijo,
                @Anio::smallint
            )";

        // La función PostgreSQL generar_numero_reclamo() siempre retorna
        // TEXT no nulo (upsert atómico garantizado) — null-forgiving es seguro.
        // CRÍTICO: cast explícito @Anio::smallint — sin él, PostgreSQL no
        // resuelve la sobrecarga (int4 ≠ int2) y falla en runtime.
        return (await _connection.ExecuteScalarAsync<string>(sql, new
        {
            EmpresaId = empresaId,
            Prefijo = "REC-" + prefijo,
            Anio = (short)anio
        }))!;
    }

    public async Task<string?> GetNumeroReclamoAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT numero_reclamo
            FROM reclamos
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        return await _connection.QueryFirstOrDefaultAsync<string>(sql,
            new { Id = id, EmpresaId = empresaId });
    }

    public async Task UpdateNumeroReclamoAsync(Guid id, Guid empresaId, string numero)
    {
        const string sql = @"
            UPDATE reclamos
            SET numero_reclamo = @Numero
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId, Numero = numero });
    }
}