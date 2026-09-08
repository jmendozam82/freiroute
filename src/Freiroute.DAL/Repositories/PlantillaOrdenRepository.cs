using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de plantillas de orden (HU-027) — creación rápida y
/// recurrencia programada (background job RecurrenciaOrdenesJob, ADR-013).
/// El campo datos_orden (JSONB) se almacena como string: PostgreSQL lo
/// valida/serializa al escribir y Dapper lo devuelve como texto al leer.
/// ADR-003: todo método filtra por empresa_id. No existe DeleteAsync (ADR-005).
/// </summary>
public class PlantillaOrdenRepository : IPlantillaOrdenRepository
{
    private const string ColPlantilla = @"
        id                       AS Id,
        empresa_id               AS EmpresaId,
        nombre                   AS Nombre,
        descripcion              AS Descripcion,
        datos_orden::text        AS DatosOrden,
        es_recurrente            AS EsRecurrente,
        frecuencia_recurrencia   AS FrecuenciaRecurrencia,
        proxima_ejecucion        AS ProximaEjecucion,
        activo                   AS Activo,
        fecha_creacion           AS FechaCreacion,
        fecha_modificacion       AS FechaModificacion,
        creado_por               AS CreadoPor";

    private readonly IDbConnection _connection;

    public PlantillaOrdenRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista las plantillas activas de la empresa. Selecciona solo los campos
    /// de listado (sin datos_orden — el snapshot JSON se carga en GetByIdAsync).
    /// </summary>
    public async Task<IEnumerable<PlantillaOrden>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                       AS Id,
                empresa_id               AS EmpresaId,
                nombre                   AS Nombre,
                descripcion              AS Descripcion,
                es_recurrente            AS EsRecurrente,
                frecuencia_recurrencia   AS FrecuenciaRecurrencia,
                proxima_ejecucion        AS ProximaEjecucion,
                activo                   AS Activo,
                fecha_creacion           AS FechaCreacion,
                fecha_modificacion       AS FechaModificacion,
                creado_por               AS CreadoPor
            FROM plantillas_orden
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<PlantillaOrden>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene una plantilla completa (incluye datos_orden) dentro de la empresa.</summary>
    public async Task<PlantillaOrden?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = $@"
            SELECT {ColPlantilla}
            FROM plantillas_orden
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<PlantillaOrden>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar plantilla. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(PlantillaOrden entidad)
    {
        const string sql = @"
            INSERT INTO plantillas_orden (
                empresa_id,
                nombre,
                descripcion,
                datos_orden,
                es_recurrente,
                frecuencia_recurrencia,
                proxima_ejecucion,
                activo,
                creado_por
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Descripcion,
                @DatosOrden::jsonb,
                @EsRecurrente,
                @FrecuenciaRecurrencia,
                @ProximaEjecucion,
                @Activo,
                @CreadoPor
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza una plantilla activa de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(PlantillaOrden entidad)
    {
        const string sql = @"
            UPDATE plantillas_orden SET
                nombre                  = @Nombre,
                descripcion             = @Descripcion,
                datos_orden             = @DatosOrden::jsonb,
                es_recurrente           = @EsRecurrente,
                frecuencia_recurrencia  = @FrecuenciaRecurrencia,
                proxima_ejecucion       = @ProximaEjecucion
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, entidad);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE plantillas_orden
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Plantillas recurrentes activas cuya próxima ejecución es &lt;= la fecha
    /// de referencia. Consulta del background job RecurrenciaOrdenesJob (HU-027).
    /// El índice parcial idx_plantillas_recurrentes acelera esta lectura.
    /// </summary>
    public async Task<IEnumerable<PlantillaOrden>> GetPlantillasParaEjecutarAsync(
        Guid empresaId, DateOnly fechaReferencia)
    {
        const string sql = $@"
            SELECT {ColPlantilla}
            FROM plantillas_orden
            WHERE empresa_id = @EmpresaId
              AND es_recurrente = true
              AND activo = true
              AND proxima_ejecucion <= @FechaReferencia
            ORDER BY proxima_ejecucion ASC";

        return await _connection.QueryAsync<PlantillaOrden>(sql,
            new { EmpresaId = empresaId, FechaReferencia = fechaReferencia });
    }

    /// <summary>
    /// Actualiza la próxima ejecución tras crear la orden recurrente (HU-027).
    /// La nueva fecha la calcula la BLL con CalcularProximaEjecucion().
    /// </summary>
    public async Task<bool> ActualizarProximaEjecucionAsync(
        Guid plantillaId, DateOnly nuevaFecha, Guid empresaId)
    {
        const string sql = @"
            UPDATE plantillas_orden
            SET proxima_ejecucion = @NuevaFecha
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = plantillaId, NuevaFecha = nuevaFecha, EmpresaId = empresaId });
        return rows > 0;
    }
}