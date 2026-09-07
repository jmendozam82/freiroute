using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos del catálogo de clientes (shippers) y
/// sus contactos (HU-019, ADR-003). Todo método recibe
/// <paramref name="empresaId"/> extraído del JWT. No existe DeleteAsync.
/// </summary>
public interface IClienteRepository
{
    /// <summary>
    /// Lista clientes de la empresa con filtros opcionales por tipo,
    /// estado de crédito y texto (nombre, RUC/NIT, ciudad).
    /// </summary>
    Task<IEnumerable<Cliente>> GetAllAsync(Guid empresaId,
        string? tipoCliente = null, string? estadoCredito = null,
        string? q = null);

    /// <summary>Obtiene un cliente por Id dentro de la empresa.</summary>
    Task<Cliente?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// true si ya existe un cliente con el RUC/NIT en la empresa
    /// (HU-019 CA-08). excludeId permite excluir el propio cliente en Update.
    /// </summary>
    Task<bool> ExisteRucAsync(string rucNit, Guid empresaId,
        Guid? excludeId = null);

    /// <summary>Crea el cliente. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(Cliente entidad);

    /// <summary>Actualiza el cliente de la empresa. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateAsync(Cliente entidad);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Cambia solo el estado de crédito (HU-019 CA-05).
    /// Los clientes BLOQUEADO muestran alerta visual en toda la UI.
    /// </summary>
    Task<bool> ActualizarEstadoCreditoAsync(Guid id,
        string nuevoEstado, Guid empresaId);

    // ── Contactos ──────────────────────────────────────────────

    /// <summary>Lista los contactos de un cliente de la empresa.</summary>
    Task<IEnumerable<ContactoCliente>> GetContactosAsync(
        Guid clienteId, Guid empresaId);

    /// <summary>Crea un contacto. Retorna el Id generado en BD.</summary>
    Task<Guid> CreateContactoAsync(ContactoCliente entidad);

    /// <summary>Actualiza un contacto. Retorna true si afectó una fila.</summary>
    Task<bool> UpdateContactoAsync(ContactoCliente entidad);

    /// <summary>Soft delete de un contacto: activo = false.</summary>
    Task<bool> DeactivateContactoAsync(Guid contactoId, Guid empresaId);
}