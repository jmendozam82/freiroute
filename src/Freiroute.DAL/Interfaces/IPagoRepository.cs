using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'pagos'.
/// INMUTABLE — solo CreateAsync y consultas. NO existe UpdateAsync ni DeactivateAsync (ADR-004).
/// </summary>
public interface IPagoRepository
{
    /// <summary>Obtiene todos los pagos de una suscripción (historial, HU-011 CA-08).</summary>
    Task<IEnumerable<Pago>> GetBySuscripcionIdAsync(Guid suscripcionId);

    /// <summary>Obtiene los pagos de una empresa (paginado, panel del Super Admin).</summary>
    Task<IEnumerable<Pago>> GetByEmpresaIdAsync(Guid empresaId,
        int pageNumber = 1, int pageSize = 20);

    /// <summary>Registrar un pago. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Pago entidad);

    /// <summary>
    /// MRR — suma de los precios pactados de las suscripciones ACTIVE (mensual normalizado).
    /// Para el dashboard financiero (HU-011 CA-09).
    /// </summary>
    Task<decimal> GetMrrAsync();

    /// <summary>
    /// Suma de los pagos COMPLETED registrados en el mes/año indicado.
    /// Para el dashboard financiero (HU-011 CA-09).
    /// </summary>
    Task<decimal> GetIngresosDelMesAsync(int año, int mes);
}
