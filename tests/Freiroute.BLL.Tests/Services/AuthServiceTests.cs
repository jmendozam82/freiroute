using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using FluentAssertions;

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
    private readonly Mock<ISesionRepository> _sesionRepository;
    private readonly Mock<ISupabaseAuthService> _supabaseAuth;
    private readonly IJwtService _jwtService;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _usuarioRepository = new Mock<IUsuarioRepository>();
        _permisoRepository = new Mock<IPermisoRepository>();
        _empresaRepository = new Mock<IEmpresaRepository>();
        _invitacionRepository = new Mock<IInvitacionRepository>();
        _sesionRepository = new Mock<ISesionRepository>();
        _supabaseAuth = new Mock<ISupabaseAuthService>();
        _auditoria = new Mock<IAuditoriaService>();
        _emailService = new Mock<IEmailService>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
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

        _service = new AuthService(
            _usuarioRepository.Object,
            _permisoRepository.Object,
            _empresaRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            _supabaseAuth.Object,
            _jwtService,
            _auditoria.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            _jwtSettings,
            _appSettings,
            _logger.Object);
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
            .Setup(s => s.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

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
            s => s.UpdatePasswordAsync(usuario.SupabaseUserId.Value, "NuevaPassword123!"),
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
}
