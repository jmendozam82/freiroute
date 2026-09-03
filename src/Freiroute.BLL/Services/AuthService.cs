using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de autenticación (HU-003, HU-007). Servicio más crítico
/// del Sprint 1:
/// - Login: resuelve el tenant por email (GetByEmailGlobalAsync), valida estado
///   y bloqueos, verifica credenciales contra Supabase Auth, genera JWT con los
///   claims del ADR-007 y persiste un refresh token (hash SHA-256) en 'sesiones'.
/// - Refresh: rota el refresh token y emite un nuevo access token.
/// - Logout: invalida el refresh token.
/// - Forgot/Reset password: token de un solo uso de 30 min en 'invitaciones'.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IInvitacionRepository _invitacionRepository;
    private readonly ISesionRepository _sesionRepository;
    private readonly ISupabaseAuthService _supabaseAuth;
    private readonly IJwtService _jwtService;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPermisoRepository permisoRepository,
        IEmpresaRepository empresaRepository,
        IInvitacionRepository invitacionRepository,
        ISesionRepository sesionRepository,
        ISupabaseAuthService supabaseAuth,
        IJwtService jwtService,
        IAuditoriaService auditoria,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<JwtSettings> jwtSettings,
        IOptions<AppSettings> appSettings,
        ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _permisoRepository = permisoRepository;
        _empresaRepository = empresaRepository;
        _invitacionRepository = invitacionRepository;
        _sesionRepository = sesionRepository;
        _supabaseAuth = supabaseAuth;
        _jwtService = jwtService;
        _auditoria = auditoria;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtSettings.Value;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    // ── Login (HU-003) ──────────────────────────────────────────────

    /// <summary>
    /// Inicia sesión con email y contraseña (HU-003).
    /// Registra LOGIN o LOGIN_FAILED en auditoría (HU-003 CA-08, HU-008 CA-01).
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var (ipAddress, userAgent) = GetRequestContext();

        // 1. Resolver el tenant por email (sin empresa_id — GetByEmailGlobalAsync).
        var usuario = await _usuarioRepository.GetByEmailGlobalAsync(request.Email);

        if (usuario is null)
        {
            // Mensaje genérico — no revelar si el email existe o no (CA-03 / HU-003).
            // Guid.Empty: el tenant es desconocido (no se puede resolver sin el usuario).
            await RegistrarLoginFallido(Guid.Empty, request.Email, ipAddress, userAgent);
            throw new BusinessException("Credenciales inválidas");
        }

        // 2. Bloqueo temporal por intentos fallidos (CA-04).
        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            throw new BusinessException(
                $"Cuenta bloqueada hasta {usuario.BloqueadoHasta.Value:dd/MM/yyyy HH:mm}");
        }

        // 3. Estado de la cuenta (CA-07): mensaje específico según estado.
        if (usuario.Estado != EstadoUsuario.ACTIVE)
        {
            var mensaje = usuario.Estado switch
            {
                EstadoUsuario.PENDING => "Cuenta pendiente de activación. Revise su email.",
                EstadoUsuario.SUSPENDED => "Cuenta suspendida. Contacte al administrador.",
                EstadoUsuario.LOCKED => "Cuenta bloqueada. Contacte al administrador.",
                _ => "La cuenta no está activa. Contacte al administrador."
            };
            throw new BusinessException(mensaje);
        }

        // 4. Verificar credenciales contra Supabase Auth (CA-06).
        var signIn = await _supabaseAuth.SignInWithPasswordAsync(usuario.Email, request.Password);

        if (!signIn.Success)
        {
            // 5 intentos fallidos consecutivos → bloqueo 30 min (CA-04).
            await _usuarioRepository.IncrementarIntentosFallidosAsync(usuario.Id);

            var intentos = usuario.IntentosFallidos + 1;
            if (intentos >= 5)
            {
                await _usuarioRepository.BloquearHastaAsync(
                    usuario.Id, DateTime.UtcNow.AddMinutes(30));
                _logger.LogWarning(
                    "Cuenta {UsuarioId} bloqueada por {Intentos} intentos fallidos",
                    usuario.Id, intentos);
            }

            await RegistrarLoginFallido(usuario.EmpresaId, usuario.Email, ipAddress, userAgent);
            throw new BusinessException("Credenciales inválidas");
        }

        // 5. Login exitoso: resetear intentos y actualizar último acceso (CA-05).
        await _usuarioRepository.ResetearIntentosFallidosAsync(usuario.Id);
        await _usuarioRepository.ActualizarUltimoAccesoAsync(usuario.Id);

        // 6. Permisos del perfil → claims "modulo:accion" (ADR-009).
        var permisos = await CargarPermisosAsync(usuario.PerfilId, usuario.EmpresaId);

        // 7. Nombre del tenant para la respuesta.
        var empresa = await _empresaRepository.GetByIdAsync(usuario.EmpresaId);
        var empresaNombre = empresa?.Nombre ?? string.Empty;

        // 8. Generar access token + refresh token (hash persistido en sesiones).
        var accessToken = _jwtService.GenerateAccessToken(
            usuario.Id, usuario.EmpresaId, usuario.PerfilId,
            usuario.TipoUsuario, usuario.NombreCompleto, permisos);

        var refreshToken = await CrearSesionAsync(usuario);

        // 9. Auditoría (CA-08).
        await _auditoria.RegistrarAsync(
            "auth", AccionAuditoria.LOGIN, usuario.EmpresaId, usuario.Id,
            nameof(Usuario), usuario.Id,
            new { metodo = "password" }, ipAddress, userAgent);

        // 10. Respuesta con claims y permisos (HU-003 response).
        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.ExpiryHours * 3600, // 28800 s = 8 h
            Usuario = new UsuarioTokenDto
            {
                Id = usuario.Id,
                Nombre = usuario.NombreCompleto,
                Email = usuario.Email,
                TipoUsuario = usuario.TipoUsuario,
                EmpresaNombre = empresaNombre,
                Permisos = permisos.ToList()
            }
        };
    }

    // ── Refresh (HU-003 CA-02) ──────────────────────────────────────

    /// <summary>Renueva el access token con el refresh token (rotación del refresh).</summary>
    public async Task<LoginResponseDto> RefreshAsync(RefreshTokenRequestDto request)
    {
        var hash = _jwtService.HashRefreshToken(request.RefreshToken);
        var sesion = await _sesionRepository.GetByRefreshTokenHashAsync(hash);

        if (sesion is null || !sesion.Activa || sesion.FechaExpiracion < DateTime.UtcNow)
        {
            throw new BusinessException("Token inválido");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(sesion.UsuarioId, sesion.EmpresaId);
        if (usuario is null || !usuario.Activo)
        {
            throw new BusinessException("Token inválido");
        }

        // Reconstruir claims (los permisos se releen del perfil — HU-006 CA-05:
        // un cambio de permisos aplica en el próximo login/refresh).
        var permisos = await CargarPermisosAsync(usuario.PerfilId, usuario.EmpresaId);
        var empresa = await _empresaRepository.GetByIdAsync(usuario.EmpresaId);
        var empresaNombre = empresa?.Nombre ?? string.Empty;

        var accessToken = _jwtService.GenerateAccessToken(
            usuario.Id, usuario.EmpresaId, usuario.PerfilId,
            usuario.TipoUsuario, usuario.NombreCompleto, permisos);

        // Rotación: revocar el refresh usado y emitir uno nuevo.
        await _sesionRepository.RevocarAsync(sesion.Id);
        var nuevoRefresh = await CrearSesionAsync(usuario);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = nuevoRefresh,
            ExpiresIn = _jwtSettings.ExpiryHours * 3600,
            Usuario = new UsuarioTokenDto
            {
                Id = usuario.Id,
                Nombre = usuario.NombreCompleto,
                Email = usuario.Email,
                TipoUsuario = usuario.TipoUsuario,
                EmpresaNombre = empresaNombre,
                Permisos = permisos.ToList()
            }
        };
    }

    // ── Logout (HU-003) ─────────────────────────────────────────────

    /// <summary>Cierra la sesión invalidando el refresh token. Registra LOGOUT (HU-008 CA-01).</summary>
    public async Task LogoutAsync(string refreshToken)
    {
        var (ipAddress, userAgent) = GetRequestContext();
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = await _sesionRepository.GetByRefreshTokenHashAsync(hash);

        if (sesion is null)
        {
            // Logout idempotente: token desconocido no es un error.
            return;
        }

        await _sesionRepository.RevocarAsync(sesion.Id);

        await _auditoria.RegistrarAsync(
            "auth", AccionAuditoria.LOGOUT, sesion.EmpresaId, sesion.UsuarioId,
            nameof(Sesion), sesion.Id, null, ipAddress, userAgent);
    }

    // ── Forgot password (HU-007) ────────────────────────────────────

    /// <summary>
    /// Solicita recuperación de contraseña. Respuesta SIEMPRE genérica (HU-007
    /// CA-03): no revela si el email existe. Token de un solo uso, 30 min (CA-02/04).
    /// </summary>
    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var (ipAddress, userAgent) = GetRequestContext();

        // TODO: El usuario no existe — respuesta idéntica (CA-03).
        var usuario = await _usuarioRepository.GetByEmailGlobalAsync(request.Email);

        if (usuario is null)
        {
            // CA-03: respuesta idéntica — no hacer nada y retornar genérico.
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        var invitacion = new Invitacion
        {
            EmpresaId = usuario.EmpresaId,
            Email = usuario.Email,
            PerfilId = usuario.PerfilId,
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30), // CA-02: 30 min
            CreadoPorId = usuario.Id,
            FechaCreacion = DateTime.UtcNow
        };

        await _invitacionRepository.CreateAsync(invitacion);

        var link = $"{_appSettings.BaseUrl}/auth/reset-password?token={token}";
        await _emailService.EnviarAsync(
            usuario.Email,
            "Recuperación de contraseña — Freiroute TMS",
            $"<p>Hola {usuario.NombreCompleto},</p>" +
            "<p>Recibimos una solicitud para restablecer tu contraseña.</p>" +
            $"<p><a href=\"{link}\">Restablecer contraseña</a> (válido por 30 minutos)</p>" +
            "<p>Si no solicitaste este cambio, ignora este correo.</p>");

        // CA-07: registro de la solicitud.
        await _auditoria.RegistrarAsync(
            "auth", "FORGOT_PASSWORD", usuario.EmpresaId, usuario.Id,
            nameof(Usuario), usuario.Id,
            new { email = usuario.Email }, ipAddress, userAgent);
    }

    // ── Reset password (HU-007) ─────────────────────────────────────

    /// <summary>
    /// Restablece la contraseña con el token de un solo uso (CA-04/05).
    /// Invalida TODAS las sesiones activas del usuario (CA-06).
    /// </summary>
    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var (ipAddress, userAgent) = GetRequestContext();

        var invitacion = await _invitacionRepository.GetByTokenAsync(request.Token);

        if (invitacion is null ||
            invitacion.Estado != "PENDING" ||
            invitacion.FechaExpiracion < DateTime.UtcNow)
        {
            throw new BusinessException("Token inválido o expirado");
        }

        var usuario = await _usuarioRepository.GetByEmailAsync(
            invitacion.Email, invitacion.EmpresaId);

        if (usuario is null)
        {
            throw new BusinessException("Token inválido o expirado");
        }

        // Cambiar contraseña en Supabase Auth (stub Sprint 1 — ver TODO).
        if (usuario.SupabaseUserId.HasValue)
        {
            await _supabaseAuth.UpdatePasswordAsync(
                usuario.SupabaseUserId.Value, request.NewPassword);
        }

        // Token de un solo uso (CA-04).
        await _invitacionRepository.MarcarAceptadaAsync(invitacion.Id, DateTime.UtcNow);

        // CA-06: invalidar todas las sesiones activas.
        await _sesionRepository.RevocarTodasPorUsuarioAsync(usuario.Id);

        await _auditoria.RegistrarAsync(
            "auth", "RESET_PASSWORD", usuario.EmpresaId, usuario.Id,
            nameof(Usuario), usuario.Id, null, ipAddress, userAgent);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>Serializa los permisos del perfil a claims "modulo:read|create|update" (ADR-009).</summary>
    private async Task<IEnumerable<string>> CargarPermisosAsync(Guid perfilId, Guid empresaId)
    {
        var permisos = await _permisoRepository.GetByPerfilAsync(perfilId, empresaId);

        return permisos
            .SelectMany(p => new[]
            {
                p.PuedeLeer ? $"{p.Modulo}:read" : null,
                p.PuedeCrear ? $"{p.Modulo}:create" : null,
                p.PuedeActualizar ? $"{p.Modulo}:update" : null
            })
            .Where(v => v is not null)
            .Select(v => v!);
    }

    /// <summary>Crea una sesión con el hash del refresh token y devuelve el token opaco.</summary>
    private async Task<string> CrearSesionAsync(Usuario usuario)
    {
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshHash = _jwtService.HashRefreshToken(refreshToken);

        await _sesionRepository.CreateAsync(new Sesion
        {
            EmpresaId = usuario.EmpresaId,
            UsuarioId = usuario.Id,
            RefreshTokenHash = refreshHash,
            FechaExpiracion = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationDays),
            Activa = true,
            FechaCreacion = DateTime.UtcNow
        });

        return refreshToken;
    }

    /// <summary>Registra LOGIN_FAILED en auditoría (CA-08). Nunca propaga excepciones.</summary>
    private async Task RegistrarLoginFallido(
        Guid empresaId, string email, string? ipAddress, string? userAgent)
    {
        try
        {
            await _auditoria.RegistrarAsync(
                "auth", AccionAuditoria.LOGIN_FAILED, empresaId, null,
                nameof(Usuario), null,
                new { email }, ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar LOGIN_FAILED para {Email}", email);
        }
    }

    /// <summary>Lee IP y User-Agent del request actual (null-safe para tests sin HttpContext).</summary>
    private (string? IpAddress, string? UserAgent) GetRequestContext()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return (null, null);
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var agent = context.Request.Headers.UserAgent.ToString();

        return (string.IsNullOrWhiteSpace(ip) ? null : ip,
                string.IsNullOrWhiteSpace(agent) ? null : agent);
    }
}