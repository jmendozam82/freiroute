using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de shipments (HU-025) — esqueleto mínimo del Sprint 4.
/// Solo las operaciones necesarias para la consolidación/desconsolidación
/// de órdenes (EP-04). El módulo completo de Shipment Planning se
/// implementa en Sprint 7 (EP-06).
/// ADR-003: todo método filtra por empresa_id. No existe DeleteAsync (ADR-005).
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly IDbConnection _connection;

    public ShipmentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene un shipment activo por Id dentro de la empresa.</summary>
    public async Task<Shipment?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                    AS Id,
                empresa_id            AS EmpresaId,
                numero_shipment       AS NumeroShipment,
                estado                AS Estado,
                activo                AS Activo,
                fecha_creacion        AS FechaCreacion,
                fecha_modificacion    AS FechaModificacion
            FROM shipments
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Shipment>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Insertar shipment en estado PLANNED. El UUID lo genera la BD.</summary>
    public async Task<Guid> CreateAsync(Shipment entidad)
    {
        const string sql = @"
            INSERT INTO shipments (
                empresa_id,
                numero_shipment,
                estado,
                activo
            ) VALUES (
                @EmpresaId,
                @NumeroShipment,
                'PLANNED',
                @Activo
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    /// <summary>Actualiza un shipment activo de la empresa. Retorna true si afectó una fila.</summary>
    public async Task<bool> UpdateAsync(Shipment entidad)
    {
        const string sql = @"
            UPDATE shipments SET
                numero_shipment = @NumeroShipment,
                estado          = @Estado
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
            UPDATE shipments
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql,
            new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    /// <summary>
    /// Conteo de órdenes activas asignadas a un shipment (HU-025).
    /// La BLL lo usa para decidir consolidación vs. desconsolidación.
    /// </summary>
    public async Task<int> ContarOrdenesAsync(Guid shipmentId, Guid empresaId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM ordenes
            WHERE shipment_id = @ShipmentId
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.ExecuteScalarAsync<int>(sql,
            new { ShipmentId = shipmentId, EmpresaId = empresaId });
    }
}