using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
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
using Microsoft.IdentityModel.Tokens;
using Moq;
using FluentAssertions;
using OtpNet;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del servicio de autenticación (HU-003) — el más crítico del Sprint 1.
/// Cubre los criterios de aceptación CA-01 a CA-08 del login, el refresh token
/// y los claims del JWT (ADR-007).
/// </summary>
public class AuthServiceTests
{
    private const string TestSecret = "EstaEsUnaClaveSecretaParaTesting2026SoloDevLocal";
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid PerfilId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IUsuarioRepository> _usuarioRepository;
    private readonly Mock<IPermisoRepository> _permisoRepository;
    private readonly Mock<IEmpresaRepository> _empresaRepository;
    private readonly Mock<IInvitacionRepository> _invitacionRepository;
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<ISesionRepository> _sesionRepository;
    private readonly Mock<IConfiguracion2faRepository> _config2faRepository;
    private readonly Mock<ISupabaseAuthService> _supabaseAuth;
    private readonly IJwtService _jwtService;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<IStorageService> _storageService;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _usuarioRepository = new Mock<IUsuarioRepository>();
        _permisoRepository = new Mock<IPermisoRepository>();
        _empresaRepository = new Mock<IEmpresaRepository>();
        _invitacionRepository = new Mock<IInvitacionRepository>();
        _perfilRepository = new Mock<IPerfilRepository>();
        _sesionRepository = new Mock<ISesionRepository>();
        _config2faRepository = new Mock<IConfiguracion2faRepository>();
        _supabaseAuth = new Mock<ISupabaseAuthService>();
        _auditoria = new Mock<IAuditoriaService>();
        _emailService = new Mock<IEmailService>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _storageService = new Mock<IStorageService>();
        _logger = new Mock<ILogger<AuthService>>();
        _jwtSettings = Options.Create(new JwtSettings
        {
            Key = TestSecret,
            Issuer = "freiroute-api",
            Audience = "freiroute-client",
            ExpiryHours = 8,
            RefreshExpirationDays = 30
        });
        _appSettings = Options.Create(new AppSettings { BaseUrl = "https://localhost:5001" });
        _logger = new Mock<ILogger<AuthService>>();

        // Usamos el JwtService REAL para poder verificar los claims del token.
        _jwtService = new JwtService(_jwtSettings);

        // Fix re-smoke test #5B: AuthService requiere Security:TotpEncryptionKey
        // (antes usaba RandomKeyFallback). Los tests usan la clave TOTP conocida.
        var configConClave = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:TotpEncryptionKey", ClaveTotpConocida }
            })
            .Build();

        _service = new AuthService(
            _usuarioRepository.Object,
            _permisoRepository.Object,
            _empresaRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            _config2faRepository.Object,
            _supabaseAuth.Object,
            _jwtService,
            _auditoria.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            configConClave,
            _jwtSettings,
            _appSettings,
            _logger.Object,
            _perfilRepository.Object,
            _httpClientFactory.Object,
            _storageService.Object);
    }

    [Fact]
    public void Constructor_CuandoFaltaClaveTotp_LanzaInvalidOperation()
    {
        // Fix re-smoke test #5B: sin Security:TotpEncryptionKey el constructor DEBE
        // fallar (antes se derivaba una clave aleatoria que rompía el descifrado del
        // secret TOTP entre requests porque cada instancia usaba una clave distinta).
        var configSinClave = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .Build();

        var act = () => new AuthService(
            _usuarioRepository.Object,
            _permisoRepository.Object,
            _empresaRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            _config2faRepository.Object,
            _supabaseAuth.Object,
            _jwtService,
            _auditoria.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            configSinClave,
            _jwtSettings,
            _appSettings,
            _logger.Object,
            _perfilRepository.Object,
            _httpClientFactory.Object,
            _storageService.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TotpEncryptionKey*");
    }

    private Usuario UsuarioActivo() => new()
    {
        Id = UsuarioId,
        EmpresaId = EmpresaId,
        PerfilId = PerfilId,
        NombreCompleto = "Juan Pérez",
        Email = "juan@transnic.com",
        TipoUsuario = TipoUsuario.DISPATCHER,
        Estado = EstadoUsuario.ACTIVE,
        Activo = true,
        IntentosFallidos = 0,
        FechaCreacion = DateTime.UtcNow
    };

    private void ConfigurarLoginExitoso()
    {
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(UsuarioActivo());

        _supabaseAuth
            .Setup(s => s.SignInWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SupabaseSignInResult(true, Guid.NewGuid()));

        _usuarioRepository
            .Setup(r => r.ResetearIntentosFallidosAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _usuarioRepository
            .Setup(r => r.ActualizarUltimoAccesoAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<Permiso>
            {
                new() {
                    Modulo = "embarques",
                    PuedeLeer = true,
                    PuedeCrear = true,
                    PuedeActualizar = true,
                    Activo = true
                }
            });

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Trans Nicaragua S.A." });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private static JwtSecurityToken Decodificar(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    // ── Login Happy path ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CuandoCredencialesValidas_RetornaTokenConClaims()
    {
        ConfigurarLoginExitoso();

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.Usuario.Email.Should().Be("juan@transnic.com");
        result.Usuario.TipoUsuario.Should().Be(TipoUsuario.DISPATCHER);
        result.Usuario.EmpresaNombre.Should().Be("Trans Nicaragua S.A.");
    }

    [Fact]
    public async Task LoginAsync_CuandoLogin_ActualizaUltimoAcceso()
    {
        ConfigurarLoginExitoso();

        await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        _usuarioRepository.Verify(
            r => r.ActualizarUltimoAccesoAsync(UsuarioId), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_CuandoLogin_ResetIntentosFallidos()
    {
        ConfigurarLoginExitoso();

        await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        _usuarioRepository.Verify(
            r => r.ResetearIntentosFallidosAsync(UsuarioId), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_CuandoLogin_RegistraAuditoriaLoginExitoso()
    {
        ConfigurarLoginExitoso();

        await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", AccionAuditoria.LOGIN, EmpresaId, UsuarioId,
                nameof(Usuario), UsuarioId,
                It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    // ── Login Error paths ──────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CuandoEmailNoExiste_LanzaBusinessException()
    {
        // CA-02 — mensaje genérico, no revela si el email existe.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "nadie@empresa.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Credenciales inválidas");
    }

    [Fact]
    public async Task LoginAsync_CuandoPasswordIncorrecta_LanzaBusinessException()
    {
        // CA-02 — mensaje genérico.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(UsuarioActivo());

        _supabaseAuth
            .Setup(s => s.SignInWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SupabaseSignInResult(false));

        _usuarioRepository
            .Setup(r => r.IncrementarIntentosFallidosAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "incorrecta"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Credenciales inválidas");
    }

    [Fact]
    public async Task LoginAsync_CuandoPasswordIncorrecta_IncrementaIntentos()
    {
        // CA-06 — login fallido incrementa intentos_fallidos.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(UsuarioActivo());

        _supabaseAuth
            .Setup(s => s.SignInWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SupabaseSignInResult(false));

        _usuarioRepository
            .Setup(r => r.IncrementarIntentosFallidosAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginRequestDto
            {
                Email = "juan@transnic.com",
                Password = "incorrecta"
            }));

        _usuarioRepository.Verify(
            r => r.IncrementarIntentosFallidosAsync(UsuarioId), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_CuandoIntentosFallidos5_BloqueaCuenta30Min()
    {
        // CA-04 — bloqueo tras 5 intentos fallidos consecutivos (NOW() + 30 min).
        var usuario = UsuarioActivo();
        usuario.IntentosFallidos = 4; // este será el 5º intento

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        _supabaseAuth
            .Setup(s => s.SignInWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SupabaseSignInResult(false));

        _usuarioRepository
            .Setup(r => r.IncrementarIntentosFallidosAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _usuarioRepository
            .Setup(r => r.BloquearHastaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginRequestDto
            {
                Email = "juan@transnic.com",
                Password = "incorrecta"
            }));

        _usuarioRepository.Verify(
            r => r.BloquearHastaAsync(
                UsuarioId,
                It.Is<DateTime>(d => d > DateTime.UtcNow.AddMinutes(29))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_CuandoCuentaBloqueada_LanzaBusinessException()
    {
        // CA-04 — ya bloqueada → no permite login.
        var usuario = UsuarioActivo();
        usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(30);

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*bloqueada*");
    }

    [Fact]
    public async Task LoginAsync_CuandoEstadoPending_LanzaBusinessExceptionEspecifica()
    {
        // CA-07 — estado PENDING → mensaje específico.
        var usuario = UsuarioActivo();
        usuario.Estado = EstadoUsuario.PENDING;

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*pendiente*");
    }

    [Fact]
    public async Task LoginAsync_CuandoEstadoSuspended_LanzaBusinessExceptionEspecifica()
    {
        // CA-07 — estado SUSPENDED → mensaje específico.
        var usuario = UsuarioActivo();
        usuario.Estado = EstadoUsuario.SUSPENDED;

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*suspendida*");
    }

    [Fact]
    public async Task LoginAsync_CuandoLogin_RegistraAuditoriaLoginFallido()
    {
        // CA-08 — LOGIN_FAILED registrado.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginRequestDto
            {
                Email = "nadie@empresa.com",
                Password = "MiPassword123!"
            }));

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", AccionAuditoria.LOGIN_FAILED, Guid.Empty, null,
                nameof(Usuario), null,
                It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    // ── Refresh token ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_CuandoTokenValido_RetornaNewAccessToken()
    {
        // Arrange
        var refreshToken = "some-refresh-token";
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            RefreshTokenHash = hash,
            FechaExpiracion = DateTime.UtcNow.AddDays(30),
            Activa = true,
            FechaCreacion = DateTime.UtcNow
        };

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync(sesion);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new List<Permiso>());

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Trans Nicaragua S.A." });

        _sesionRepository
            .Setup(r => r.RevocarAsync(sesion.Id))
            .ReturnsAsync(true);

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _service.RefreshAsync(new RefreshTokenRequestDto
        {
            RefreshToken = refreshToken
        });

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        // El refresh usado se revoca (rotación).
        _sesionRepository.Verify(r => r.RevocarAsync(sesion.Id), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_CuandoTokenRevocado_LanzaBusinessException()
    {
        // Arrange — sesión encontrada pero NO activa (revocada).
        var refreshToken = "revoked-token";
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            RefreshTokenHash = hash,
            FechaExpiracion = DateTime.UtcNow.AddDays(30),
            Activa = false,
            FechaCreacion = DateTime.UtcNow
        };

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync(sesion);

        // Act
        var act = async () => await _service.RefreshAsync(new RefreshTokenRequestDto
        {
            RefreshToken = refreshToken
        });

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Token inválido");
    }

    [Fact]
    public async Task RefreshTokenAsync_CuandoTokenExpirado_LanzaBusinessException()
    {
        // Arrange — sesión válida pero expirada.
        var refreshToken = "expired-token";
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            RefreshTokenHash = hash,
            FechaExpiracion = DateTime.UtcNow.AddDays(-1), // vencida
            Activa = true,
            FechaCreacion = DateTime.UtcNow.AddDays(-31)
        };

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync(sesion);

        // Act
        var act = async () => await _service.RefreshAsync(new RefreshTokenRequestDto
        {
            RefreshToken = refreshToken
        });

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Token inválido");
    }

    // ── JWT Claims ─────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_JwtContiene_UserId_EmpresaId_PerfilId_TipoUsuario()
    {
        // CA-01 — el token contiene los claims del ADR-007.
        ConfigurarLoginExitoso();

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var token = Decodificar(result.AccessToken);

        token.Claims.First(c => c.Type == "user_id").Value.Should().Be(UsuarioId.ToString());
        token.Claims.First(c => c.Type == "empresa_id").Value.Should().Be(EmpresaId.ToString());
        token.Claims.First(c => c.Type == "perfil_id").Value.Should().Be(PerfilId.ToString());
        token.Claims.First(c => c.Type == "tipo_usuario").Value.Should().Be(TipoUsuario.DISPATCHER);
        token.Claims.First(c => c.Type == "nombre").Value.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task LoginAsync_JwtContiene_PermisosComoClaimsIndividuales()
    {
        // Un claim "permisos" por permiso → User.FindAll("permisos") los ve.
        ConfigurarLoginExitoso();

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var token = Decodificar(result.AccessToken);
        var permisos = token.Claims
            .Where(c => c.Type == "permisos")
            .Select(c => c.Value)
            .ToList();

        permisos.Should().Contain("embarques:read");
        permisos.Should().Contain("embarques:create");
        permisos.Should().Contain("embarques:update");
    }

    [Fact]
    public async Task LoginAsync_JwtExpiraEn8Horas()
    {
        // CA-02 — JWT válido por 8 horas.
        var before = DateTime.UtcNow;
        ConfigurarLoginExitoso();

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var token = Decodificar(result.AccessToken);

        token.ValidTo.Should().BeAfter(before.AddHours(7.99));
        token.ValidTo.Should().BeBefore(before.AddHours(8.01));
    }

    // ── Login: estados adicionales y casos de cobertura ────────────

    [Fact]
    public async Task LoginAsync_CuandoEstadoLocked_LanzaMensajeDeBloqueo()
    {
        // CA-07 — estado LOCKED → mensaje específico.
        var usuario = UsuarioActivo();
        usuario.Estado = EstadoUsuario.LOCKED;

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*bloqueada*");
    }

    [Fact]
    public async Task LoginAsync_CuandoPasswordIncorrectaMenosDe5Intentos_NoBloquea()
    {
        // CA-04 — menos de 5 intentos: incrementa pero NO bloquea.
        var usuario = UsuarioActivo();
        usuario.IntentosFallidos = 1;

        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(usuario);

        _supabaseAuth
            .Setup(s => s.SignInWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SupabaseSignInResult(false));

        _usuarioRepository
            .Setup(r => r.IncrementarIntentosFallidosAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginRequestDto
            {
                Email = "juan@transnic.com",
                Password = "incorrecta"
            }));

        _usuarioRepository.Verify(
            r => r.BloquearHastaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_CuandoPermisosParciales_GeneraClaimsSoloDeFlagsActivos()
    {
        // CargarPermisosAsync: un flag en false NO genera su claim "modulo:accion".
        ConfigurarLoginExitoso();

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new List<Permiso>
            {
                new() { Modulo = "embarques", PuedeLeer = true, PuedeCrear = false, PuedeActualizar = true, Activo = true },
                new() { Modulo = "carriers", PuedeLeer = false, PuedeCrear = true, PuedeActualizar = false, Activo = true }
            });

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        var permisos = Decodificar(result.AccessToken).Claims
            .Where(c => c.Type == "permisos")
            .Select(c => c.Value)
            .ToList();

        permisos.Should().Contain("embarques:read");
        permisos.Should().Contain("embarques:update");
        permisos.Should().Contain("carriers:create");
        permisos.Should().NotContain("embarques:create");
        permisos.Should().NotContain("carriers:read");
        permisos.Should().NotContain("carriers:update");
    }

    [Fact]
    public async Task LoginAsync_CuandoAuditoriaFallidaNoPropagaExcepcion()
    {
        // RegistrarLoginFallido nunca propaga: si la auditoría falla, el login
        // sigue lanzando la BusinessException de credenciales, no la de auditoría.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("BD caída"));

        var act = async () => await _service.LoginAsync(new LoginRequestDto
        {
            Email = "nadie@empresa.com",
            Password = "MiPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Credenciales inválidas");
    }

    // ── Logout (HU-003) ────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_CuandoSesionValida_RevocaYRegistraAuditoria()
    {
        var refreshToken = "some-refresh";
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            RefreshTokenHash = hash,
            FechaExpiracion = DateTime.UtcNow.AddDays(30),
            Activa = true,
            FechaCreacion = DateTime.UtcNow
        };

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync(sesion);

        _sesionRepository
            .Setup(r => r.RevocarAsync(sesion.Id))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _service.LogoutAsync(refreshToken);

        _sesionRepository.Verify(r => r.RevocarAsync(sesion.Id), Times.Once);
        _auditoria.Verify(
            a => a.RegistrarAsync("auth", AccionAuditoria.LOGOUT, EmpresaId, UsuarioId,
                nameof(Sesion), sesion.Id, null, null, null),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_CuandoTokenDesconocido_EsIdempotente()
    {
        var refreshToken = "desconocido";
        var hash = _jwtService.HashRefreshToken(refreshToken);

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync((Sesion?)null);

        await _service.LogoutAsync(refreshToken);

        _sesionRepository.Verify(r => r.RevocarAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ── Forgot password (HU-007) ───────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_CuandoEmailExiste_CreaInvitacionYEnviaEmail()
    {
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync(UsuarioActivo());

        _invitacionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Invitacion>()))
            .ReturnsAsync(Guid.NewGuid());

        _emailService
            .Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            Email = "juan@transnic.com"
        });

        _invitacionRepository.Verify(r => r.CreateAsync(It.IsAny<Invitacion>()), Times.Once);
        _emailService.Verify(
            e => e.EnviarAsync("juan@transnic.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_CuandoEmailNoExiste_RespuestaGenéricaSinAccion()
    {
        // CA-03 — no revela si el email existe.
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            Email = "nadie@empresa.com"
        });

        _invitacionRepository.Verify(r => r.CreateAsync(It.IsAny<Invitacion>()), Times.Never);
        _emailService.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Reset password (HU-007) ────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_CuandoTokenValido_ActualizaYRevocaSesiones()
    {
        var token = "token-valido";
        var invitacion = new Invitacion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            Email = "juan@transnic.com",
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30)
        };
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = Guid.NewGuid();

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(invitacion);

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(invitacion.Email, EmpresaId))
            .ReturnsAsync(usuario);

        _supabaseAuth
            .Setup(s => s.CambiarPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _invitacionRepository
            .Setup(r => r.MarcarAceptadaAsync(invitacion.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _sesionRepository
            .Setup(r => r.RevocarTodasPorUsuarioAsync(usuario.Id))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NuevaPassword123!"
        });

        _supabaseAuth.Verify(
            s => s.CambiarPasswordAsync(usuario.SupabaseUserId.Value, "NuevaPassword123!"),
            Times.Once);
        _invitacionRepository.Verify(
            r => r.MarcarAceptadaAsync(invitacion.Id, It.IsAny<DateTime>()),
            Times.Once);
        _sesionRepository.Verify(
            r => r.RevocarTodasPorUsuarioAsync(usuario.Id), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoTokenExpirado_LanzaBusinessException()
    {
        var token = "token-expirado";
        var invitacion = new Invitacion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            Email = "juan@transnic.com",
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(-1)
        };

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(invitacion);

        var act = async () => await _service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NuevaPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*inválido*");
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoUsuarioNoExiste_LanzaBusinessException()
    {
        var token = "token-valido";
        var invitacion = new Invitacion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            Email = "juan@transnic.com",
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30)
        };

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(invitacion);

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(invitacion.Email, EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NuevaPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*inválido*");
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoUsuarioSinSupabaseUserId_LanzaBusinessException()
    {
        // G-06-D: sin supabase_user_id no hay cuenta en Supabase Auth donde cambiar
        // la contraseña — el flujo de recuperación debe fallar de forma controlada.
        var token = "token-valido-sin-supabase";
        var invitacion = new Invitacion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            Email = "juan@transnic.com",
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30)
        };
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = null;

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(invitacion);

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(invitacion.Email, EmpresaId))
            .ReturnsAsync(usuario);

        var act = async () => await _service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NuevaPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*Supabase Auth*");

        _supabaseAuth.Verify(
            s => s.CambiarPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
        _invitacionRepository.Verify(
            r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoSupabaseNoPuedeCambiar_LanzaBusinessException()
    {
        // G-06-D: si Supabase Auth rechaza el cambio (false), el flujo no debe
        // marcar la invitación como aceptada ni revocar sesiones.
        var token = "token-valido-supabase-falla";
        var invitacion = new Invitacion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            Email = "juan@transnic.com",
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30)
        };
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = Guid.NewGuid();

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(invitacion);

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(invitacion.Email, EmpresaId))
            .ReturnsAsync(usuario);

        _supabaseAuth
            .Setup(s => s.CambiarPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var act = async () => await _service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NuevaPassword123!"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*No se pudo actualizar la contraseña*");

        _sesionRepository.Verify(
            r => r.RevocarTodasPorUsuarioAsync(usuario.Id), Times.Never);
    }

    // ── OAuth (HU-004) ──────────────────────────────────────────────

    [Fact]
    public async Task LoginConOAuthAsync_CuandoTokenInvalido_LanzaBusinessException()
    {
        // HU-004 CA-01: token del proveedor rechazado → 422 controlado por el
        // middleware (BusinessException), nunca un 500 por fallo de red.
        var httpFactory = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)))
        {
            BaseAddress = new Uri("http://localhost:54321")
        };
        httpFactory.Setup(f => f.CreateClient("SupabaseAuth")).Returns(httpClient);

        var service = CrearServiceConClaveConocidaConHttpFactory(ClaveTotpConocida, httpFactory.Object);

        var act = async () => await service.LoginConOAuthAsync(new OAuthCallbackRequestDto
        {
            Provider = "google",
            SupabaseToken = "token-invalido"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*OAuth*");
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoUsuarioVinculado_CompletaSesionYAuditaLOGIN_OAUTH()
    {
        // HU-004: usuario ya vinculado por supabase_user_id → login completo
        // (perfiles, permisos, refresh) + auditoría LOGIN_OAUTH con provider.
        var supabaseUserId = Guid.NewGuid();
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = supabaseUserId;

        var httpFactory = HttpFactoryConSupabaseUser(supabaseUserId, "juan@transnic.com");

        _usuarioRepository
            .Setup(r => r.GetBySupabaseUserIdAsync(supabaseUserId))
            .ReturnsAsync(usuario);

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new[]
            {
                new Permiso { Modulo = "embarques", PuedeLeer = true }
            });

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Transnic" });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _usuarioRepository
            .Setup(r => r.ResetearIntentosFallidosAsync(usuario.Id))
            .Returns(Task.CompletedTask);
        _usuarioRepository
            .Setup(r => r.ActualizarUltimoAccesoAsync(usuario.Id))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = CrearServiceConClaveConocidaConHttpFactory(ClaveTotpConocida, httpFactory.Object);

        var resultado = await service.LoginConOAuthAsync(new OAuthCallbackRequestDto
        {
            Provider = "google",
            SupabaseToken = "token-valido"
        });

        resultado.AccessToken.Should().NotBeNullOrEmpty();
        resultado.RefreshToken.Should().NotBeNullOrEmpty();
        resultado.Usuario.Email.Should().Be("juan@transnic.com");
        resultado.Usuario.Permisos.Should().Contain("embarques:read");

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", AccionAuditoria.LOGIN_OAUTH, EmpresaId, UsuarioId,
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);

        _usuarioRepository.Verify(
            r => r.ActualizarSupabaseUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoUsuarioExistePorEmail_VinculaSupabaseUserId()
    {
        // HU-004 CA-03: usuario Freiroute existente pero sin vínculo → se vincula
        // el supabase_user_id recibido del proveedor y se completa el login.
        var supabaseUserId = Guid.NewGuid();
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = null;

        var httpFactory = HttpFactoryConSupabaseUser(supabaseUserId, "juan@transnic.com");

        _usuarioRepository
            .Setup(r => r.GetBySupabaseUserIdAsync(supabaseUserId))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync("juan@transnic.com"))
            .ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ActualizarSupabaseUserIdAsync(usuario.Id, supabaseUserId))
            .ReturnsAsync(true);

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(Array.Empty<Permiso>());

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Transnic" });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _usuarioRepository
            .Setup(r => r.ResetearIntentosFallidosAsync(usuario.Id))
            .Returns(Task.CompletedTask);
        _usuarioRepository
            .Setup(r => r.ActualizarUltimoAccesoAsync(usuario.Id))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = CrearServiceConClaveConocidaConHttpFactory(ClaveTotpConocida, httpFactory.Object);

        var resultado = await service.LoginConOAuthAsync(new OAuthCallbackRequestDto
        {
            Provider = "microsoft",
            SupabaseToken = "token-valido"
        });

        resultado.Usuario.Email.Should().Be("juan@transnic.com");

        _usuarioRepository.Verify(
            r => r.ActualizarSupabaseUserIdAsync(usuario.Id, supabaseUserId), Times.Once);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoEmailNuevoConInvitacionPendiente_AutoprovisionaUsuario()
    {
        // HU-004 CA-04: email sin usuario pero con invitación PENDING → se crea el
        // usuario con perfil/empresa de la invitación y se marca como aceptada.
        var supabaseUserId = Guid.NewGuid();
        var invitacionId = Guid.NewGuid();
        var nuevoUsuarioId = Guid.NewGuid();

        var invitacion = new Invitacion
        {
            Id = invitacionId,
            EmpresaId = EmpresaId,
            Email = "nuevo@transnic.com",
            PerfilId = PerfilId,
            Token = "token-invitacion",
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(48 * 60),
            FechaCreacion = DateTime.UtcNow
        };

        var httpFactory = HttpFactoryConSupabaseUser(supabaseUserId, "nuevo@transnic.com");

        _usuarioRepository
            .Setup(r => r.GetBySupabaseUserIdAsync(supabaseUserId))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync("nuevo@transnic.com"))
            .ReturnsAsync((Usuario?)null);
        _invitacionRepository
            .Setup(r => r.GetPendienteByEmailAsync("nuevo@transnic.com"))
            .ReturnsAsync(invitacion);
        _perfilRepository
            .Setup(r => r.GetByIdAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new Perfil
            {
                Id = PerfilId,
                EmpresaId = EmpresaId,
                Nombre = "Operador",
                TipoPerfil = TipoPerfil.OPERADOR,
                Activo = true
            });
        _usuarioRepository
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(nuevoUsuarioId);
        _invitacionRepository
            .Setup(r => r.MarcarAceptadaAsync(invitacionId, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(Array.Empty<Permiso>());

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Transnic" });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _usuarioRepository
            .Setup(r => r.ResetearIntentosFallidosAsync(nuevoUsuarioId))
            .Returns(Task.CompletedTask);
        _usuarioRepository
            .Setup(r => r.ActualizarUltimoAccesoAsync(nuevoUsuarioId))
            .Returns(Task.CompletedTask);
        _usuarioRepository
            .Setup(r => r.ActualizarSupabaseUserIdAsync(nuevoUsuarioId, supabaseUserId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = CrearServiceConClaveConocidaConHttpFactory(ClaveTotpConocida, httpFactory.Object);

        var resultado = await service.LoginConOAuthAsync(new OAuthCallbackRequestDto
        {
            Provider = "google",
            SupabaseToken = "token-valido"
        });

        resultado.Usuario.Email.Should().Be("nuevo@transnic.com");
        resultado.Usuario.TipoUsuario.Should().Be(TipoUsuario.OPERADOR);

        _invitacionRepository.Verify(
            r => r.MarcarAceptadaAsync(invitacionId, It.IsAny<DateTime>()), Times.Once);
        _usuarioRepository.Verify(
            r => r.CreateAsync(It.Is<Usuario>(u => u.Email == "nuevo@transnic.com")), Times.Once);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoEmailNuevoSinInvitacion_LanzaBusinessException()
    {
        // HU-004 CA-05: email sin usuario y sin invitación → acceso denegado 422.
        var supabaseUserId = Guid.NewGuid();

        var httpFactory = HttpFactoryConSupabaseUser(supabaseUserId, "desconocido@external.com");

        _usuarioRepository
            .Setup(r => r.GetBySupabaseUserIdAsync(supabaseUserId))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepository
            .Setup(r => r.GetByEmailGlobalAsync("desconocido@external.com"))
            .ReturnsAsync((Usuario?)null);
        _invitacionRepository
            .Setup(r => r.GetPendienteByEmailAsync("desconocido@external.com"))
            .ReturnsAsync((Invitacion?)null);

        var service = CrearServiceConClaveConocidaConHttpFactory(ClaveTotpConocida, httpFactory.Object);

        var act = async () => await service.LoginConOAuthAsync(new OAuthCallbackRequestDto
        {
            Provider = "google",
            SupabaseToken = "token-valido"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*invitación*");
    }

    // ── Refresh: usuario inválido ──────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_CuandoUsuarioInactivo_LanzaBusinessException()
    {
        var refreshToken = "some-refresh";
        var hash = _jwtService.HashRefreshToken(refreshToken);
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            RefreshTokenHash = hash,
            FechaExpiracion = DateTime.UtcNow.AddDays(30),
            Activa = true,
            FechaCreacion = DateTime.UtcNow
        };
        var usuario = UsuarioActivo();
        usuario.Activo = false;

        _sesionRepository
            .Setup(r => r.GetByRefreshTokenHashAsync(hash))
            .ReturnsAsync(sesion);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(usuario);

        var act = async () => await _service.RefreshAsync(new RefreshTokenRequestDto
        {
            RefreshToken = refreshToken
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("Token inválido");
    }

    // ── GetRequestContext con HttpContext ──────────────────────────

    [Fact]
    public async Task LoginAsync_CuandoHttpContextPresente_UsaIpYUserAgentEnAuditoria()
    {
        // GetRequestContext con HttpContext: captura IP + User-Agent.
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.5");
        httpContext.Request.Headers.UserAgent = "integration-test/1.0";

        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        ConfigurarLoginExitoso();

        await _service.LoginAsync(new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        _auditoria.Verify(
            a => a.RegistrarAsync("auth", AccionAuditoria.LOGIN, EmpresaId, UsuarioId,
                nameof(Usuario), UsuarioId,
                It.IsAny<object>(), "203.0.113.5", "integration-test/1.0"),
            Times.Once);
    }

    // ── 2FA (HU-005) ────────────────────────────────────────────────

    /// <summary>Replica la lógica HashCodigo del AuthService — SHA-256 hex.</summary>
    private static string HashCodigo(string codigo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Configura los mocks mínimos para ObtenerUsuarioActivoAsync.</summary>
    private void ConfigurarUsuarioExiste()
    {
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());
    }

    // ── Setup2faAsync ─────────────────────────────────────────────

    [Fact]
    public async Task Setup2faAsync_CuandoUsuarioNoExiste_LanzaNotFoundException()
    {
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.Setup2faAsync(UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Contain(nameof(Usuario));
    }

    [Fact]
    public async Task Setup2faAsync_CuandoUsuarioExiste_CreaConfiguracionPendiente()
    {
        ConfigurarUsuarioExiste();

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        _config2faRepository
            .Setup(r => r.CreateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Setup2faAsync(UsuarioId, EmpresaId);

        result.Should().NotBeNull();
        result.Secret.Should().NotBeNullOrEmpty();
        result.QrCodeUrl.Should().Contain("otpauth://totp/");
        result.QrCodeUrl.Should().Contain(Uri.EscapeDataString("juan@transnic.com"));
        result.CodigosRecuperacion.Should().HaveCount(8);

        _config2faRepository.Verify(
            r => r.CreateAsync(It.Is<Configuracion2fa>(c =>
                c.EmpresaId == EmpresaId &&
                c.UsuarioId == UsuarioId &&
                c.TotpHabilitado == false &&
                c.EmailHabilitado == false &&
                c.CodigosRecuperacion.Length == 8)),
            Times.Once);
    }

    [Fact]
    public async Task Setup2faAsync_CuandoConfigExistente_ActualizaEnLugarDeCrear()
    {
        ConfigurarUsuarioExiste();

        var configExistente = new Configuracion2fa
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            TotpSecret = "old-secret",
            TotpHabilitado = false,
            EmailHabilitado = false,
            CodigosRecuperacion = [],
            Activo = true
        };

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(configExistente);

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Setup2faAsync(UsuarioId, EmpresaId);

        result.Should().NotBeNull();
        result.Secret.Should().NotBeNullOrEmpty();
        result.CodigosRecuperacion.Should().HaveCount(8);

        _config2faRepository.Verify(r => r.CreateAsync(It.IsAny<Configuracion2fa>()), Times.Never);
        _config2faRepository.Verify(
            r => r.UpdateAsync(It.Is<Configuracion2fa>(c =>
                c.Id == configExistente.Id &&
                c.TotpHabilitado == false &&
                c.CodigosRecuperacion.Length == 8)),
            Times.Once);
    }

    [Fact]
    public async Task Setup2faAsync_QrCodeUrlContieneEmailDelUsuario()
    {
        ConfigurarUsuarioExiste();

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        _config2faRepository
            .Setup(r => r.CreateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Setup2faAsync(UsuarioId, EmpresaId);

        result.QrCodeUrl.Should().Contain(Uri.EscapeDataString("juan@transnic.com"));
        result.QrCodeUrl.Should().Contain("issuer=");
        result.QrCodeUrl.Should().Contain("algorithm=SHA1");
        result.QrCodeUrl.Should().Contain("digits=6");
    }

    [Fact]
    public async Task Setup2faAsync_SecretNoEsVacioString()
    {
        ConfigurarUsuarioExiste();

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        _config2faRepository
            .Setup(r => r.CreateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Setup2faAsync(UsuarioId, EmpresaId);

        result.Secret.Should().NotBeNullOrWhiteSpace();
        // Base32 output contains only A-Z and 2-7
        result.Secret.Should().MatchRegex("^[A-Z2-7]+$");
    }

    // ── Activar2faAsync ───────────────────────────────────────────

    [Fact]
    public async Task Activar2faAsync_CuandoNoExisteConfig_LanzaBusinessException()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        var act = async () => await _service.Activar2faAsync(
            new Activar2faRequestDto { Tipo = "TOTP", Codigo = "123456" },
            UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("setup");
    }

    [Fact]
    public async Task Activar2faAsync_CuandoSecretVacio_LanzaBusinessException()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = null,
                TotpHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        var act = async () => await _service.Activar2faAsync(
            new Activar2faRequestDto { Tipo = "TOTP", Codigo = "123456" },
            UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("setup");
    }

    [Fact]
    public async Task Activar2faAsync_CuandoCodigoInvalido_LanzaBusinessException()
    {
        // Arrange: crear config con secret cifrado y verificar con código inválido.
        // Usar service con clave conocida para poder cifrar/descifrar el secret TOTP.
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var config = new Configuracion2fa
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
            TotpHabilitado = false,
            Activo = true,
            CodigosRecuperacion = []
        };

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(config);

        var act = async () => await service.Activar2faAsync(
            new Activar2faRequestDto { Tipo = "TOTP", Codigo = "000000" },
            UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("inválido");
    }

    [Fact]
    public async Task Activar2faAsync_CuandoCodigoValido_ActivaYActualiza()
    {
        // Arrange: generar un código TOTP válido con el secret conocido.
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var totp = new Totp(Base32Encoding.ToBytes(secretB32), step: 30,
            mode: OtpHashMode.Sha1, totpSize: 6);
        var codigoValido = totp.ComputeTotp(DateTime.UtcNow);

        var config = new Configuracion2fa
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
            TotpHabilitado = false,
            Activo = true,
            CodigosRecuperacion = []
        };

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(config);

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await service.Activar2faAsync(
            new Activar2faRequestDto { Tipo = "TOTP", Codigo = codigoValido },
            UsuarioId, EmpresaId);

        result.Should().BeTrue();

        _config2faRepository.Verify(
            r => r.UpdateAsync(It.Is<Configuracion2fa>(c =>
                c.Id == config.Id && c.TotpHabilitado == true)),
            Times.Once);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", "ACTIVAR_2FA", EmpresaId, UsuarioId,
                nameof(Configuracion2fa), config.Id,
                It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    // ── Desactivar2faAsync ────────────────────────────────────────

    [Fact]
    public async Task Desactivar2faAsync_CuandoNoExisteConfig_LanzaNotFoundException()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        var act = async () => await _service.Desactivar2faAsync(UsuarioId, EmpresaId, "123456");

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Contain(nameof(Configuracion2fa));
    }

    [Fact]
    public async Task Desactivar2faAsync_CuandoTotpHabilitadoYCodigoIncorrecto_LanzaBusinessException()
    {
        // Usar service con clave conocida para TOTP decrypt.
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        var act = async () => await service.Desactivar2faAsync(UsuarioId, EmpresaId, "000000");

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Código incorrecto");
    }

    [Fact]
    public async Task Desactivar2faAsync_CuandoEmailHabilitadoYCodigoTemporalValido_Desactiva()
    {
        var codigo = "123456";
        var codigoHash = HashCodigo(codigo);
        var codigoId = Guid.NewGuid();

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = null,
                TotpHabilitado = false,
                EmailHabilitado = true,
                Activo = true,
                CodigosRecuperacion = []
            });

        _config2faRepository
            .Setup(r => r.GetCodigoTemporalValidoAsync(UsuarioId, codigoHash))
            .ReturnsAsync(new Codigo2faTempora
            {
                Id = codigoId,
                UsuarioId = UsuarioId,
                CodigoHash = codigoHash,
                Tipo = "EMAIL",
                Usado = false,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(5)
            });

        _config2faRepository
            .Setup(r => r.MarcarCodigoUsadoAsync(codigoId))
            .ReturnsAsync(true);

        _config2faRepository
            .Setup(r => r.DeactivateAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Desactivar2faAsync(UsuarioId, EmpresaId, codigo);

        result.Should().BeTrue();

        _config2faRepository.Verify(r => r.MarcarCodigoUsadoAsync(codigoId), Times.Once);
        _config2faRepository.Verify(r => r.DeactivateAsync(UsuarioId, EmpresaId), Times.Once);
    }

    [Fact]
    public async Task Desactivar2faAsync_CuandoEmailHabilitadoPeroCodigoTemporalInvalido_LanzaBusinessException()
    {
        var codigoHash = HashCodigo("invalido");

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = null,
                TotpHabilitado = false,
                EmailHabilitado = true,
                Activo = true,
                CodigosRecuperacion = []
            });

        _config2faRepository
            .Setup(r => r.GetCodigoTemporalValidoAsync(UsuarioId, codigoHash))
            .ReturnsAsync((Codigo2faTempora?)null);

        var act = async () => await _service.Desactivar2faAsync(UsuarioId, EmpresaId, "invalido");

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Código incorrecto");
    }

    [Fact]
    public async Task Desactivar2faAsync_CuandoTotpHabilitadoYCodigoValido_Desactiva()
    {
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var totp = new Totp(Base32Encoding.ToBytes(secretB32), step: 30,
            mode: OtpHashMode.Sha1, totpSize: 6);
        var codigoValido = totp.ComputeTotp(DateTime.UtcNow);

        var config = new Configuracion2fa
        {
            Id = Guid.NewGuid(),
            EmpresaId = EmpresaId,
            UsuarioId = UsuarioId,
            TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
            TotpHabilitado = true,
            EmailHabilitado = false,
            Activo = true,
            CodigosRecuperacion = []
        };

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(config);

        _config2faRepository
            .Setup(r => r.DeactivateAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await service.Desactivar2faAsync(UsuarioId, EmpresaId, codigoValido);

        result.Should().BeTrue();

        _config2faRepository.Verify(r => r.DeactivateAsync(UsuarioId, EmpresaId), Times.Once);
    }

    [Fact]
    public async Task Desactivar2faAsync_CuandoDeactivateRetornaFalse_NoRegistraAuditoria()
    {
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var totp = new Totp(Base32Encoding.ToBytes(secretB32), step: 30,
            mode: OtpHashMode.Sha1, totpSize: 6);
        var codigoValido = totp.ComputeTotp(DateTime.UtcNow);

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        _config2faRepository
            .Setup(r => r.DeactivateAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(false);

        var result = await service.Desactivar2faAsync(UsuarioId, EmpresaId, codigoValido);

        result.Should().BeFalse();

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", "DESACTIVAR_2FA", EmpresaId, UsuarioId,
                It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    // ── Verificar2faAsync ─────────────────────────────────────────

    [Fact]
    public async Task Verificar2faAsync_CuandoTempTokenInvalido_LanzaBusinessException()
    {
        var act = async () => await _service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = "token-no-valido",
            Codigo = "123456"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Sesión 2FA inválida");
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoUsuarioNoExiste_LanzaBusinessException()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);

        // El GetByIdAsync llama con (payload.UserId, payload.EmpresaId)
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = "123456"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Usuario inválido");
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoUsuarioInactivo_LanzaBusinessException()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);

        var usuario = UsuarioActivo();
        usuario.Activo = false;

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(usuario);

        var act = async () => await _service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = "123456"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Usuario inválido");
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoConfigNull_LanzaBusinessException()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        var act = async () => await _service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = "123456"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("no tiene 2FA activo");
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoTotpHabilitadoYCodigoInvalido_LanzaBusinessException()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        var act = async () => await service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = "000000" // código inválido
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("inválido");
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoCodigoRecuperacionValido_RetornaLoginCompleto()
    {
        // Arrange: usar código de recuperación cuyo hash coincida.
        var codigo = "ABCD-1234";
        var codigoHash = HashCodigo(codigo);

        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());

        // Config con TotpHabilitado=true (para que entre al TOTP check primero, falla → recovery)
        // TotpSecret con clave conocida para que el decrypt funcione (pero el código no coincide).
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = new[] { codigoHash }
            });

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        // Configurar mocks de CompletarLoginAsync
        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new List<Permiso>());

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Trans Nicaragua S.A." });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = codigo
        });

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.Usuario.Should().NotBeNull();
        result.Usuario.Email.Should().Be("juan@transnic.com");

        // Verificar que se actualizó la config (código usado removido)
        _config2faRepository.Verify(
            r => r.UpdateAsync(It.Is<Configuracion2fa>(c =>
                !c.CodigosRecuperacion.Contains(codigoHash))),
            Times.Once);
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoCodigoTotpValido_RetornaLoginCompleto()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);
        var service = CrearServiceConClaveConocida(ClaveTotpConocida);
        var secretB32 = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var totp = new Totp(Base32Encoding.ToBytes(secretB32), step: 30,
            mode: OtpHashMode.Sha1, totpSize: 6);
        var codigoValido = totp.ComputeTotp(DateTime.UtcNow);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(UsuarioActivo());

        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpSecret = AesGcmEncryptor.Encrypt(secretB32, ClaveTotpConocida),
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new List<Permiso>());

        _empresaRepository
            .Setup(r => r.GetByIdAsync(EmpresaId))
            .ReturnsAsync(new Empresa { Id = EmpresaId, Nombre = "Trans Nicaragua S.A." });

        _sesionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Sesion>()))
            .ReturnsAsync(Guid.NewGuid());

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = codigoValido
        });

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.Usuario.Id.Should().Be(UsuarioId);
    }

    [Fact]
    public async Task Verificar2faAsync_CuandoUsuarioEstadoNoActive_LanzaBusinessException()
    {
        var tempToken = _jwtService.GenerateTempToken(UsuarioId, EmpresaId, 5);

        var usuario = UsuarioActivo();
        usuario.Estado = EstadoUsuario.SUSPENDED;

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(usuario);

        var act = async () => await _service.Verificar2faAsync(new Verificar2faRequestDto
        {
            TempToken = tempToken,
            Codigo = "123456"
        });

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("Usuario inválido");
    }

    // ── RegenerarRecoveryCodesAsync ───────────────────────────────

    [Fact]
    public async Task RegenerarRecoveryCodesAsync_CuandoNoExisteConfig_LanzaNotFoundException()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync((Configuracion2fa?)null);

        var act = async () => await _service.RegenerarRecoveryCodesAsync(UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Contain(nameof(Configuracion2fa));
    }

    [Fact]
    public async Task RegenerarRecoveryCodesAsync_CuandoSin2faActivo_LanzaBusinessException()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpHabilitado = false,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = ["hash1", "hash2"]
            });

        var act = async () => await _service.RegenerarRecoveryCodesAsync(UsuarioId, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Message.Should().Contain("No tienes 2FA activo");
    }

    [Fact]
    public async Task RegenerarRecoveryCodesAsync_CuandoTotpHabilitado_Regenera8Codigos()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = ["old-hash-1", "old-hash-2"]
            });

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RegenerarRecoveryCodesAsync(UsuarioId, EmpresaId);

        result.Should().HaveCount(8);
        result.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c));

        _config2faRepository.Verify(
            r => r.UpdateAsync(It.Is<Configuracion2fa>(c =>
                c.CodigosRecuperacion.Length == 8)),
            Times.Once);
    }

    [Fact]
    public async Task RegenerarRecoveryCodesAsync_CuandoEmailHabilitado_Regenera8Codigos()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpHabilitado = false,
                EmailHabilitado = true,
                Activo = true,
                CodigosRecuperacion = []
            });

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RegenerarRecoveryCodesAsync(UsuarioId, EmpresaId);

        result.Should().HaveCount(8);
    }

    [Fact]
    public async Task RegenerarRecoveryCodesAsync_RegistraAuditoria()
    {
        _config2faRepository
            .Setup(r => r.GetByUsuarioIdAsync(UsuarioId, EmpresaId))
            .ReturnsAsync(new Configuracion2fa
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                UsuarioId = UsuarioId,
                TotpHabilitado = true,
                EmailHabilitado = false,
                Activo = true,
                CodigosRecuperacion = []
            });

        _config2faRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Configuracion2fa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _service.RegenerarRecoveryCodesAsync(UsuarioId, EmpresaId);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "auth", "2FA_CODIGOS_REGENERADOS", EmpresaId, UsuarioId,
                It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    // ── Helper: crear AuthService con clave TOTP conocida ──────────

    /// <summary>Clave TOTP de encriptación usada en tests (reemplaza RandomKeyFallback).</summary>
    private static readonly string ClaveTotpConocida = "TestTotpKey2026ParaFreiroute!123";

    /// <summary>Crea un AuthService con una clave de encriptación TOTP conocida (para tests de cifrado/descifrado).</summary>
    private AuthService CrearServiceConClaveConocida(string claveConocida)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:TotpEncryptionKey", claveConocida }
            })
            .Build();

        return new AuthService(
            _usuarioRepository.Object,
            _permisoRepository.Object,
            _empresaRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            _config2faRepository.Object,
            _supabaseAuth.Object,
            _jwtService,
            _auditoria.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            config,
            _jwtSettings,
            _appSettings,
            _logger.Object,
            _perfilRepository.Object,
            _httpClientFactory.Object,_storageService.Object);
    }

    /// <summary>
    /// Crea un AuthService con una clave TOTP conocida y un IHttpClientFactory
    /// custom (para los tests de OAuth — HU-004).
    /// </summary>
    private AuthService CrearServiceConClaveConocidaConHttpFactory(
        string claveConocida, IHttpClientFactory httpClientFactory)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:TotpEncryptionKey", claveConocida },
                { "Supabase:Url", "http://localhost:54321" },
                { "Supabase:AnonKey", "anon-test-key" }
            })
            .Build();

        return new AuthService(
            _usuarioRepository.Object,
            _permisoRepository.Object,
            _empresaRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            _config2faRepository.Object,
            _supabaseAuth.Object,
            _jwtService,
            _auditoria.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            config,
            _jwtSettings,
            _appSettings,
            _logger.Object,
            _perfilRepository.Object,
            httpClientFactory,
            new Mock<IStorageService>().Object);
    }

    /// <summary>
    /// Crea un IHttpClientFactory cuyo client "SupabaseAuth" responde el GET
    /// /auth/v1/user con un usuario de Supabase válido (id + email).
    /// </summary>
    private static Mock<IHttpClientFactory> HttpFactoryConSupabaseUser(Guid supabaseUserId, string email)
    {
        var body = $$"""{ "id": "{{supabaseUserId}}", "email": "{{email}}" }""";
        var httpClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }))
        {
            BaseAddress = new Uri("http://localhost:54321")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SupabaseAuth")).Returns(httpClient);
        return factory;
    }

    /// <summary>Handler HTTP de test que devuelve SIEMPRE la respuesta programada.</summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _respuesta;

        public StubHttpMessageHandler(HttpResponseMessage respuesta)
        {
            _respuesta = respuesta;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_respuesta);
        }
    }
}
