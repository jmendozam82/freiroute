using System.Data;
using Dapper;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;
using Freiroute.Utility.Pagination;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de órdenes de transporte — entidad central de EP-04
/// Order Management (HU-021 a HU-026, ADR-003, ADR-019, ADR-020).
/// ADR-003: todo método filtra por empresa_id (multi-tenant).
/// ADR-019: máquina de estados finitos validada en BLL; aquí solo se
///          persisten los cambios de estado y su historial de auditoría.
/// ADR-020: GenerarNumeroOrdenAsync llama a la función PostgreSQL
///          generar_numero_orden() (upsert atómico por tenant y año).
/// No existe DeleteAsync: soft delete (ADR-005). Las líneas de detalle
/// se eliminan físicamente en DeleteLineasAsync (recreación en Update).
/// </summary>
public class OrdenRepository : IOrdenRepository
{
    // Columnas completas de 'ordenes' mapeadas a la entidad Orden (PascalCase).
    // Se usa en GETs con JOINs (GetById, GetByShipmentId, GetSubOrdenes).
    private const string ColOrden = @"
        o.id                      AS Id,
        o.empresa_id              AS EmpresaId,
        o.numero_orden            AS NumeroOrden,
        o.cliente_id              AS ClienteId,
        o.origen_id               AS OrigenId,
        o.destino_id              AS DestinoId,
        o.tipo_mercancia_id       AS TipoMercanciaId,
        o.unidad_medida_id        AS UnidadMedidaId,
        o.tipo_embalaje_id        AS TipoEmbalajeId,
        o.tarifa_id               AS TarifaId,
        o.shipment_id             AS ShipmentId,
        o.api_key_id              AS ApiKeyId,
        o.cantidad                AS Cantidad,
        o.peso_kg                 AS PesoKg,
        o.volumen_m3              AS VolumenM3,
        o.valor_declarado         AS ValorDeclarado,
        o.modo_transporte         AS ModoTransporte,
        o.nivel_servicio          AS NivelServicio,
        o.prioridad               AS Prioridad,
        o.fecha_pickup_solicitada AS FechaPickupSolicitada,
        o.fecha_entrega_requerida AS FechaEntregaRequerida,
        o.fecha_confirmacion      AS FechaConfirmacion,
        o.referencia_cliente      AS ReferenciaCliente,
        o.instrucciones           AS Instrucciones,
        o.estado                  AS Estado,
        o.es_split                AS EsSplit,
        o.orden_origen_id         AS OrdenOrigenId,
        o.origen_creacion         AS OrigenCreacion,
        o.activo                  AS Activo,
        o.fecha_creacion          AS FechaCreacion,
        o.fecha_modificacion      AS FechaModificacion,
        o.creado_por              AS CreadoPor,
        o.modificado_por          AS ModificadoPor,
        c.nombre                  AS ClienteNombre,
        uo.nombre                 AS OrigenNombre,
        ud.nombre                 AS DestinoNombre";

    // Subconjunto de columnas para el listado paginado (sin texto pesado:
    // instrucciones, valor_declarado, etc. — se cargan en GetByIdAsync).
    private const string ColOrdenListado = @"
        o.id                      AS Id,
        o.empresa_id              AS EmpresaId,
        o.numero_orden            AS NumeroOrden,
        o.cliente_id              AS ClienteId,
        o.origen_id               AS OrigenId,
        o.destino_id              AS DestinoId,
        o.tipo_mercancia_id       AS TipoMercanciaId,
        o.shipment_id             AS ShipmentId,
        o.cantidad                AS Cantidad,
        o.peso_kg                 AS PesoKg,
        o.modo_transporte         AS ModoTransporte,
        o.nivel_servicio          AS NivelServicio,
        o.prioridad               AS Prioridad,
        o.fecha_pickup_solicitada AS FechaPickupSolicitada,
        o.fecha_entrega_requerida AS FechaEntregaRequerida,
        o.referencia_cliente      AS ReferenciaCliente,
        o.estado                  AS Estado,
        o.es_split                AS EsSplit,
        o.origen_creacion         AS OrigenCreacion,
        o.fecha_creacion          AS FechaCreacion,
        o.fecha_modificacion      AS FechaModificacion,
        c.nombre                  AS ClienteNombre,
        uo.nombre                 AS OrigenNombre,
        ud.nombre                 AS DestinoNombre,
        o.numero_po               AS NumeroPo";

    private readonly IDbConnection _connection;

    public OrdenRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista órdenes paginadas de la empresa con filtros opcionales
    /// (RNF-01.4: 20/página, máx 100). Los filtros se construyen
    /// dinámicamente sobre la base empresa_id + activo (ADR-003).
    /// El COUNT se ejecuta en una query separada para obtener TotalItems
    /// sin transportar filas.
    /// </summary>
    public async Task<PagedResult<Orden>> GetAllAsync(
        Guid empresaId, int page, int pageSize,
        string? clienteId = null, string? estado = null,
        string? modoTransporte = null, string? nivelServicio = null,
        string? prioridad = null, string? origenCreacion = null,
        string? shipmentId = null, string? q = null,
        DateOnly? fechaPickupDesde = null, DateOnly? fechaPickupHasta = null,
        DateOnly? fechaEntregaDesde = null, DateOnly? fechaEntregaHasta = null,
        bool? esSplit = null, string? po = null)
    {
        // Valores seguros de paginación (RNF-01.4)
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var where = new List<string> { "o.empresa_id = @EmpresaId", "o.activo = true" };
        var parameters = new DynamicParameters();
        parameters.Add("EmpresaId", empresaId);

        // clienteId y shipmentId llegan como string (query params) — se parsean a Guid.
        if (Guid.TryParse(clienteId, out var clienteIdParsed))
        {
            where.Add("o.cliente_id = @ClienteId");
            parameters.Add("ClienteId", clienteIdParsed);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            where.Add("o.estado = @Estado");
            parameters.Add("Estado", estado);
        }

        if (!string.IsNullOrWhiteSpace(modoTransporte))
        {
            where.Add("o.modo_transporte = @ModoTransporte");
            parameters.Add("ModoTransporte", modoTransporte);
        }

        if (!string.IsNullOrWhiteSpace(nivelServicio))
        {
            where.Add("o.nivel_servicio = @NivelServicio");
            parameters.Add("NivelServicio", nivelServicio);
        }

        if (!string.IsNullOrWhiteSpace(prioridad))
        {
            where.Add("o.prioridad = @Prioridad");
            parameters.Add("Prioridad", prioridad);
        }

        if (!string.IsNullOrWhiteSpace(origenCreacion))
        {
            where.Add("o.origen_creacion = @OrigenCreacion");
            parameters.Add("OrigenCreacion", origenCreacion);
        }

        if (Guid.TryParse(shipmentId, out var shipmentIdParsed))
        {
            where.Add("o.shipment_id = @ShipmentId");
            parameters.Add("ShipmentId", shipmentIdParsed);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            where.Add(@"(
                o.numero_orden       ILIKE '%' || @Q || '%'
                OR o.referencia_cliente ILIKE '%' || @Q || '%'
            )");
            parameters.Add("Q", q);
        }

        if (fechaPickupDesde.HasValue)
        {
            where.Add("o.fecha_pickup_solicitada >= @FechaPickupDesde");
            parameters.Add("FechaPickupDesde", fechaPickupDesde.Value);
        }

        if (fechaPickupHasta.HasValue)
        {
            where.Add("o.fecha_pickup_solicitada <= @FechaPickupHasta");
            parameters.Add("FechaPickupHasta", fechaPickupHasta.Value);
        }

        if (fechaEntregaDesde.HasValue)
        {
            where.Add("o.fecha_entrega_requerida >= @FechaEntregaDesde");
            parameters.Add("FechaEntregaDesde", fechaEntregaDesde.Value);
        }

        if (fechaEntregaHasta.HasValue)
        {
            where.Add("o.fecha_entrega_requerida <= @FechaEntregaHasta");
            parameters.Add("FechaEntregaHasta", fechaEntregaHasta.Value);
        }

        if (esSplit.HasValue)
        {
            where.Add("o.es_split = @EsSplit");
            parameters.Add("EsSplit", esSplit.Value);
        }

        // HU-028 CA-01: filtro por número de Purchase Order exacto
        // (la BLL envía el valor de OrdenFiltroDto.Po).
        if (!string.IsNullOrWhiteSpace(po))
        {
            where.Add("o.numero_po = @Po");
            parameters.Add("Po", po);
        }

        var whereSql = string.Join(" AND ", where);
        var offset = (page - 1) * pageSize;

        // Total de registros (para calcular páginas)
        var sqlCount = $@"
            SELECT COUNT(*)
            FROM ordenes o
            WHERE {whereSql}";

        var totalItems = await _connection.ExecuteScalarAsync<int>(sqlCount, parameters);

        // Página de registros — JOINs a clientes y ubicaciones (origen/destino)
        // para validar integridad referencial y habilitar filtros/orden por nombre.
        var sqlQuery = $@"
            SELECT {ColOrdenListado}
            FROM ordenes o
            INNER JOIN clientes     c  ON c.id  = o.cliente_id
            INNER JOIN ubicaciones  uo ON uo.id = o.origen_id
            INNER JOIN ubicaciones  ud ON ud.id = o.destino_id
            WHERE {whereSql}
            ORDER BY o.fecha_creacion DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var items = await _connection.QueryAsync<Orden>(sqlQuery, parameters);

        return new PagedResult<Orden>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Obtiene una orden completa dentro de la empresa con JOINs a los
    /// maestros de detalle (cliente, orígenes/destino, tipo de mercancía,
    /// unidad de medida y embalaje — este último LEFT JOIN porque es opcional).
    /// </summary>
    public async Task<Orden?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes          c  ON c.id  = o.cliente_id
            INNER JOIN ubicaciones       uo ON uo.id = o.origen_id
            INNER JOIN ubicaciones       ud ON ud.id = o.destino_id
            INNER JOIN tipos_mercancia   tm ON tm.id = o.tipo_mercancia_id
            INNER JOIN unidades_medida   um ON um.id = o.unidad_medida_id
            LEFT  JOIN tipos_embalaje    te ON te.id = o.tipo_embalaje_id
            WHERE o.id = @Id
              AND o.empresa_id = @EmpresaId
              AND o.activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Orden>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Obtiene las líneas de detalle de una orden en orden de presentación.</summary>
    public async Task<IEnumerable<LineaOrden>> GetLineasAsync(Guid ordenId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                orden_id            AS OrdenId,
                descripcion         AS Descripcion,
                cantidad            AS Cantidad,
                unidad_medida_id    AS UnidadMedidaId,
                peso_kg             AS PesoKg,
                volumen_m3          AS VolumenM3,
                valor_unitario      AS ValorUnitario,
                numero_linea        AS NumeroLinea,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM lineas_orden
            WHERE orden_id = @OrdenId
              AND empresa_id = @EmpresaId
              AND activo = true
            ORDER BY numero_linea ASC";

        return await _connection.QueryAsync<LineaOrden>(
            sql, new { OrdenId = ordenId, EmpresaId = empresaId });
    }

    /// <summary>Insertar orden. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Orden entidad)
    {
        const string sql = @"
            INSERT INTO ordenes (
                empresa_id,
                numero_orden,
                cliente_id,
                origen_id,
                destino_id,
                tipo_mercancia_id,
                unidad_medida_id,
                tipo_embalaje_id,
                tarifa_id,
                shipment_id,
                api_key_id,
                cantidad,
                peso_kg,
                volumen_m3,
                valor_declarado,
                modo_transporte,
                nivel_servicio,
                prioridad,
                fecha_pickup_solicitada,
                fecha_entrega_requerida,
                fecha_confirmacion,
                referencia_cliente,
                numero_po,
                numero_so,
                instrucciones,
                estado,
                es_split,
                orden_origen_id,
                origen_creacion,
                activo,
                creado_por,
                modificado_por
            ) VALUES (
                @EmpresaId,
                @NumeroOrden,
                @ClienteId,
                @OrigenId,
                @DestinoId,
                @TipoMercanciaId,
                @UnidadMedidaId,
                @TipoEmbalajeId,
                @TarifaId,
                @ShipmentId,
                @ApiKeyId,
                @Cantidad,
                @PesoKg,
                @VolumenM3,
                @ValorDeclarado,
                @ModoTransporte,
                @NivelServicio,
                @Prioridad,
                @FechaPickupSolicitada,
                @FechaEntregaRequerida,
                @FechaConfirmacion,
                @ReferenciaCliente,
                @NumeroPo,
                @NumeroSo,
                @Instrucciones,
                @Estado,
                @EsSplit,
                @OrdenOrigenId,
                @OrigenCreacion,
                @Activo,
                @CreadoPor,
                @ModificadoPor
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Insertar línea de detalle. El UUID lo genera la BD.</summary>
    public async Task<Guid> CreateLineaAsync(LineaOrden entidad)
    {
        const string sql = @"
            INSERT INTO lineas_orden (
                empresa_id,
                orden_id,
                descripcion,
                cantidad,
                unidad_medida_id,
                peso_kg,
                volumen_m3,
                valor_unitario,
                numero_linea,
                activo
            ) VALUES (
                @EmpresaId,
                @OrdenId,
                @Descripcion,
                @Cantidad,
                @UnidadMedidaId,
                @PesoKg,
                @VolumenM3,
                @ValorUnitario,
                @NumeroLinea,
                @Activo
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>
    /// Elimina TODAS las líneas de una orden (recreación en Update).
    /// DELETE físico deliberado y exclusivo de las tablas hijas de detalle —
    /// la orden en sí nunca se borra (ADR-005). Documentado en la migración
    /// de lineas_orden (ON DELETE CASCADE).
    /// </summary>
    public async Task<bool> DeleteLineasAsync(Guid ordenId, Guid empresaId)
    {
        const string sql = @"
            DELETE FROM lineas_orden
            WHERE orden_id = @OrdenId
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { OrdenId = ordenId, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>Actualiza una orden activa de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(Orden entidad)
    {
        const string sql = @"
            UPDATE ordenes SET
                numero_orden            = @NumeroOrden,
                cliente_id              = @ClienteId,
                origen_id               = @OrigenId,
                destino_id              = @DestinoId,
                tipo_mercancia_id       = @TipoMercanciaId,
                unidad_medida_id        = @UnidadMedidaId,
                tipo_embalaje_id        = @TipoEmbalajeId,
                tarifa_id               = @TarifaId,
                shipment_id             = @ShipmentId,
                api_key_id              = @ApiKeyId,
                cantidad                = @Cantidad,
                peso_kg                 = @PesoKg,
                volumen_m3              = @VolumenM3,
                valor_declarado         = @ValorDeclarado,
                modo_transporte         = @ModoTransporte,
                nivel_servicio          = @NivelServicio,
                prioridad               = @Prioridad,
                fecha_pickup_solicitada = @FechaPickupSolicitada,
                fecha_entrega_requerida = @FechaEntregaRequerida,
                fecha_confirmacion      = @FechaConfirmacion,
                referencia_cliente      = @ReferenciaCliente,
                numero_po               = @NumeroPo,
                numero_so               = @NumeroSo,
                instrucciones           = @Instrucciones,
                estado                  = @Estado,
                es_split                = @EsSplit,
                orden_origen_id         = @OrdenOrigenId,
                origen_creacion         = @OrigenCreacion,
                modificado_por          = @ModificadoPor
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false (ADR-005). Solo en estado DRAFT (validado en BLL).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE ordenes
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Genera el número de orden atómico (ADR-020) llamando a la función
    /// PostgreSQL generar_numero_orden(). CRÍTICO: el parámetro Anio se
    /// castea a smallint — la función recibe SMALLINT.
    /// </summary>
    public async Task<string> GenerarNumeroOrdenAsync(Guid empresaId, string prefijo, int anio)
    {
        const string sql = @"
            SELECT generar_numero_orden(
                @EmpresaId::uuid,
                @Prefijo,
                @Anio::smallint
            )";

        // La función PostgreSQL generar_numero_orden() siempre retorna
        // TEXT no nulo (upsert atómico garantizado) — null-forgiving es seguro.
        return (await _connection.ExecuteScalarAsync<string>(sql, new
        {
            EmpresaId = empresaId,
            Prefijo = prefijo,
            Anio = (short)anio
        }))!;
    }

    /// <summary>
    /// Actualiza el estado FSM de la orden (ADR-019). El historial debe
    /// registrarse en la misma transacción desde la BLL.
    /// </summary>
    public async Task<bool> ActualizarEstadoAsync(Guid id, string estadoNuevo, Guid empresaId)
    {
        const string sql = @"
            UPDATE ordenes
            SET estado = @EstadoNuevo,
                fecha_confirmacion = CASE
                    WHEN @EstadoNuevo = 'CONFIRMED' THEN COALESCE(fecha_confirmacion, now())
                    ELSE fecha_confirmacion
                END
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EstadoNuevo = estadoNuevo, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Registra una transición de estado en el historial de auditoría (ADR-019).
    /// Registro INSERT-only: el estado_anterior es NULL en la inserción del DRAFT inicial.
    /// </summary>
    public async Task<bool> RegistrarHistorialEstadoAsync(HistorialEstadoOrden historial)
    {
        const string sql = @"
            INSERT INTO historial_estados_orden (
                empresa_id,
                orden_id,
                estado_anterior,
                estado_nuevo,
                motivo,
                usuario_id,
                activo
            ) VALUES (
                @EmpresaId,
                @OrdenId,
                @EstadoAnterior,
                @EstadoNuevo,
                @Motivo,
                @UsuarioId,
                @Activo
            )";

        var rows = await _connection.ExecuteAsync(sql, historial);
        return rows > 0;
    }

    /// <summary>
    /// Obtiene el historial de estados de una orden ordenado por fecha DESC.
    /// LEFT JOIN a usuarios para validar integridad; se devuelven solo los
    /// campos de la entidad (el nombre del usuario se enriquece en la BLL).
    /// </summary>
    public async Task<IEnumerable<HistorialEstadoOrden>> GetHistorialAsync(
        Guid ordenId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                h.id                 AS Id,
                h.empresa_id         AS EmpresaId,
                h.orden_id           AS OrdenId,
                h.estado_anterior    AS EstadoAnterior,
                h.estado_nuevo       AS EstadoNuevo,
                h.motivo             AS Motivo,
                h.usuario_id         AS UsuarioId,
                u.nombre_completo   AS UsuarioNombre,
                h.activo             AS Activo,
                h.fecha_creacion     AS FechaCreacion,
                h.fecha_modificacion AS FechaModificacion
            FROM historial_estados_orden h
            LEFT JOIN usuarios u ON u.id = h.usuario_id
            WHERE h.orden_id = @OrdenId
              AND h.empresa_id = @EmpresaId
            ORDER BY h.fecha_creacion DESC";

        return await _connection.QueryAsync<HistorialEstadoOrden>(
            sql, new { OrdenId = ordenId, EmpresaId = empresaId });
    }

    /// <summary>
    /// Asigna o limpia el shipment de una orden actualizando su estado
    /// (HU-025 — consolidación y desconsolidación). Si shipmentId es null,
    /// la referencia queda en NULL.
    /// </summary>
    public async Task<bool> AsignarShipmentAsync(
        Guid ordenId, Guid? shipmentId, string estadoNuevo, Guid empresaId)
    {
        const string sql = @"
            UPDATE ordenes
            SET shipment_id = @ShipmentId,
                estado      = @EstadoNuevo
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new
            {
                Id = ordenId,
                ShipmentId = shipmentId,
                EstadoNuevo = estadoNuevo,
                EmpresaId = empresaId
            });
        return rows > 0;
    }

    /// <summary>Obtiene todas las órdenes activas asignadas a un shipment (HU-025).</summary>
    public async Task<IEnumerable<Orden>> GetByShipmentIdAsync(
        Guid shipmentId, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes    c  ON c.id  = o.cliente_id
            INNER JOIN ubicaciones uo ON uo.id = o.origen_id
            INNER JOIN ubicaciones ud ON ud.id = o.destino_id
            WHERE o.shipment_id = @ShipmentId
              AND o.empresa_id = @EmpresaId
              AND o.activo = true
            ORDER BY o.fecha_creacion ASC";

        return await _connection.QueryAsync<Orden>(sql,
            new { ShipmentId = shipmentId, EmpresaId = empresaId });
    }

    /// <summary>Obtiene las sub-órdenes resultantes de un split (HU-026).</summary>
    public async Task<IEnumerable<Orden>> GetSubOrdenesAsync(
        Guid ordenOrigenId, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes    c  ON c.id  = o.cliente_id
            INNER JOIN ubicaciones uo ON uo.id = o.origen_id
            INNER JOIN ubicaciones ud ON ud.id = o.destino_id
            WHERE o.orden_origen_id = @OrdenOrigenId
              AND o.empresa_id = @EmpresaId
              AND o.activo = true
            ORDER BY o.fecha_creacion ASC";

        return await _connection.QueryAsync<Orden>(sql,
            new { OrdenOrigenId = ordenOrigenId, EmpresaId = empresaId });
    }

    /// <summary>
    /// Crea múltiples órdenes en lote (importación CSV HU-022, patrón
    /// fail-soft ADR-017). Cada INSERT retorna su Id; el set completo
    /// se ejecuta en UNA transacción atómica.
    /// </summary>
    public async Task<IEnumerable<Guid>> CreateBulkAsync(IEnumerable<Orden> ordenes)
    {
        const string sql = @"
            INSERT INTO ordenes (
                empresa_id,
                numero_orden,
                cliente_id,
                origen_id,
                destino_id,
                tipo_mercancia_id,
                unidad_medida_id,
                tipo_embalaje_id,
                tarifa_id,
                shipment_id,
                api_key_id,
                cantidad,
                peso_kg,
                volumen_m3,
                valor_declarado,
                modo_transporte,
                nivel_servicio,
                prioridad,
                fecha_pickup_solicitada,
                fecha_entrega_requerida,
                fecha_confirmacion,
                referencia_cliente,
                numero_po,
                numero_so,
                instrucciones,
                estado,
                es_split,
                orden_origen_id,
                origen_creacion,
                activo,
                creado_por,
                modificado_por
            ) VALUES (
                @EmpresaId,
                @NumeroOrden,
                @ClienteId,
                @OrigenId,
                @DestinoId,
                @TipoMercanciaId,
                @UnidadMedidaId,
                @TipoEmbalajeId,
                @TarifaId,
                @ShipmentId,
                @ApiKeyId,
                @Cantidad,
                @PesoKg,
                @VolumenM3,
                @ValorDeclarado,
                @ModoTransporte,
                @NivelServicio,
                @Prioridad,
                @FechaPickupSolicitada,
                @FechaEntregaRequerida,
                @FechaConfirmacion,
                @ReferenciaCliente,
                @NumeroPo,
                @NumeroSo,
                @Instrucciones,
                @Estado,
                @EsSplit,
                @OrdenOrigenId,
                @OrigenCreacion,
                @Activo,
                @CreadoPor,
                @ModificadoPor
            )
            RETURNING id";

        // Gestión del ciclo de conexión: solo la cerramos si la abrimos
        // nosotros (no interferir con el contenedor DI).
        var wasClosed = _connection.State == ConnectionState.Closed;
        if (wasClosed) _connection.Open();

        using var transaction = _connection.BeginTransaction();
        try
        {
            var ids = new List<Guid>();
            foreach (var orden in ordenes)
            {
                var id = await _connection.ExecuteScalarAsync<Guid>(sql, orden, transaction);
                ids.Add(id);
            }

            transaction.Commit();
            return ids;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            if (wasClosed) _connection.Close();
        }
    }

    /// <summary>
    /// Crea múltiples líneas de orden en lote (importación CSV HU-022)
    /// en UNA transacción atómica.
    /// </summary>
    public async Task CreateLineasBulkAsync(IEnumerable<LineaOrden> lineas)
    {
        const string sql = @"
            INSERT INTO lineas_orden (
                empresa_id,
                orden_id,
                descripcion,
                cantidad,
                unidad_medida_id,
                peso_kg,
                volumen_m3,
                valor_unitario,
                numero_linea,
                activo
            ) VALUES (
                @EmpresaId,
                @OrdenId,
                @Descripcion,
                @Cantidad,
                @UnidadMedidaId,
                @PesoKg,
                @VolumenM3,
                @ValorUnitario,
                @NumeroLinea,
                @Activo
            )";

        var wasClosed = _connection.State == ConnectionState.Closed;
        if (wasClosed) _connection.Open();

        using var transaction = _connection.BeginTransaction();
        try
        {
            foreach (var linea in lineas)
            {
                await _connection.ExecuteAsync(sql, linea, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            if (wasClosed) _connection.Close();
        }
    }

    // ── Sprint 5: PO/SO, SLA y prioridades (HU-028, HU-029, HU-031) ──────

    /// <summary>
    /// Busca todas las órdenes activas con un número de PO EXACTO del
    /// mismo tenant (HU-028 CA-05 — GET /api/ordenes/por-po/{numero}).
    /// La BLL valida el formato antes de llamar. Coherente con la
    /// interfaz IOrdenPoService (búsqueda exacta, no ILIKE).
    /// </summary>
    public async Task<IEnumerable<Orden>> GetPorPoAsync(string numeroPo, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes    c  ON c.id  = o.cliente_id AND c.empresa_id = o.empresa_id
            INNER JOIN ubicaciones uo ON uo.id = o.origen_id  AND uo.empresa_id = o.empresa_id
            INNER JOIN ubicaciones ud ON ud.id = o.destino_id AND ud.empresa_id = o.empresa_id
            WHERE o.numero_po = @NumeroPo
              AND o.empresa_id = @EmpresaId
              AND o.activo = true
            ORDER BY o.fecha_creacion DESC";

        return await _connection.QueryAsync<Orden>(
            sql, new { NumeroPo = numeroPo, EmpresaId = empresaId });
    }

    /// <summary>
    /// Órdenes críticas (HU-029 CA-04): prioridad CRITICO o ALTO cuyo
    /// último cambio de estado (historial_estados_orden) supera las 4 horas
    /// sin avanzar. Excluye estados terminales. Las consume el endpoint
    /// GET /api/ordenes/criticas y el job PrioridadOrdenesJob (ADR-013).
    /// </summary>
    public async Task<IEnumerable<Orden>> GetCriticasAsync(Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes    c  ON c.id  = o.cliente_id AND c.empresa_id = o.empresa_id
            INNER JOIN ubicaciones uo ON uo.id = o.origen_id  AND uo.empresa_id = o.empresa_id
            INNER JOIN ubicaciones ud ON ud.id = o.destino_id AND ud.empresa_id = o.empresa_id
            WHERE o.empresa_id = @EmpresaId
              AND o.activo = true
              AND o.prioridad IN ('CRITICO', 'ALTO')
              AND o.estado NOT IN ('DELIVERED', 'CLOSED', 'CANCELLED')
              AND (
                  SELECT MAX(h.fecha_creacion)
                  FROM historial_estados_orden h
                  WHERE h.orden_id = o.id
                    AND h.empresa_id = o.empresa_id
              ) < now() - interval '4 hours'
            ORDER BY
                CASE o.prioridad WHEN 'CRITICO' THEN 0 ELSE 1 END,
                o.fecha_entrega_requerida ASC NULLS LAST";

        return await _connection.QueryAsync<Orden>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Órdenes con SLA en riesgo (HU-031 CA-02): entrega requerida dentro
    /// de las próximas 24 h (incluye ya vencidas sin estado terminal) y
    /// estado no terminal. Las consume la BLL para alertas y el dashboard SLA.
    /// </summary>
    public async Task<IEnumerable<Orden>> GetSlaEnRiesgoAsync(Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColOrden}
            FROM ordenes o
            INNER JOIN clientes    c  ON c.id  = o.cliente_id AND c.empresa_id = o.empresa_id
            INNER JOIN ubicaciones uo ON uo.id = o.origen_id  AND uo.empresa_id = o.empresa_id
            INNER JOIN ubicaciones ud ON ud.id = o.destino_id AND ud.empresa_id = o.empresa_id
            WHERE o.empresa_id = @EmpresaId
              AND o.activo = true
              AND o.estado NOT IN ('DELIVERED', 'CLOSED', 'CANCELLED')
              AND o.fecha_entrega_requerida IS NOT NULL
              AND o.fecha_entrega_requerida <= now() + interval '24 hours'
            ORDER BY o.fecha_entrega_requerida ASC";

        return await _connection.QueryAsync<Orden>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>
    /// Persiste la fecha/hora real de entrega al registrar el POD
    /// (HU-031 CA-04). Sin filtro de activo — una orden puede registrar
    /// su fecha real aunque haya sido desactivada posteriormente.
    /// </summary>
    public async Task<bool> UpdateFechaEntregaRealAsync(
        Guid ordenId, Guid empresaId, DateTime fechaEntregaReal)
    {
        const string sql = @"
            UPDATE ordenes
            SET fecha_entrega_real = @FechaEntregaReal
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = ordenId, EmpresaId = empresaId, FechaEntregaReal = fechaEntregaReal });
        return rows > 0;
    }

    /// <summary>
    /// Métricas SLA de un cliente en el período (HU-031 CA-05): total de
    /// entregas reales registradas y cuántas cumplieron la fecha requerida.
    /// </summary>
    public async Task<(int TotalOrdenes, int OrdenesATiempo)> GetSlaClienteAsync(
        Guid clienteId, Guid empresaId, DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT
                COUNT(*) AS TotalOrdenes,
                COUNT(*) FILTER (
                    WHERE fecha_entrega_real <= fecha_entrega_requerida
                ) AS OrdenesATiempo
            FROM ordenes
            WHERE empresa_id = @EmpresaId
              AND cliente_id = @ClienteId
              AND activo = true
              AND estado IN ('DELIVERED', 'INVOICED', 'CLOSED')
              AND fecha_entrega_real IS NOT NULL
              AND fecha_entrega_real BETWEEN @Desde AND @Hasta";

        var resultado = await _connection.QuerySingleAsync(sql,
            new
            {
                EmpresaId = empresaId,
                ClienteId = clienteId,
                Desde = desde,
                Hasta = hasta
            });

        return (
            TotalOrdenes: (int)resultado.TotalOrdenes,
            OrdenesATiempo: (int)resultado.OrdenesATiempo
        );
    }

    /// <summary>
    /// Reporte de cumplimiento SLA por cliente en el período (HU-031 CA-07).
    /// Dapper mapea los campos agregados; PorcentajeCumplimiento y
    /// OrdenesTardias los calcula la BLL al construir el DTO de respuesta.
    /// </summary>
    public async Task<IEnumerable<SlaReporteItemDto>> GetSlaReporteAsync(
        Guid empresaId, DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT
                c.id              AS ClienteId,
                c.nombre          AS ClienteNombre,
                c.tipo_cliente    AS TipoCliente,
                COUNT(o.id)       AS TotalOrdenes,
                COUNT(*) FILTER (
                    WHERE o.fecha_entrega_real IS NOT NULL
                      AND o.fecha_entrega_real <= o.fecha_entrega_requerida
                ) AS OrdenesATiempo
            FROM ordenes o
            INNER JOIN clientes c ON c.id = o.cliente_id
            WHERE o.empresa_id = @EmpresaId
              AND o.activo = true
              AND o.estado IN ('DELIVERED', 'INVOICED', 'CLOSED')
              AND o.fecha_entrega_real IS NOT NULL
              AND o.fecha_entrega_real BETWEEN @Desde AND @Hasta
            GROUP BY c.id, c.nombre, c.tipo_cliente
            ORDER BY c.nombre ASC";

        return await _connection.QueryAsync<SlaReporteItemDto>(sql,
            new { EmpresaId = empresaId, Desde = desde, Hasta = hasta });
    }

    /// <summary>
    /// Elevación automática de prioridad (HU-029 CA-03). La BLL valida
    /// la transición de la FSM antes de llamar.
    /// </summary>
    public async Task<bool> UpdatePrioridadAsync(
        Guid ordenId, Guid empresaId, string prioridad)
    {
        const string sql = @"
            UPDATE ordenes
            SET prioridad = @Prioridad
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = ordenId, EmpresaId = empresaId, Prioridad = prioridad });
        return rows > 0;
    }
}