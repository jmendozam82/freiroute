using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio del catálogo 'clientes' (shippers) y sus contactos
/// (tabla 'contactos_cliente', HU-019, ADR-003, ADR-017).
/// ADR-003: todo método filtra por empresaId.
/// La unicidad de RUC/NIT se valida con ExisteRucAsync (HU-019 CA-08) —
/// no hay constraint UNIQUE a nivel BD porque es por empresa y el RUC
/// es opcional. La importación CSV (ADR-017) usa este método por fila.
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class ClienteRepository : IClienteRepository
{
    // Columnas de 'clientes' mapeadas a la entidad Cliente (PascalCase).
    private const string Col = @"
        id                   AS Id,
        empresa_id           AS EmpresaId,
        nombre               AS Nombre,
        nombre_comercial     AS NombreComercial,
        ruc_nit              AS RucNit,
        tipo_documento       AS TipoDocumento,
        tipo_cliente         AS TipoCliente,
        industria            AS Industria,
        email                AS Email,
        telefono             AS Telefono,
        sitio_web            AS SitioWeb,
        direccion_fiscal     AS DireccionFiscal,
        pais                 AS Pais,
        departamento         AS Departamento,
        ciudad               AS Ciudad,
        ubicacion_defecto_id AS UbicacionDefectoId,
        credito_dias         AS CreditoDias,
        limite_credito       AS LimiteCredito,
        moneda               AS Moneda,
        estado_credito       AS EstadoCredito,
        sla_dias_entrega     AS SlaDiasEntrega,
        sla_ventana_inicio   AS SlaVentanaInicio,
        sla_ventana_fin      AS SlaVentanaFin,
        activo               AS Activo,
        fecha_creacion       AS FechaCreacion,
        fecha_modificacion   AS FechaModificacion";

    private readonly IDbConnection _connection;

    public ClienteRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista clientes activos de la empresa con filtros opcionales por tipo
    /// de cliente (REGULAR, VIP, ...), estado de crédito (AL_DIA, BLOQUEADO, ...)
    /// y texto de búsqueda (nombre, RUC/NIT, ciudad). La paginación del listado
    /// (RNF-01.4) se resuelve en la capa BLL/Controller.
    /// </summary>
    public async Task<IEnumerable<Cliente>> GetAllAsync(
        Guid empresaId, string? tipoCliente = null,
        string? estadoCredito = null, string? q = null)
    {
        var sql = $@"
            SELECT {Col}
            FROM clientes
            WHERE empresa_id = @EmpresaId
              AND activo = true";

        if (!string.IsNullOrWhiteSpace(tipoCliente))
        {
            sql += " AND tipo_cliente = @TipoCliente";
        }

        if (!string.IsNullOrWhiteSpace(estadoCredito))
        {
            sql += " AND estado_credito = @EstadoCredito";
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            sql += @" AND (
                nombre        ILIKE '%' || @Q || '%'
                OR nombre_comercial ILIKE '%' || @Q || '%'
                OR ruc_nit     ILIKE '%' || @Q || '%'
                OR ciudad      ILIKE '%' || @Q || '%'
            )";
        }

        sql += " ORDER BY nombre ASC";

        return await _connection.QueryAsync<Cliente>(sql, new
        {
            EmpresaId = empresaId,
            TipoCliente = tipoCliente,
            EstadoCredito = estadoCredito,
            Q = q
        });
    }

    /// <summary>Obtiene un cliente activo por Id dentro de la empresa.</summary>
    public async Task<Cliente?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                   AS Id,
                empresa_id           AS EmpresaId,
                nombre               AS Nombre,
                nombre_comercial     AS NombreComercial,
                ruc_nit              AS RucNit,
                tipo_documento       AS TipoDocumento,
                tipo_cliente         AS TipoCliente,
                industria            AS Industria,
                email                AS Email,
                telefono             AS Telefono,
                sitio_web            AS SitioWeb,
                direccion_fiscal     AS DireccionFiscal,
                pais                 AS Pais,
                departamento         AS Departamento,
                ciudad               AS Ciudad,
                ubicacion_defecto_id AS UbicacionDefectoId,
                credito_dias         AS CreditoDias,
                limite_credito       AS LimiteCredito,
                moneda               AS Moneda,
                estado_credito       AS EstadoCredito,
                sla_dias_entrega     AS SlaDiasEntrega,
                sla_ventana_inicio   AS SlaVentanaInicio,
                sla_ventana_fin      AS SlaVentanaFin,
                activo               AS Activo,
                fecha_creacion       AS FechaCreacion,
                fecha_modificacion   AS FechaModificacion
            FROM clientes
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Cliente>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>
    /// true si ya existe un cliente con el RUC/NIT en la empresa
    /// (HU-019 CA-08 — importación CSV y alta/edición). excludeId permite
    /// excluir el propio cliente en un UPDATE. Comparación case-insensitive
    /// y tolerante a RUC/NIT nulos.
    /// </summary>
    public async Task<bool> ExisteRucAsync(string rucNit, Guid empresaId,
        Guid? excludeId = null)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1
                FROM clientes
                WHERE empresa_id = @EmpresaId
                  AND LOWER(COALESCE(ruc_nit, '')) = LOWER(@RucNit)
                  AND (@ExcludeId IS NULL OR id != @ExcludeId)
            )";

        return await _connection.ExecuteScalarAsync<bool>(sql, new
        {
            EmpresaId = empresaId,
            RucNit = rucNit,
            ExcludeId = excludeId
        });
    }

    /// <summary>Insertar cliente. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Cliente entidad)
    {
        const string sql = @"
            INSERT INTO clientes (
                empresa_id,
                nombre,
                nombre_comercial,
                ruc_nit,
                tipo_documento,
                tipo_cliente,
                industria,
                email,
                telefono,
                sitio_web,
                direccion_fiscal,
                pais,
                departamento,
                ciudad,
                ubicacion_defecto_id,
                credito_dias,
                limite_credito,
                moneda,
                estado_credito,
                sla_dias_entrega,
                sla_ventana_inicio,
                sla_ventana_fin
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @NombreComercial,
                @RucNit,
                @TipoDocumento,
                @TipoCliente,
                @Industria,
                @Email,
                @Telefono,
                @SitioWeb,
                @DireccionFiscal,
                @Pais,
                @Departamento,
                @Ciudad,
                @UbicacionDefectoId,
                @CreditoDias,
                @LimiteCredito,
                @Moneda,
                @EstadoCredito,
                @SlaDiasEntrega,
                @SlaVentanaInicio,
                @SlaVentanaFin
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un cliente activo de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(Cliente entidad)
    {
        const string sql = @"
            UPDATE clientes SET
                nombre               = @Nombre,
                nombre_comercial     = @NombreComercial,
                ruc_nit              = @RucNit,
                tipo_documento       = @TipoDocumento,
                tipo_cliente         = @TipoCliente,
                industria            = @Industria,
                email                = @Email,
                telefono             = @Telefono,
                sitio_web            = @SitioWeb,
                direccion_fiscal     = @DireccionFiscal,
                pais                 = @Pais,
                departamento         = @Departamento,
                ciudad               = @Ciudad,
                ubicacion_defecto_id = @UbicacionDefectoId,
                credito_dias         = @CreditoDias,
                limite_credito       = @LimiteCredito,
                moneda               = @Moneda,
                estado_credito       = @EstadoCredito,
                sla_dias_entrega     = @SlaDiasEntrega,
                sla_ventana_inicio   = @SlaVentanaInicio,
                sla_ventana_fin      = @SlaVentanaFin
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false (ADR-005). Nunca se borra físicamente.</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE clientes
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Cambia solo el estado de crédito (HU-019 CA-05). Los clientes con
    /// estado BLOQUEADO muestran alerta visual en toda la UI.
    /// </summary>
    public async Task<bool> ActualizarEstadoCreditoAsync(
        Guid id, string nuevoEstado, Guid empresaId)
    {
        const string sql = @"
            UPDATE clientes
            SET estado_credito = @NuevoEstado
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, NuevoEstado = nuevoEstado, EmpresaId = empresaId });
        return rows > 0;
    }

    // ── Contactos del cliente (HU-019 CA-02) ─────────────────────

    /// <summary>
    /// Lista los contactos ACTIVOS de un cliente de la empresa (HU-019 CA-02).
    /// El contacto principal (es_principal = true) se lista primero.
    /// </summary>
    public async Task<IEnumerable<ContactoCliente>> GetContactosAsync(
        Guid clienteId, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id              AS Id,
                empresa_id      AS EmpresaId,
                cliente_id      AS ClienteId,
                nombre          AS Nombre,
                cargo           AS Cargo,
                rol             AS Rol,
                email           AS Email,
                telefono        AS Telefono,
                es_principal    AS EsPrincipal,
                activo          AS Activo,
                fecha_creacion  AS FechaCreacion
            FROM contactos_cliente
            WHERE cliente_id = @ClienteId
              AND empresa_id = @EmpresaId
              AND activo = true
            ORDER BY es_principal DESC, nombre ASC";

        return await _connection.QueryAsync<ContactoCliente>(
            sql, new { ClienteId = clienteId, EmpresaId = empresaId });
    }

    /// <summary>Insertar contacto de un cliente. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateContactoAsync(ContactoCliente entidad)
    {
        const string sql = @"
            INSERT INTO contactos_cliente (
                empresa_id,
                cliente_id,
                nombre,
                cargo,
                rol,
                email,
                telefono,
                es_principal
            ) VALUES (
                @EmpresaId,
                @ClienteId,
                @Nombre,
                @Cargo,
                @Rol,
                @Email,
                @Telefono,
                @EsPrincipal
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un contacto de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateContactoAsync(ContactoCliente entidad)
    {
        const string sql = @"
            UPDATE contactos_cliente SET
                nombre       = @Nombre,
                cargo        = @Cargo,
                rol          = @Rol,
                email        = @Email,
                telefono     = @Telefono,
                es_principal = @EsPrincipal
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete de un contacto: SET activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateContactoAsync(Guid contactoId, Guid empresaId)
    {
        const string sql = @"
            UPDATE contactos_cliente
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = contactoId, EmpresaId = empresaId });
        return rows > 0;
    }
}