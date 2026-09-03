using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'sesiones' (refresh tokens, HU-003 CA-02).
/// Migración: 20260101000006_tabla_sesiones.sql.
/// </summary>
public interface ISesionRepository
{
    /// <summary>Registra una sesión con el hash del refresh token. El UUID lo genera la BD.</summary>
    Task<Guid> CreateAsync(Sesion sesion);

    /// <summary>Obtiene una sesión por el hash del refresh token (lookup global — el hash es UNIQUE).</summary>
    Task<Sesion?> GetByRefreshTokenHashAsync(string refreshTokenHash);

    /// <summary>Revoca una sesión (activa = false) — logout o rotación de refresh token.</summary>
    Task<bool> RevocarAsync(Guid id);

    /// <summary>Revoca todas las sesiones activas de un usuario (HU-007 CA-06: invalidar sesiones tras reset).</summary>
    Task<bool> RevocarTodasPorUsuarioAsync(Guid usuarioId);
}