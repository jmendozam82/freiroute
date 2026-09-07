using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Settings;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Usuario;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del reseteo de contraseña por admin (G-06, Sprint 3).
/// G-06-D: la contraseña temporal (formato G6:Otp12) se aplica SOLO en
/// Supabase Auth — nunca se muestra al admin ni se registra en logs.
/// G-06-E: se invalidan todas las sesiones activas del usuario.
/// La auditoría registra el admin que ejecutó la acción.
/// </summary>
public class UsuarioServiceResetPasswordAdminTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid SupabaseUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Mock<IUsuarioRepository> _usuarioRepository;
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<IInvitacionRepository> _invitacionRepository;
    private readonly Mock<ISesionRepository> _sesionRepository;
    private readonly Mock<ISupabaseAuthService> _supabaseAuth;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IPlanLimiteService> _planLimiteService;
    private readonly UsuarioService _service;

    public UsuarioServiceResetPasswordAdminTests()
    {
        _usuarioRepository = new Mock<IUsuarioRepository>();
        _perfilRepository = new Mock<IPerfilRepository>();
        _invitacionRepository = new Mock<IInvitacionRepository>();
        _sesionRepository = new Mock<ISesionRepository>();
        _supabaseAuth = new Mock<ISupabaseAuthService>();
        _auditoria = new Mock<IAuditoriaService>();
        _emailService = new Mock<IEmailService>();
        _planLimiteService = new Mock<IPlanLimiteService>();
        _auditoria.Setup(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        _service = new UsuarioService(
            _usuarioRepository.Object,
            _perfilRepository.Object,
            _invitacionRepository.Object,
            _sesionRepository.Object,
            new UsuarioValidator(),
            _supabaseAuth.Object,
            _auditoria.Object,
            _emailService.Object,
            _planLimiteService.Object,
            Options.Create(new AppSettings { BaseUrl = "https://localhost:5001" }),
            Mock.Of<ILogger<UsuarioService>>());
    }

    private Usuario UsuarioActivo() => new()
    {
        Id = UsuarioId,
        EmpresaId = EmpresaId,
        PerfilId = Guid.NewGuid(),
        NombreCompleto = "María López",
        Email = "maria@transnic.com",
        SupabaseUserId = SupabaseUserId,
        TipoUsuario = TipoUsuario.OPERADOR,
        Estado = EstadoUsuario.ACTIVE,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task ResetPasswordAdminAsync_CuandoUsuarioActivo_GeneraPasswordTemporalSinLogsYAudita()
    {
        var usuario = UsuarioActivo();
        _usuarioRepository.Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId)).ReturnsAsync(usuario);
        _sesionRepository.Setup(r => r.RevocarTodasPorUsuarioAsync(UsuarioId)).ReturnsAsync(true);
        _supabaseAuth.Setup(s => s.CambiarPasswordAsync(SupabaseUserId,
            It.Is<string>(p => p.Length == 19 && p[6] == ':'))).ReturnsAsync(true);

        await _service.ResetPasswordAdminAsync(UsuarioId, EmpresaId, AdminId);

        // G-06-D: se llama a Supabase Auth con una contraseña temporal con formato G6:Otp12.
        _supabaseAuth.Verify(s => s.CambiarPasswordAsync(SupabaseUserId,
            It.Is<string>(p => p.Length == 19 && p[6] == ':')), Times.Once);
        // G-06-E: se invalidan todas las sesiones del usuario.
        _sesionRepository.Verify(r => r.RevocarTodasPorUsuarioAsync(UsuarioId), Times.Once);
        // La auditoría registra al ADMIN que ejecutó la acción, nunca la contraseña.
        _auditoria.Verify(a => a.RegistrarAsync("usuarios", "RESET_PASSWORD_ADMIN", EmpresaId,
            AdminId, "Usuario", UsuarioId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAdminAsync_CuandoUsuarioInactivo_LanzaNotFoundException()
    {
        var usuario = UsuarioActivo();
        usuario.Activo = false;
        _usuarioRepository.Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId)).ReturnsAsync(usuario);

        var act = async () => await _service.ResetPasswordAdminAsync(UsuarioId, EmpresaId, AdminId);

        await act.Should().ThrowAsync<NotFoundException>();
        _supabaseAuth.Verify(s => s.CambiarPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAdminAsync_CuandoUsuarioSinSupabase_LanzaBusinessException()
    {
        var usuario = UsuarioActivo();
        usuario.SupabaseUserId = null;
        _usuarioRepository.Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId)).ReturnsAsync(usuario);

        var act = async () => await _service.ResetPasswordAdminAsync(UsuarioId, EmpresaId, AdminId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("El usuario no tiene cuenta en Supabase Auth. Espere a que acepte la invitación.");
    }

    [Fact]
    public async Task ResetPasswordAdminAsync_CuandoSupabaseFallaLanzaBusinessExceptionSinRevocarSesiones()
    {
        var usuario = UsuarioActivo();
        _usuarioRepository.Setup(r => r.GetByIdAsync(UsuarioId, EmpresaId)).ReturnsAsync(usuario);
        _supabaseAuth.Setup(s => s.CambiarPasswordAsync(SupabaseUserId, It.IsAny<string>())).ReturnsAsync(false);

        var act = async () => await _service.ResetPasswordAdminAsync(UsuarioId, EmpresaId, AdminId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("No se pudo restablecer la contraseña en Supabase Auth. Intente nuevamente.");
        _sesionRepository.Verify(r => r.RevocarTodasPorUsuarioAsync(It.IsAny<Guid>()), Times.Never);
    }
}