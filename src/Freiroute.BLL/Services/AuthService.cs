using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;

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
    private readonly IConfiguracion2faRepository _config2faRepository;
    private readonly ISupabaseAuthService _supabaseAuth;
    private readonly IJwtService _jwtService;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;
    private readonly AppSettings _appSettings;
    private readonly string _totpEncryptionKey;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPermisoRepository permisoRepository,
        IEmpresaRepository empresaRepository,
        IInvitacionRepository invitacionRepository,
        ISesionRepository sesionRepository,
        IConfiguracion2faRepository config2faRepository,
        ISupabaseAuthService supabaseAuth,
        IJwtService jwtService,
        IAuditoriaService auditoria,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IOptions<JwtSettings> jwtSettings,
        IOptions<AppSettings> appSettings,
        ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _permisoRepository = permisoRepository;
        _empresaRepository = empresaRepository;
        _invitacionRepository = invitacionRepository;
        _sesionRepository = sesionRepository;
        _config2faRepository = config2faRepository;
        _supabaseAuth = supabaseAuth;
        _jwtService = jwtService;
        _auditoria = auditoria;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtSettings.Value;
        _appSettings = appSettings.Value;
        // Clave maestra para cifrar el secret TOTP (ADR-011). OBLIGATORIA:
        // sin ella, cada instancia scoped usaría una clave distinta y el 2FA
        // nunca podría descifrar el secret (Fix re-smoke test — error de descifrado).
        // Fuentes válidas: variable de entorno TOTP_ENCRYPTION_KEY o
        // appsettings.Development.json → Security:TotpEncryptionKey.
        var claveTotp = configuration["Security:TotpEncryptionKey"];
        if (string.IsNullOrWhiteSpace(claveTotp))
        {
            throw new InvalidOperationException(
                "La clave de cifrado TOTP no está configurada. Verificar Security:TotpEncryptionKey en la configuración.");
        }
        _totpEncryptionKey = claveTotp;
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

        // 7b. Verificar si el usuario tiene 2FA activo (HU-005).
        var config2fa = await _config2faRepository
            .GetByUsuarioIdAsync(usuario.Id, usuario.EmpresaId);

        if (config2fa is not null && config2fa.Activo &&
            (config2fa.TotpHabilitado || config2fa.EmailHabilitado))
        {
            // Enviar código por email si EmailHabilitado.
            if (config2fa.EmailHabilitado)
            {
                var codigo = GenerarCodigo6Digitos();
                var codigoHash = HashCodigo(codigo);
                await _config2faRepository.CrearCodigoTemporalAsync(new Codigo2faTempora
                {
                    UsuarioId = usuario.Id,
                    CodigoHash = codigoHash,
                    Tipo = "EMAIL",
                    Usado = false,
                    FechaExpiracion = DateTime.UtcNow.AddMinutes(10)
                });

                try
                {
                    await _emailService.EnviarAsync(
                        usuario.Email,
                        "Código de verificación Freiroute",
                        $"Tu código de verificación es: <strong>{codigo}</strong>" +
                        $"<br>Válido por 10 minutos.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error enviando código 2FA por email a {Email}", usuario.Email);
                }
            }

            // Generar TempToken (JWT mínimo, exp 5 min).
            var tempToken = _jwtService.GenerateTempToken(
                usuario.Id, usuario.EmpresaId, 5);

            // Registrar en auditoría.
            await _auditoria.RegistrarAsync(
                "auth", "2FA_REQUERIDO", usuario.EmpresaId, usuario.Id,
                ipAddress: ipAddress);

            // Lanzar excepción especial → el middleware retorna 202.
            throw new Requires2faException(tempToken);
        }

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

    // ── 2FA TOTP (HU-005) ──────────────────────────────────────────

    /// <summary>
    /// Prepara el alta de 2FA TOTP (HU-005 CA-01): genera secret, QR y 8 códigos
    /// de recuperación. Persiste un registro pendiente (TotpHabilitado=false) con el
    /// secret cifrado AES-256 para poder verificar el primer código al activar.
    /// </summary>
    public async Task<Setup2faResponseDto> Setup2faAsync(Guid usuarioId, Guid empresaId)
    {
        var usuario = await ObtenerUsuarioActivoAsync(usuarioId, empresaId);

        // Generar secret TOTP de 20 bytes (160 bits) y el URI otpauth.
        var secretBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(20);
        var secretB32 = Base32Encoding.ToString(secretBytes);

        var otpauthUri = $"otpauth://totp/{Uri.EscapeDataString("Freiroute TMS")}:" +
                         $"{Uri.EscapeDataString(usuario.Email)}?secret={secretB32}&issuer=" +
                         $"{Uri.EscapeDataString("Freiroute TMS")}&algorithm=SHA1&digits=6&period=30";

        // 8 códigos de recuperación de un solo uso.
        var recoveryCodes = GenerarCodigosRecuperacion(8);

        // Persistir (o actualizar) un registro pendiente con el secret cifrado.
        var config = await _config2faRepository.GetByUsuarioIdAsync(usuarioId, empresaId);
        string secretCifrado = AesGcmEncryptor.Encrypt(secretB32, _totpEncryptionKey);

        if (config is null)
        {
            await _config2faRepository.CreateAsync(new Configuracion2fa
            {
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                TotpSecret = secretCifrado,
                TotpHabilitado = false,
                EmailHabilitado = false,
                CodigosRecuperacion = recoveryCodes.Select(HashCodigo).ToArray(),
                FechaCreacion = DateTime.UtcNow
            });
        }
        else
        {
            config.TotpSecret = secretCifrado;
            config.TotpHabilitado = false;
            config.CodigosRecuperacion = recoveryCodes.Select(HashCodigo).ToArray();
            config.FechaModificacion = DateTime.UtcNow;
            await _config2faRepository.UpdateAsync(config);
        }

        return new Setup2faResponseDto
        {
            // Solo se muestra una vez: el secret en claro y los códigos de recuperación.
            Secret = secretB32,
            // Sin librería QrCodeOwnEnvironment en el stack: se expone la URI otpauth
            // para que el frontend genere el QR (data URL) del lado cliente.
            QrCodeUrl = otpauthUri,
            CodigosRecuperacion = recoveryCodes.ToList()
        };
    }

    /// <summary>
    /// Activa el 2FA tras verificar el primer código TOTP (HU-005 CA-01).
    /// Marca TotpHabilitado=true; los códigos de recuperación ya quedaron persistidos
    /// como hashes durante el setup.
    /// </summary>
    public async Task<bool> Activar2faAsync(Activar2faRequestDto dto, Guid usuarioId, Guid empresaId)
    {
        var config = await _config2faRepository.GetByUsuarioIdAsync(usuarioId, empresaId);
        if (config is null || string.IsNullOrEmpty(config.TotpSecret))
        {
            throw new BusinessException("Debe ejecutar el setup de 2FA antes de activarlo.");
        }

        var secretB32 = AesGcmEncryptor.Decrypt(config.TotpSecret, _totpEncryptionKey);
        var totp = new Totp(Base32Encoding.ToBytes(secretB32), step: 30,
            mode: OtpHashMode.Sha1, totpSize: 6);

        if (!totp.VerifyTotp(dto.Codigo, out _, new VerificationWindow(1, 1)))
        {
            throw new BusinessException("El código TOTP es inválido o ha expirado.");
        }

        config.TotpHabilitado = true;
        config.FechaModificacion = DateTime.UtcNow;
        await _config2faRepository.UpdateAsync(config);

        await _auditoria.RegistrarAsync(
            "auth", "ACTIVAR_2FA", empresaId, usuarioId,
            nameof(Configuracion2fa), config.Id, new { tipo = "TOTP" });

        return true;
    }

    /// <summary>
    /// Desactiva el 2FA de un usuario (HU-005 CA-06). Requiere la verificación del
    /// código actual (TOTP o email) antes de desactivar por seguridad.
    /// </summary>
    public async Task<bool> Desactivar2faAsync(Guid usuarioId, Guid empresaId, string codigoActual)
    {
        var config = await _config2faRepository.GetByUsuarioIdAsync(usuarioId, empresaId)
            ?? throw new NotFoundException(nameof(Configuracion2fa), usuarioId);

        // Verificar el código actual antes de desactivar.
        var codigoValido = false;

        if (config.TotpHabilitado && !string.IsNullOrEmpty(config.TotpSecret))
        {
            var secret = AesGcmEncryptor.Decrypt(config.TotpSecret, _totpEncryptionKey);
            var totp = new Totp(Base32Encoding.ToBytes(secret), step: 30,
                mode: OtpHashMode.Sha1, totpSize: 6);
            codigoValido = totp.VerifyTotp(codigoActual, out _, new VerificationWindow(2, 2));
        }

        if (!codigoValido && config.EmailHabilitado)
        {
            var hash = HashCodigo(codigoActual);
            var codigoTemp = await _config2faRepository
                .GetCodigoTemporalValidoAsync(usuarioId, hash);
            codigoValido = codigoTemp is not null;
            if (codigoValido)
            {
                await _config2faRepository.MarcarCodigoUsadoAsync(codigoTemp!.Id);
            }
        }

        if (!codigoValido)
        {
            throw new BusinessException(
                "Código incorrecto. No se puede desactivar el 2FA.");
        }

        var ok = await _config2faRepository.DeactivateAsync(usuarioId, empresaId);

        if (ok)
        {
            await _auditoria.RegistrarAsync(
                "auth", "DESACTIVAR_2FA", empresaId, usuarioId,
                nameof(Configuracion2fa), config.Id, null);
        }

        return ok;
    }

    /// <summary>
    /// Segundo paso del login con 2FA (HU-005): valida el temp token y el código
    /// TOTP (o un código de recuperación de un solo uso). Emite access + refresh token.
    /// </summary>
    public async Task<LoginResponseDto> Verificar2faAsync(Verificar2faRequestDto request)
    {
        var (ipAddress, userAgent) = GetRequestContext();

        var payload = _jwtService.ValidateTempToken(request.TempToken);
        if (payload is null)
        {
            throw new BusinessException("Sesión 2FA inválida o expirada. Vuelva a iniciar sesión.");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(payload.UserId, payload.EmpresaId);
        if (usuario is null || !usuario.Activo || usuario.Estado != EstadoUsuario.ACTIVE)
        {
            throw new BusinessException("Usuario inválido");
        }

        var config = await _config2faRepository.GetByUsuarioIdAsync(usuario.Id, usuario.EmpresaId);
        if (config is null || (!config.TotpHabilitado && !config.EmailHabilitado))
        {
            throw new BusinessException("El usuario no tiene 2FA activo.");
        }

        var valido = false;

        if (config.TotpHabilitado && !string.IsNullOrEmpty(config.TotpSecret))
        {
            var secretB32 = AesGcmEncryptor.Decrypt(config.TotpSecret, _totpEncryptionKey);
            var totp = new Totp(Base32Encoding.ToBytes(secretB32), mode: OtpHashMode.Sha1, totpSize: 6);
            valido = totp.VerifyTotp(request.Codigo, out _, new VerificationWindow(1, 1));
        }

        // Código de recuperación de un solo uso (hash).
        if (!valido)
        {
            var codigoHash = HashCodigo(request.Codigo);
            if (config.CodigosRecuperacion.Contains(codigoHash))
            {
                config.CodigosRecuperacion = config.CodigosRecuperacion
                    .Where(c => c != codigoHash).ToArray();
                await _config2faRepository.UpdateAsync(config);
                valido = true;
            }
        }

        if (!valido)
        {
            throw new BusinessException("El código 2FA es inválido o ha expirado.");
        }

        return await CompletarLoginAsync(usuario, "2fa", ipAddress, userAgent);
    }

    /// <summary>
    /// Login con OAuth (HU-004). En este sprint se resuelve el vínculo por
    /// supabase_user_id; la llamada real a Supabase Auth por proveedor va pendiente.
    /// </summary>
    public async Task<LoginResponseDto> LoginConOAuthAsync(OAuthCallbackRequestDto request)
    {
        var (ipAddress, userAgent) = GetRequestContext();

        // Implementación base: se asume que el frontend ya validó el access token del
        // proveedor contra Supabase. Aquí solo se resuelve el usuario vinculado.
        // El parseo real del token OAuth de Supabase se integra en Sprint 3.
        await Task.CompletedTask;
        throw new NotImplementedException(
            "La resolución del token OAuth de Supabase se implementa en Sprint 3 (HU-004).");
    }

    // ── Recovery codes (HU-005) ──────────────────────────────────

    /// <summary>
    /// Los códigos de recuperación en BD son hashes SHA-256 — no se pueden recuperar.
    /// Siempre lanza BusinessException con un mensaje informativo.
    /// </summary>
    public Task GetRecoveryCodesAsync(Guid usuarioId, Guid empresaId)
    {
        throw new BusinessException(
            "Los códigos de recuperación solo se muestran al activar 2FA. " +
            "Genera nuevos códigos si necesitas acceder a ellos.");
    }

    /// <summary>
    /// Regenera los 8 códigos de recuperación de 2FA (HU-005 CA-04).
    /// Retorna los códigos en claro — solo se muestran una vez.
    /// </summary>
    public async Task<List<string>> RegenerarRecoveryCodesAsync(Guid usuarioId, Guid empresaId)
    {
        var config = await _config2faRepository.GetByUsuarioIdAsync(usuarioId, empresaId)
            ?? throw new NotFoundException(nameof(Configuracion2fa), usuarioId);

        if (!config.TotpHabilitado && !config.EmailHabilitado)
        {
            throw new BusinessException(
                "No tienes 2FA activo. Activa 2FA primero.");
        }

        // Generar 8 códigos nuevos.
        var codigosClaro = Enumerable.Range(0, 8)
            .Select(_ => Guid.NewGuid().ToString("N")[..8].ToUpper())
            .ToList();

        // Hashear y guardar.
        config.CodigosRecuperacion = codigosClaro
            .Select(HashCodigo).ToArray();
        config.FechaModificacion = DateTime.UtcNow;
        await _config2faRepository.UpdateAsync(config);

        await _auditoria.RegistrarAsync(
            "auth", "2FA_CODIGOS_REGENERADOS", empresaId, usuarioId);

        return codigosClaro; // Retornar en claro — solo esta vez
    }

    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>Obtiene un usuario activo de la empresa; si no, lanza not found.</summary>
    private async Task<Usuario> ObtenerUsuarioActivoAsync(Guid usuarioId, Guid empresaId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, empresaId);
        if (usuario is null)
        {
            throw new NotFoundException(nameof(Usuario), usuarioId);
        }
        return usuario;
    }

    /// <summary>Genera N códigos de recuperación aleatorios (formato XXXX-XXXX).</summary>
    private static List<string> GenerarCodigosRecuperacion(int cantidad)
    {
        var codigos = new List<string>();
        for (var i = 0; i < cantidad; i++)
        {
            var rnd = new Random();
            var parte1 = rnd.Next(0, 10000).ToString("D4");
            var parte2 = rnd.Next(0, 10000).ToString("D4");
            codigos.Add($"{parte1}-{parte2}");
        }
        return codigos;
    }

    /// <summary>Hash SHA-256 (hex) de un código de recuperación para persistirlo.</summary>
    private static string HashCodigo(string codigo)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(codigo));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Completa el login tras superar la verificación 2FA: relee permisos, genera
    /// access token, crea la sesión de refresh y registra auditoría LOGIN.
    /// </summary>
    private async Task<LoginResponseDto> CompletarLoginAsync(
        Usuario usuario, string metodo, string? ipAddress, string? userAgent)
    {
        await _usuarioRepository.ResetearIntentosFallidosAsync(usuario.Id);
        await _usuarioRepository.ActualizarUltimoAccesoAsync(usuario.Id);

        var permisos = await CargarPermisosAsync(usuario.PerfilId, usuario.EmpresaId);
        var empresa = await _empresaRepository.GetByIdAsync(usuario.EmpresaId);
        var empresaNombre = empresa?.Nombre ?? string.Empty;

        var accessToken = _jwtService.GenerateAccessToken(
            usuario.Id, usuario.EmpresaId, usuario.PerfilId,
            usuario.TipoUsuario, usuario.NombreCompleto, permisos);

        var refreshToken = await CrearSesionAsync(usuario);

        await _auditoria.RegistrarAsync(
            "auth", AccionAuditoria.LOGIN, usuario.EmpresaId, usuario.Id,
            nameof(Usuario), usuario.Id,
            new { metodo }, ipAddress, userAgent);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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

    /// <summary>Genera un código de verificación de 6 dígitos numéricos aleatorios.</summary>
    private static string GenerarCodigo6Digitos()
    {
        var rnd = new Random();
        return rnd.Next(0, 999999).ToString("D6");
    }

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