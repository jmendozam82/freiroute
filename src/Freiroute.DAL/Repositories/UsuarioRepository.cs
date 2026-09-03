using System.Data;
using Dapper;
using Freiroute.Entity;
using Freiroute.DAL.Interfaces;

namespace Freiroute.DAL.Repositories;

/// <summary>
/// Repositorio de la tabla 'usuarios'.
/// ADR-003: todo método filtra por empresaId (capa 1 de aislamiento multi-tenant),
/// con UNA excepción: GetBySupabaseUserIdAsync no filtra por empresa para poder
/// resolver el tenant durante el login (HU-003).
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnection _connection;

    public UsuarioRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Obtiene los usuarios activos de una empresa.</summary>
    public async Task<IEnumerable<Usuario>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                tipo_identidad     AS TipoIdentidad,
                numero_identidad   AS NumeroIdentidad,
                nombre_completo    AS NombreCompleto,
                email              AS Email,
                telefono           AS Telefono,
                foto_url           AS FotoUrl,
                supabase_user_id   AS SupabaseUserId,
                tipo_usuario       AS TipoUsuario,
                estado             AS Estado,
                ultimo_acceso      AS UltimoAcceso,
                intentos_fallidos  AS IntentosFallidos,
                bloqueado_hasta    AS BloqueadoHasta,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM usuarios
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre_completo ASC";

        return await _connection.QueryAsync<Usuario>(sql, new { EmpresaId = empresaId });
    }

    /// <summary>Obtiene un usuario activo por Id dentro de la empresa.</summary>
    public async Task<Usuario?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                tipo_identidad     AS TipoIdentidad,
                numero_identidad   AS NumeroIdentidad,
                nombre_completo    AS NombreCompleto,
                email              AS Email,
                telefono           AS Telefono,
                foto_url           AS FotoUrl,
                supabase_user_id   AS SupabaseUserId,
                tipo_usuario       AS TipoUsuario,
                estado             AS Estado,
                ultimo_acceso      AS UltimoAcceso,
                intentos_fallidos  AS IntentosFallidos,
                bloqueado_hasta    AS BloqueadoHasta,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM usuarios
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    /// <summary>Obtiene un usuario activo por email dentro de la empresa (login, HU-003).</summary>
    public async Task<Usuario?> GetByEmailAsync(string email, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                tipo_identidad     AS TipoIdentidad,
                numero_identidad   AS NumeroIdentidad,
                nombre_completo    AS NombreCompleto,
                email              AS Email,
                telefono           AS Telefono,
                foto_url           AS FotoUrl,
                supabase_user_id   AS SupabaseUserId,
                tipo_usuario       AS TipoUsuario,
                estado             AS Estado,
                ultimo_acceso      AS UltimoAcceso,
                intentos_fallidos  AS IntentosFallidos,
                bloqueado_hasta    AS BloqueadoHasta,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM usuarios
            WHERE LOWER(email) = LOWER(@Email)
              AND empresa_id = @EmpresaId
              AND activo = true
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { Email = email, EmpresaId = empresaId });
    }

    /// <summary>
    /// Obtiene un usuario por su vínculo con Supabase Auth (OAuth / SSO, HU-004).
    /// EXCEPCIÓN ADR-003 deliberada: NO filtra por empresa_id porque se usa para
    /// resolver el tenant durante el login, antes de autenticar al usuario.
    /// </summary>
    public async Task<Usuario?> GetBySupabaseUserIdAsync(Guid supabaseUserId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                perfil_id          AS PerfilId,
                tipo_identidad     AS TipoIdentidad,
                numero_identidad   AS NumeroIdentidad,
                nombre_completo    AS NombreCompleto,
                email              AS Email,
                telefono           AS Telefono,
                foto_url           AS FotoUrl,
                supabase_user_id   AS SupabaseUserId,
                tipo_usuario       AS TipoUsuario,
                estado             AS Estado,
                ultimo_acceso      AS UltimoAcceso,
                intentos_fallidos  AS IntentosFallidos,
                bloqueado_hasta    AS BloqueadoHasta,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM usuarios
            WHERE supabase_user_id = @SupabaseUserId
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { SupabaseUserId = supabaseUserId });
    }

    /// <summary>
    /// Obtiene un usuario por email SIN filtrar por empresa y SIN filtrar por activo
    /// (HU-003 — login). EXCEPCIÓN ADR-003 deliberada: resuelve el tenant antes de
    /// autenticar. No filtra por activo porque AuthService necesita ver usuarios
    /// PENDING/SUSPENDED para retornar el mensaje de estado correcto (CA-07).
    /// </summary>
    public async Task<Usuario?> GetByEmailGlobalAsync(string email)
    {
        const string sql = @"
            SELECT
                id                  AS Id,
                empresa_id          AS EmpresaId,
                perfil_id           AS PerfilId,
                tipo_identidad      AS TipoIdentidad,
                numero_identidad    AS NumeroIdentidad,
                nombre_completo     AS NombreCompleto,
                email               AS Email,
                telefono            AS Telefono,
                foto_url            AS FotoUrl,
                supabase_user_id    AS SupabaseUserId,
                tipo_usuario        AS TipoUsuario,
                estado              AS Estado,
                ultimo_acceso       AS UltimoAcceso,
                intentos_fallidos   AS IntentosFallidos,
                bloqueado_hasta     AS BloqueadoHasta,
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
            FROM usuarios
            WHERE email = @Email
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { Email = email });
    }

    /// <summary>Insertar un usuario. El UUID lo genera la BD (gen_random_uuid).</summary>
    public async Task<Guid> CreateAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO usuarios (
                empresa_id,
                perfil_id,
                tipo_identidad,
                numero_identidad,
                nombre_completo,
                email,
                telefono,
                foto_url,
                supabase_user_id,
                tipo_usuario,
                estado,
                ultimo_acceso,
                intentos_fallidos,
                bloqueado_hasta
            ) VALUES (
                @EmpresaId,
                @PerfilId,
                @TipoIdentidad,
                @NumeroIdentidad,
                @NombreCompleto,
                @Email,
                @Telefono,
                @FotoUrl,
                @SupabaseUserId,
                @TipoUsuario,
                @Estado,
                @UltimoAcceso,
                @IntentosFallidos,
                @BloqueadoHasta
            )
            RETURNING id";

        return await _connection.ExecuteScalarAsync<Guid>(sql, usuario);
    }

    /// <summary>
    /// Actualiza un usuario activo de la empresa. Incluye actualizaciones de
    /// seguridad de cuenta: ultimo_acceso, intentos_fallidos, bloqueado_hasta, estado.
    /// </summary>
    public async Task<bool> UpdateAsync(Usuario usuario)
    {
        const string sql = @"
            UPDATE usuarios SET
                perfil_id         = @PerfilId,
                tipo_identidad    = @TipoIdentidad,
                numero_identidad  = @NumeroIdentidad,
                nombre_completo   = @NombreCompleto,
                email             = @Email,
                telefono          = @Telefono,
                foto_url          = @FotoUrl,
                supabase_user_id  = @SupabaseUserId,
                tipo_usuario      = @TipoUsuario,
                estado            = @Estado,
                ultimo_acceso     = @UltimoAcceso,
                intentos_fallidos = @IntentosFallidos,
                bloqueado_hasta   = @BloqueadoHasta
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        var rows = await _connection.ExecuteAsync(sql, usuario);
        return rows > 0;
    }

    /// <summary>Soft delete: SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId.</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE usuarios
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }

    // ── Seguridad de cuenta (HU-003 CA-04/05/06) ──────────────────

    /// <summary>Actualiza solo el campo ultimo_acceso = NOW() tras un login exitoso (CA-05).</summary>
    public async Task ActualizarUltimoAccesoAsync(Guid id)
    {
        const string sql = @"
            UPDATE usuarios
            SET ultimo_acceso = NOW()
            WHERE id = @Id
              AND activo = true";

        await _connection.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>Incrementa intentos_fallidos en 1 tras un login fallido (CA-06).</summary>
    public async Task IncrementarIntentosFallidosAsync(Guid id)
    {
        const string sql = @"
            UPDATE usuarios
            SET intentos_fallidos = intentos_fallidos + 1
            WHERE id = @Id
              AND activo = true";

        await _connection.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>Bloquea la cuenta: SET bloqueado_hasta = NOW() + 30 min tras 5 intentos fallidos (CA-04).</summary>
    public async Task BloquearHastaAsync(Guid id, DateTime bloqueadoHasta)
    {
        const string sql = @"
            UPDATE usuarios
            SET bloqueado_hasta = @BloqueadoHasta,
                estado          = 'LOCKED'
            WHERE id = @Id
              AND activo = true";

        await _connection.ExecuteAsync(sql, new { Id = id, BloqueadoHasta = bloqueadoHasta });
    }

    /// <summary>Resetea intentos_fallidos a 0 tras un login exitoso (CA-05).</summary>
    public async Task ResetearIntentosFallidosAsync(Guid id)
    {
        const string sql = @"
            UPDATE usuarios
            SET intentos_fallidos = 0,
                bloqueado_hasta   = NULL,
                estado            = 'ACTIVE'
            WHERE id = @Id
              AND activo = true";

        await _connection.ExecuteAsync(sql, new { Id = id });
    }
}