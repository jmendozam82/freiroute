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
    /// <summary>Empresa de tenant usada en los tokens por defecto de los tests.</summary>
    public static Guid EmpresaTenant { get; } = Guid.NewGuid();

    /// <summary>
    /// Token de SUPER_ADMIN (acceso global al SaaS). Es el único token que puede
    /// gestionar el módulo de empresas. TenantMiddleware no lo rechaza: como es
    /// SUPER_ADMIN no exige empresa_id (usa el header X-Empresa-Id si se necesita).
    /// </summary>
    public static string TokenSuperAdmin { get; } =
        GenerateTokenCore(Guid.NewGuid(), Guid.Empty, Array.Empty<string>(), "SUPER_ADMIN");

    /// <summary>
    /// Token de ADMIN de un tenant. Tiene permisos "estándar admin" sobre módulos
    /// de su empresa (embarques, usuarios) pero NO sobre 'configuracion' (módulo
    /// global del SaaS) → obtiene 403 en /api/empresas.
    /// </summary>
    public static string TokenAdmin { get; } =
        GenerateTokenCore(
            Guid.NewGuid(), EmpresaTenant,
            new[] { "embarques:read", "embarques:create", "embarques:update", "usuarios:read", "usuarios:create" },
            "ADMIN");

    /// <summary>
    /// Token de OPERADOR con SOLO lectura (módulos configuracion y usuarios).
    /// Pasa las operaciones de lectura pero obtiene 403 en create/update.
    /// </summary>
    public static string TokenSoloLectura { get; } =
        GenerateTokenCore(
            Guid.NewGuid(), EmpresaTenant,
            new[] { "configuracion:read", "usuarios:read" },
            "OPERADOR");

    /// <summary>
    /// Token de OPERADOR SIN ningún permiso declarado. Obtiene 403 en casi todo
    /// (RequirePermission no encuentra el claim).
    /// </summary>
    public static string TokenSinPermisos { get; } =
        GenerateTokenCore(Guid.NewGuid(), EmpresaTenant, Array.Empty<string>(), "OPERADOR");

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
        int expiresHours = 8) =>
        GenerateTokenCore(userId, empresaId, permisos, tipoUsuario, expiresHours);

    private static string GenerateTokenCore(
        Guid userId,
        Guid empresaId,
        string[] permisos,
        string tipoUsuario,
        int expiresHours = 8)
    {
        var claims = new List<Claim>
        {
            new Claim("user_id", userId.ToString()),
            new Claim("empresa_id", empresaId.ToString()),
            new Claim("tipo_usuario", tipoUsuario),
            new Claim("perfil_id", Guid.NewGuid().ToString()),
            new Claim("nombre", "Usuario de Prueba")
        };

        // Semántica ADR-007: UN claim "permisos" POR permiso, para que
        // User.FindAll("permisos") en RequirePermissionAttribute los vea.
        foreach (var permiso in permisos)
        {
            claims.Add(new Claim("permisos", permiso));
        }

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
    /// Genera un token de tenant ADMIN sin permiso de gestión de empresas
    /// (usa un permiso de otro módulo: "configuracion:read").
    /// </summary>
    public static string GenerateTenantAdminToken() =>
        GenerateTestToken(
            userId: Guid.NewGuid(),
            empresaId: Guid.NewGuid(),
            permisos: new[] { "configuracion:read" },
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
