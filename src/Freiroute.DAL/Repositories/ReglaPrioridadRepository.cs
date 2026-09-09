using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de reglas de prioridad (tabla 'reglas_prioridad', HU-029).
/// Reglas configurables por tenant para la elevación automática de
/// prioridad de órdenes (consumidas por PrioridadOrdenesJob, ADR-013).
/// ADR-003: todo método filtra por empresaId.
/// El nombre de la regla es único por empresa mientras esté activa
/// (índice parcial idx_reglas_prioridad_nombre_activo).
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class ReglaPrioridadRepository : IReglaPrioridadRepository
{
    // Columnas de 'reglas_prioridad' mapeadas a la entidad ReglaPrioridad (PascalCase).
    private const string Col = @"
        id                 AS Id,
        empresa_id         AS EmpresaId,
        nombre             AS Nombre,
        condicion          AS Condicion,
        horas_umbral       AS HorasUmbral,
        nivel_destino      AS NivelDestino,
        activo             AS Activo,
        fecha_creacion     AS FechaCreacion,
        fecha_modificacion AS FechaModificacion";

    private readonly IDbConnection _connection;

    public ReglaPrioridadRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Lista las reglas activas de la empresa ordenadas por nombre.
    /// Las consume el job de elevación automática de prioridad.
    /// </summary>
    public async Task<IEnumerable<ReglaPrioridad>> GetActivasByEmpresaAsync(Guid empresaId)
    {
        const string sql = $@"
            SELECT {Col}
            FROM reglas_prioridad
            WHERE empresa_id = @EmpresaId
              AND activo     = true
            ORDER BY nombre ASC";

        return await _connection.QueryAsync<ReglaPrioridad>(
            sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene una regla activa por Id dentro de la empresa.</summary>
    public async Task<ReglaPrioridad?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = $@"
            SELECT {Col}
            FROM reglas_prioridad
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<ReglaPrioridad>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar regla. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(ReglaPrioridad entity)
    {
        const string sql = @"
            INSERT INTO reglas_prioridad (
                empresa_id,
                nombre,
                condicion,
                horas_umbral,
                nivel_destino,
                activo
            ) VALUES (
                @EmpresaId,
                @Nombre,
                @Condicion,
                @HorasUmbral,
                @NivelDestino,
                @Activo
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    /// <summary>
    /// Actualiza la configuración de una regla activa de la empresa.
    /// fecha_modificacion se actualiza con el trigger estándar.
    /// </summary>
    public async Task UpdateAsync(ReglaPrioridad entity)
    {
        const string sql = @"
            UPDATE reglas_prioridad SET
                nombre        = @Nombre,
                condicion     = @Condicion,
                horas_umbral  = @HorasUmbral,
                nivel_destino = @NivelDestino
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        await _connection.ExecuteAsync(sql, entity);
    }

    /// <summary>Soft delete: SET activo = false (ADR-005).</summary>
    public async Task DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE reglas_prioridad
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
    }
}