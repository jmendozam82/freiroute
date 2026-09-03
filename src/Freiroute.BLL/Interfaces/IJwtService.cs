namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de generación de tokens de autenticación (HU-003, ADR-007).
/// - Access token JWT con claims: user_id, empresa_id, perfil_id, tipo_usuario,
///   nombre y un claim "permisos" por permiso ("modulo:read|create|update").
/// - Refresh token opaco (UUID aleatorio) del que SOLO se persiste el hash SHA-256.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera el access token JWT con los claims del ADR-007.
    /// Expiración configurable (JwtSettings.ExpiryHours, default 8 h).
    /// </summary>
    string GenerateAccessToken(
        Guid userId,
        Guid empresaId,
        Guid perfilId,
        string tipoUsuario,
        string nombre,
        IEnumerable<string> permisos);

    /// <summary>Genera un refresh token opaco (UUID aleatorio). Nunca se envía en el JWT.</summary>
    string GenerateRefreshToken();

    /// <summary>Hash SHA-256 (hex) del refresh token — único valor persistido en BD.</summary>
    string HashRefreshToken(string refreshToken);
}