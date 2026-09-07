using System.Net;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de los flujos nuevos del Sprint 3 (G-06 y HU-004):
/// — ResetPasswordAsync (G-06-D/E): cambia la contraseña en Supabase Auth con
///   token de admin, invalida TODAS las sesiones y consume el token de un solo uso.
/// — LoginConOAuthAsync (HU-004): valida el token contra GET /auth/v1/user,
///   resuelve el usuario (vinculo → email → autoprovisionamiento CA-04) y emite
///   el JWT interno con campaña de auditoría LOGIN_OAUTH.
/// </summary>
public class AuthServiceG06OAuthTests
{
    private const string TestSecret = "EstaEsUnaClaveSecretaParaTesting2026SoloDevLocal";
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid PerfilId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid SupabaseUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

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
    private readonly AuthService _service;

    public AuthServiceG06OAuthTests()
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
        _jwtService = new JwtService(_jwtSettings);

        var configConClave = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:TotpEncryptionKey", "ClaveDePruebaParaTotp2026SoloTestsQA" }
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
            new Mock<IStorageService>().Object);
    }

    private const string JsonUsuarioSupabase =
        """{"id":"22222222-2222-2222-2222-222222222222","email":"juan@transnic.com"}""";

    private void ConfigurarSupabaseAuthOk(HttpStatusCode status = HttpStatusCode.OK, string json = JsonUsuarioSupabase)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://supabase.test") };
        _httpClientFactory.Setup(f => f.CreateClient("SupabaseAuth")).Returns(client);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private Usuario UsuarioVinculado() => new()
    {
        Id = UsuarioId,
        EmpresaId = EmpresaId,
        PerfilId = PerfilId,
        NombreCompleto = "Juan Pérez",
        Email = "juan@transnic.com",
        SupabaseUserId = SupabaseUserId,
        TipoUsuario = TipoUsuario.DISPATCHER,
        Estado = EstadoUsuario.ACTIVE,
        Activo = true,
        IntentosFallidos = 0,
        FechaCreacion = DateTime.UtcNow
    };

    private void ConfigurarLoginExitoso(Usuario usuario)
    {
        _usuarioRepository.Setup(r => r.GetBySupabaseUserIdAsync(SupabaseUserId)).ReturnsAsync(usuario);
        _usuarioRepository.Setup(r => r.ResetearIntentosFallidosAsync(usuario.Id)).Returns(Task.CompletedTask);
        _usuarioRepository.Setup(r => r.ActualizarUltimoAccesoAsync(usuario.Id)).Returns(Task.CompletedTask);
        _permisoRepository.Setup(r => r.GetByPerfilAsync(usuario.PerfilId, usuario.EmpresaId))
            .ReturnsAsync(new List<Permiso>
            {
                new() { Modulo = "embarques", PuedeLeer = true, Activo = true }
            });
        _empresaRepository.Setup(r => r.GetByIdAsync(usuario.EmpresaId))
            .ReturnsAsync(new Empresa { Id = usuario.EmpresaId, Nombre = "Trans Nicaragua S.A." });
        _sesionRepository.Setup(r => r.CreateAsync(It.IsAny<Sesion>())).ReturnsAsync(Guid.NewGuid());
        _auditoria.Setup(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
    }

    // ── HU-004 OAuth ──────────────────────────────────────────────

    [Fact]
    public async Task LoginConOAuthAsync_CuandoTokenVacio_LanzaBusinessException()
    {
        var act = async () => await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "  " });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Token de OAuth inválido. Vuelva a intentar el inicio de sesión.");
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoSupabaseResponde401_NoVinculaYaLanza()
    {
        ConfigurarSupabaseAuthOk(HttpStatusCode.Unauthorized, "{}");

        var act = async () => await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "token-vencido" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Token de OAuth inválido o expirado. Vuelva a intentar el inicio de sesión.");
        _usuarioRepository.Verify(r => r.GetBySupabaseUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoUsuarioYaVinculado_RetornaLoginSinReVincular()
    {
        ConfigurarSupabaseAuthOk();
        var usuario = UsuarioVinculado();
        ConfigurarLoginExitoso(usuario);

        var result = await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "token-valido" });

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Usuario.Nombre.Should().Be("Juan Pérez");
        result.Usuario.EmpresaNombre.Should().Be("Trans Nicaragua S.A.");
        result.Usuario.Permisos.Should().Contain("embarques:read");
        // Ya vinculado → no debe llamar al UPDATE del vínculo.
        _usuarioRepository.Verify(r => r.ActualizarSupabaseUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _auditoria.Verify(a => a.RegistrarAsync("auth", AccionAuditoria.LOGIN_OAUTH, EmpresaId,
            UsuarioId, "Usuario", UsuarioId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoNoVinculadoPeroMismoEmail_VinculaSupabaseUserId()
    {
        ConfigurarSupabaseAuthOk();
        var usuario = UsuarioVinculado();
        usuario.SupabaseUserId = null;
        ConfigurarLoginExitoso(usuario);
        // El usuario existía pero no estaba vinculado: resolución por email.
        _usuarioRepository.Setup(r => r.GetBySupabaseUserIdAsync(SupabaseUserId)).ReturnsAsync((Usuario?)null);
        _usuarioRepository.Setup(r => r.GetByEmailGlobalAsync("juan@transnic.com")).ReturnsAsync(usuario);
        _usuarioRepository.Setup(r => r.ActualizarSupabaseUserIdAsync(UsuarioId, SupabaseUserId)).ReturnsAsync(true);

        var result = await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "microsoft", SupabaseToken = "token-valido" });

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        _usuarioRepository.Verify(r => r.ActualizarSupabaseUserIdAsync(UsuarioId, SupabaseUserId), Times.Once);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoCuentaPendiente_LanzaBusinessException()
    {
        ConfigurarSupabaseAuthOk();
        var usuario = UsuarioVinculado();
        usuario.Estado = EstadoUsuario.PENDING;
        _usuarioRepository.Setup(r => r.GetBySupabaseUserIdAsync(SupabaseUserId)).ReturnsAsync(usuario);
        _auditoria.Setup(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        var act = async () => await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "token-valido" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Cuenta pendiente de activación. Revise su email.");
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoAutoprovisionaDesdeInvitacion_CreaUsuarioYAudita()
    {
        ConfigurarSupabaseAuthOk();
        var usuario = UsuarioVinculado();
        ConfigurarLoginExitoso(usuario);
        // El usuario NO existe todavía: ni vinculado (supabase_user_id) ni por email.
        _usuarioRepository.Setup(r => r.GetBySupabaseUserIdAsync(SupabaseUserId)).ReturnsAsync((Usuario?)null);
        _usuarioRepository.Setup(r => r.GetByEmailGlobalAsync("juan@transnic.com")).ReturnsAsync((Usuario?)null);
        _invitacionRepository.Setup(r => r.GetPendienteByEmailAsync("juan@transnic.com"))
            .ReturnsAsync(new Invitacion
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                Email = "juan@transnic.com",
                Token = "tok-oauth",
                Estado = "PENDING",
                FechaExpiracion = DateTime.UtcNow.AddHours(48)
            });
        _perfilRepository.Setup(r => r.GetByIdAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new Perfil { Id = PerfilId, EmpresaId = EmpresaId, Nombre = "Dispatcher", TipoPerfil = TipoPerfil.DISPATCHER, Activo = true });
        _usuarioRepository.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync(UsuarioId);
        _invitacionRepository.Setup(r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(true);

        var result = await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "token-valido" });

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        _usuarioRepository.Verify(r => r.CreateAsync(It.Is<Usuario>(u => u.Email == "juan@transnic.com")), Times.Once);
        _invitacionRepository.Verify(r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task LoginConOAuthAsync_CuandoSinInvitacionPendiente_LanzaBusinessException()
    {
        ConfigurarSupabaseAuthOk();
        _usuarioRepository.Setup(r => r.GetBySupabaseUserIdAsync(SupabaseUserId)).ReturnsAsync((Usuario?)null);
        _usuarioRepository.Setup(r => r.GetByEmailGlobalAsync("juan@transnic.com")).ReturnsAsync((Usuario?)null);
        _invitacionRepository.Setup(r => r.GetPendienteByEmailAsync("juan@transnic.com")).ReturnsAsync((Invitacion?)null);

        var act = async () => await _service.LoginConOAuthAsync(
            new OAuthCallbackRequestDto { Provider = "google", SupabaseToken = "token-valido" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("No tienes acceso a Freiroute con esta cuenta.*");
    }

    // ── G-06 Reset password ───────────────────────────────────────

    private Invitacion InvitacionPendiente() => new()
    {
        Id = Guid.NewGuid(),
        EmpresaId = EmpresaId,
        Email = "juan@transnic.com",
        Token = "token-reset",
        Estado = "PENDING",
        FechaExpiracion = DateTime.UtcNow.AddMinutes(30)
    };

    [Fact]
    public async Task ResetPasswordAsync_CuandoTokenValido_CambiaPasswordInvalidaSesionesYAudita()
    {
        var invitacion = InvitacionPendiente();
        _invitacionRepository.Setup(r => r.GetByTokenAsync("token-reset")).ReturnsAsync(invitacion);
        _usuarioRepository.Setup(r => r.GetByEmailAsync("juan@transnic.com", EmpresaId))
            .ReturnsAsync(UsuarioVinculado());
        _supabaseAuth.Setup(s => s.CambiarPasswordAsync(SupabaseUserId, It.IsAny<string>())).ReturnsAsync(true);
        _sesionRepository.Setup(r => r.RevocarTodasPorUsuarioAsync(UsuarioId)).ReturnsAsync(true);
        _invitacionRepository.Setup(r => r.MarcarAceptadaAsync(invitacion.Id, It.IsAny<DateTime>())).ReturnsAsync(true);
        _auditoria.Setup(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "token-reset", NewPassword = "NuevaClave123!" });

        _supabaseAuth.Verify(s => s.CambiarPasswordAsync(SupabaseUserId, "NuevaClave123!"), Times.Once);
        _invitacionRepository.Verify(r => r.MarcarAceptadaAsync(invitacion.Id, It.IsAny<DateTime>()), Times.Once);
        _sesionRepository.Verify(r => r.RevocarTodasPorUsuarioAsync(UsuarioId), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("auth", "RESET_PASSWORD", EmpresaId,
            UsuarioId, "Usuario", UsuarioId, null, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoTokenInexistente_LanzaBusinessException()
    {
        _invitacionRepository.Setup(r => r.GetByTokenAsync("token-falso")).ReturnsAsync((Invitacion?)null);

        var act = async () => await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "token-falso", NewPassword = "NuevaClave123!" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Token inválido o expirado");
        _supabaseAuth.Verify(s => s.CambiarPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoInvitacionExpirada_LanzaBusinessException()
    {
        var invitacion = InvitacionPendiente();
        invitacion.FechaExpiracion = DateTime.UtcNow.AddMinutes(-5);
        _invitacionRepository.Setup(r => r.GetByTokenAsync("token-expirado")).ReturnsAsync(invitacion);

        var act = async () => await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "token-expirado", NewPassword = "NuevaClave123!" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Token inválido o expirado");
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoUsuarioSinSupabase_LanzaBusinessException()
    {
        var invitacion = InvitacionPendiente();
        _invitacionRepository.Setup(r => r.GetByTokenAsync("token-reset")).ReturnsAsync(invitacion);
        var usuario = UsuarioVinculado();
        usuario.SupabaseUserId = null;
        _usuarioRepository.Setup(r => r.GetByEmailAsync("juan@transnic.com", EmpresaId)).ReturnsAsync(usuario);

        var act = async () => await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "token-reset", NewPassword = "NuevaClave123!" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("El usuario no tiene cuenta en Supabase Auth. Contacta al administrador.");
    }

    [Fact]
    public async Task ResetPasswordAsync_CuandoSupabaseNoCompletaElCambio_LanzaBusinessException()
    {
        var invitacion = InvitacionPendiente();
        _invitacionRepository.Setup(r => r.GetByTokenAsync("token-reset")).ReturnsAsync(invitacion);
        _usuarioRepository.Setup(r => r.GetByEmailAsync("juan@transnic.com", EmpresaId))
            .ReturnsAsync(UsuarioVinculado());
        _supabaseAuth.Setup(s => s.CambiarPasswordAsync(SupabaseUserId, It.IsAny<string>())).ReturnsAsync(false);

        var act = async () => await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "token-reset", NewPassword = "NuevaClave123!" });

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("No se pudo actualizar la contraseña. Intente nuevamente.");
        // El token de un solo uso NO se consume si el cambio falló.
        _invitacionRepository.Verify(r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }
}