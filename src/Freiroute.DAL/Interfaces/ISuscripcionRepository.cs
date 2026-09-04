using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'suscripciones'.
/// NO recibe empresaId en GetAll — el Super Admin ve TODAS las suscripciones (ADR-004).
/// Una empresa tiene UNA suscripción activa a la vez.
/// </summary>
public interface ISuscripcionRepository
{
    /// <summary>
    /// Obtiene las suscripciones paginadas, con filtro opcional por estado.
    /// Usado por el panel del Super Admin (HU-011).
    /// </summary>
    Task<IEnumerable<Suscripcion>> GetAllAsync(
        string? estado = null, int pageNumber = 1, int pageSize = 20);

    /// <summary>Obtiene una suscripción por su Id.</summary>
    Task<Suscripcion?> GetByIdAsync(Guid id);

    /// <summary>
    /// Obtiene la suscripción ACTIVA de una empresa (clave para validar límites del plan).
    /// Devuelve la que tiene activo = true y estado != CANCELLED.
    /// </summary>
    Task<Suscripcion?> GetActivaByEmpresaIdAsync(Guid empresaId);

    /// <summary>Insertar una suscripción nueva. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Suscripcion entidad);

    /// <summary>Actualiza una suscripción (fecha_vencimiento, estado, etc.).</summary>
    Task<bool> UpdateAsync(Suscripcion entidad);

    /// <summary>
    /// Suscripciones activas cuyo vencimiento está dentro de @diasUmbral.
    /// Para alertas de vencimiento al Super Admin (HU-011 CA-04).
    /// </summary>
    Task<IEnumerable<Suscripcion>> GetProximasAVencerAsync(int diasUmbral);

    /// <summary>
    /// Suscripciones en PAST_DUE que llevan más de @diasGracia en ese estado.
    /// Para el job que las pasa a SUSPENDED (HU-011 CA-06).
    /// </summary>
    Task<IEnumerable<Suscripcion>> GetVencidasEnGraciaAsync(int diasGracia);
}
