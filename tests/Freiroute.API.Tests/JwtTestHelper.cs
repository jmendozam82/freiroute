namespace Freiroute.API.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Genera tokens JWT válidos para pruebas de integración del API de Freiroute TMS.
/// Incluye claims: user_id, empresa_id, tipo_usuario, permisos[].
/// La clave secreta es fija y conocida solo en ambiente de testing.
/// </summary>
public static class JwtTestHelper
{
    private const string TestSecret = "EstaEsUnaClaveSecretaParaTesting2026SoloDevLocal";
    private const string Issuer = "freiroute-api";
    private const string Audience = "freiroute-client";

    /// <summary>
    /// Genera un token JWT con los claims especificados.
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="empresaId">Identificador de la empresa (tenant)</param>
    /// <param name="permisos">Array de permisos en formato modulo:accion</param>
    /// <param name="tipoUsuario">Tipo de usuario: SUPER_ADMIN, ADMIN, OPERADOR, CONDUCTOR, CLIENTE</param>
    /// <param name="expiresHours">Horas hasta expiración (default 8)</param>
    public static string GenerateTestToken(
        Guid userId,
        Guid empresaId,
        string[] permisos,
        string tipoUsuario = "SUPER_ADMIN",
        int expiresHours = 8)
    {
        var claims = new List<Claim>
        {
            new Claim("user_id", userId.ToString()),
            new Claim("empresa_id", empresaId.ToString()),
            new Claim("tipo_usuario", tipoUsuario),
            new Claim("permisos", string.Join(",", permisos))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un token de SUPER_ADMIN con todos los permisos de empresas habilitados.
    /// </summary>
    public static string GenerateSuperAdminToken() =>
        GenerateTestToken(
            userId: Guid.NewGuid(),
            empresaId: Guid.Empty,
            permisos: new[] { "empresas:read", "empresas:create", "empresas:update" });

    /// <summary>
    /// Genera un token de tenant ADMIN sin permiso de gestión de empresas.
    /// </summary>
    public static string GenerateTenantAdminToken() =>
        GenerateTestToken(
            userId: Guid.NewGuid(),
            empresaId: Guid.NewGuid(),
            permisos: new[] { "empresastms:read" },
            tipoUsuario: "ADMIN");

    /// <summary>
    /// Genera un token de OPERADOR con permisos limitados.
    /// </summary>
    public static string GenerateOperatorToken(Guid empresaId) =>
        GenerateTestToken(
            userId: Guid.NewGuid(),
            empresaId: empresaId,
            permisos: new[] { "embarques:read", "embarques:create" },
            tipoUsuario: "OPERADOR",
            expiresHours: 1);
}
