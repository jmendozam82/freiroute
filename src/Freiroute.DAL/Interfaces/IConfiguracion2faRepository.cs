using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la configuración 2FA del usuario
/// y de los códigos temporales de 2FA (HU-005).
/// </summary>
public interface IConfiguracion2faRepository
{
    // ── Configuración 2FA ─────────────────────────────────────────

    /// <summary>Obtiene la configuración 2FA de un usuario dentro de su empresa.</summary>
    Task<Configuracion2fa?> GetByUsuarioIdAsync(Guid usuarioId, Guid empresaId);

    /// <summary>Insertar la configuración 2FA de un usuario.</summary>
    Task<Guid> CreateAsync(Configuracion2fa entidad);

    /// <summary>Actualiza la configuración 2FA (secret, flags, códigos de recuperación).</summary>
    Task<bool> UpdateAsync(Configuracion2fa entidad);

    /// <summary>Desactiva el 2FA de un usuario: SET activo = false.</summary>
    Task<bool> DeactivateAsync(Guid usuarioId, Guid empresaId);

    // ── Códigos temporales ────────────────────────────────────────

    /// <summary>Crea un código temporal de 2FA. El UUID lo genera la BD.</summary>
    Task<Guid> CrearCodigoTemporalAsync(Codigo2faTempora entidad);

    /// <summary>
    /// Obtiene un código temporal válido (no usado, no expirado) por usuario y hash.
    /// Devuelve null si no es válido.
    /// </summary>
    Task<Codigo2faTempora?> GetCodigoTemporalValidoAsync(Guid usuarioId, string codigoHash);

    /// <summary>Marca un código temporal como usado (un solo uso).</summary>
    Task<bool> MarcarCodigoUsadoAsync(Guid codigoId);

    /// <summary>Elimina los códigos temporales vencidos. Llamado por el job de vencimientos.</summary>
    Task PurgarCodigosExpiradosAsync();
}
