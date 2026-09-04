namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de generación de tokens de autenticación (HU-003, ADR-007).
/// - Access token JWT con claims: user_id, empresa_id, perfil_id, tipo_usuario,
///   nombre y un claim "permisos" por permiso ("modulo:read|create|update").
/// - Refresh token opaco (UUID aleatorio) del que SOLO se persiste el hash SHA-256.
/// - Token temporal de corta vida (2FA) e impersonación (HU-005, HU-009 CA-05).
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

    /// <summary>
    /// Genera un access token de impersonación de tenant (HU-009 CA-05).
    /// Incluye el claim "impersonado_por" para trazabilidad del SUPER_ADMIN.
    /// </summary>
    string GenerateImpersonationToken(
        Guid userId,
        Guid empresaId,
        Guid perfilId,
        string tipoUsuario,
        string nombre,
        IEnumerable<string> permisos,
        Guid impersonadoPor,
        int expiryHours = 8);

    /// <summary>
    /// Genera un token temporal de corta vida (HU-005) con claims user_id y
    /// empresa_id. Se usa para el paso 1 del login cuando se requiere 2FA.
    /// </summary>
    string GenerateTempToken(Guid userId, Guid empresaId, int expiryMinutes = 5);

    /// <summary>
    /// Valida un token temporal de 2FA y devuelve el payload (user_id, empresa_id).
    /// Retorna null si el token es inválido, expirado o con firma incorrecta.
    /// </summary>
    TempTokenPayload? ValidateTempToken(string token);

    /// <summary>Genera un refresh token opaco (UUID aleatorio). Nunca se envía en el JWT.</summary>
    string GenerateRefreshToken();

    /// <summary>Hash SHA-256 (hex) del refresh token — único valor persistido en BD.</summary>
    string HashRefreshToken(string refreshToken);
}

/// <summary>
/// Payload de un token temporal de 2FA (HU-005) extraído al validarlo.
/// </summary>
public record TempTokenPayload(Guid UserId, Guid EmpresaId);
