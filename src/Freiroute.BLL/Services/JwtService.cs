using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Freiroute.BLL.Services;

/// <summary>
/// Implementación de generación de tokens (HU-003, ADR-007).
/// Access token: JWT firmado HMAC-SHA256 con los claims del ADR-007
/// (user_id, empresa_id, perfil_id, tipo_usuario, nombre, permisos[]).
/// Refresh token: UUID aleatorio opaco; solo se persiste el hash SHA-256.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>Genera el access token JWT con los claims del ADR-007.</summary>
    public string GenerateAccessToken(
        Guid userId,
        Guid empresaId,
        Guid perfilId,
        string tipoUsuario,
        string nombre,
        IEnumerable<string> permisos)
    {
        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new("empresa_id", empresaId.ToString()),
            new("perfil_id", perfilId.ToString()),
            new("tipo_usuario", tipoUsuario),
            new("nombre", nombre)
        };

        // Un claim "permisos" por permiso → RequirePermissionAttribute los lee
        // con User.FindAll("permisos") (HU-006 CA-05: el cambio aplica sin
        // reiniciar el servidor — se serializan en cada login).
        foreach (var permiso in permisos)
        {
            claims.Add(new Claim("permisos", permiso));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un access token de impersonación de tenant (HU-009 CA-05).
    /// Incluye el claim "impersonado_por" con el Id del SUPER_ADMIN que impersona
    /// para trazabilidad en AuditoriaService.
    /// </summary>
    public string GenerateImpersonationToken(
        Guid userId,
        Guid empresaId,
        Guid perfilId,
        string tipoUsuario,
        string nombre,
        IEnumerable<string> permisos,
        Guid impersonadoPor,
        int expiryHours = 8)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new("empresa_id", empresaId.ToString()),
            new("perfil_id", perfilId.ToString()),
            new("tipo_usuario", tipoUsuario),
            new("nombre", nombre),
            new("impersonado_por", impersonadoPor.ToString())
        };

        foreach (var permiso in permisos)
        {
            claims.Add(new Claim("permisos", permiso));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Genera un token temporal de corta vida para 2FA (HU-005).</summary>
    public string GenerateTempToken(Guid userId, Guid empresaId, int expiryMinutes = 5)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("empresa_id", empresaId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Valida un token temporal de 2FA y extrae su payload.</summary>
    public TempTokenPayload? ValidateTempToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwt = validatedToken as JwtSecurityToken;
            var userId = jwt?.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            var empresaId = jwt?.Claims.FirstOrDefault(c => c.Type == "empresa_id")?.Value;

            if (userId is null ||
                !Guid.TryParse(userId, out var parsedUserId) ||
                !Guid.TryParse(empresaId, out var parsedEmpresaId))
            {
                return null;
            }

            return new TempTokenPayload(parsedUserId, parsedEmpresaId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Genera un refresh token opaco (UUID aleatorio de 32 hex — sin guiones).</summary>
    public string GenerateRefreshToken() => Guid.NewGuid().ToString("N");

    /// <summary>Hash SHA-256 (hex) del refresh token — único valor persistido en BD.</summary>
    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}