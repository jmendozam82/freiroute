using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de rechazos de entrega (tabla 'rechazos_entrega', HU-030).
/// Relación N:1 con ordenes — una orden puede acumular múltiples rechazos
/// a lo largo de su ciclo de vida (re-entregas).
/// ADR-003: todo método filtra por empresaId.
/// No existe DeleteAsync: soft delete (ADR-005).
/// </summary>
public class RechazoEntregaRepository : IRechazoEntregaRepository
{
    // Columnas de 'rechazos_entrega' mapeadas a la entidad RechazoEntrega (PascalCase).
    private const string Col = @"
        id             AS Id,
        empresa_id     AS EmpresaId,
        orden_id       AS OrdenId,
        motivo         AS Motivo,
        descripcion    AS Descripcion,
        usuario_id     AS UsuarioId,
        fecha_creacion AS FechaCreacion,
        activo         AS Activo";

    private readonly IDbConnection _connection;

    public RechazoEntregaRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Registra el rechazo en campo. El UUID lo genera la BD
    /// (gen_random_uuid). El movimiento de la orden a FAILED_DELIVERY
    /// lo ejecuta la BLL vía la FSM (ADR-019) en la misma operación.
    /// </summary>
    public async Task<Guid> CreateAsync(RechazoEntrega entity)
    {
        const string sql = @"
            INSERT INTO rechazos_entrega (
                empresa_id,
                orden_id,
                motivo,
                descripcion,
                usuario_id
            ) VALUES (
                @EmpresaId,
                @OrdenId,
                @Motivo,
                @Descripcion,
                @UsuarioId
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    /// <summary>
    /// Lista los rechazos activos de una orden (historial de incidencias)
    /// del más reciente al más antiguo.
    /// </summary>
    public async Task<IEnumerable<RechazoEntrega>> GetByOrdenAsync(
        Guid ordenId, Guid empresaId)
    {
        const string sql = $@"
            SELECT {Col}
            FROM rechazos_entrega
            WHERE empresa_id = @EmpresaId
              AND orden_id   = @OrdenId
              AND activo     = true
            ORDER BY fecha_creacion DESC";

        return await _connection.QueryAsync<RechazoEntrega>(
            sql, new { EmpresaId = empresaId, OrdenId = ordenId });
    }
}