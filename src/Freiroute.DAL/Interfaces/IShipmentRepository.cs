using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de shipments (HU-025, esqueleto mínimo Sprint 4).
/// El módulo completo de Shipment Planning se implementa en Sprint 7 (EP-06).
/// Aquí solo se incluyen las operaciones necesarias para la consolidación
/// y desconsolidación de órdenes.
/// Todo método recibe <paramref name="empresaId"/> extraído del JWT.
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IShipmentRepository
{
    /// <summary>Obtiene un shipment por Id dentro de la empresa.</summary>
    Task<Shipment?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>Crea un shipment en estado PLANNED. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateAsync(Shipment entidad);

    /// <summary>Actualiza el shipment. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(Shipment entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>Obtiene el conteo de órdenes asignadas a un shipment.</summary>
    Task<int> ContarOrdenesAsync(Guid shipmentId, Guid empresaId);
}
