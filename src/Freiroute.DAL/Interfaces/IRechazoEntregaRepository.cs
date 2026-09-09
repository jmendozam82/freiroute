using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de rechazos de entrega (HU-030).
/// Corresponde a la tabla 'rechazos_entrega'. Todo método recibe
/// <paramref name="empresaId"/> extraído del JWT (ADR-003).
/// No existe DeleteAsync — solo soft delete (ADR-005).
/// </summary>
public interface IRechazoEntregaRepository
{
    /// <summary>Registra el rechazo. Retorna el Id generado en BD (gen_random_uuid).</summary>
    Task<Guid> CreateAsync(RechazoEntrega entity);

    /// <summary>Lista los rechazos de una orden (historial de incidencias de campo).</summary>
    Task<IEnumerable<RechazoEntrega>> GetByOrdenAsync(Guid ordenId, Guid empresaId);
}