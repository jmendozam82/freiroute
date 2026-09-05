using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Settings;
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
/// Tests del servicio de usuarios por tenant (HU-003, HU-004).
/// Invitación por email con token de 48 h, aceptación de token de un solo uso
/// y soft delete. Todo método recibe empresaId del JWT.
/// </summary>
public class UsuarioServiceTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid PerfilId = Guid.NewGuid();

    private readonly Mock<IUsuarioRepository> _usuarioRepository;
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<IInvitacionRepository> _invitacionRepository;
    private readonly Mock<IValidator<UsuarioRequestDto>> _validator;
    private readonly Mock<ISupabaseAuthService> _supabaseAuth;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IPlanLimiteService> _planLimiteService;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly Mock<ILogger<UsuarioService>> _logger;
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _usuarioRepository = new Mock<IUsuarioRepository>();
        _perfilRepository = new Mock<IPerfilRepository>();
        _invitacionRepository = new Mock<IInvitacionRepository>();
        _validator = new Mock<IValidator<UsuarioRequestDto>>();
        _supabaseAuth = new Mock<ISupabaseAuthService>();
        _auditoria = new Mock<IAuditoriaService>();
        _emailService = new Mock<IEmailService>();
        _planLimiteService = new Mock<IPlanLimiteService>();
        _appSettings = Options.Create(new AppSettings { BaseUrl = "https://localhost:5001" });
        _logger = new Mock<ILogger<UsuarioService>>();

        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<UsuarioRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new UsuarioService(
            _usuarioRepository.Object,
            _perfilRepository.Object,
            _invitacionRepository.Object,
            _validator.Object,
            _supabaseAuth.Object,
            _auditoria.Object,
            _emailService.Object,
            _planLimiteService.Object,
            _appSettings,
            _logger.Object);
    }

    private void ConfigurarPerfilValido() =>
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

    private void ConfigurarAuditoriaYEmail()
    {
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _emailService
            .Setup(e => e.EnviarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task InvitarAsync_CuandoEmailNuevo_CreaInvitacionYEnviaEmail()
    {
        // Crea la cuenta PENDING + invitación con token + email (HU-003 CA-03).
        ConfigurarPerfilValido();
        ConfigurarAuditoriaYEmail();

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        _usuarioRepository
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(Guid.NewGuid());

        _invitacionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Invitacion>()))
            .ReturnsAsync(Guid.NewGuid());

        var creadoPorId = Guid.NewGuid();

        await _service.InvitarAsync(
            new InvitacionRequestDto { Email = "juan.perez@empresa.com", PerfilId = PerfilId },
            EmpresaId,
            creadoPorId);

        // Se crea la invitación y se envía el email.
        _invitacionRepository.Verify(
            r => r.CreateAsync(It.Is<Invitacion>(i =>
                i.EmpresaId == EmpresaId &&
                i.PerfilId == PerfilId &&
                i.Estado == "PENDING" &&
                !string.IsNullOrEmpty(i.Token) &&
                i.FechaExpiracion > DateTime.UtcNow.AddHours(47))),
            Times.Once);

        _emailService.Verify(
            e => e.EnviarAsync("juan.perez@empresa.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task InvitarAsync_CuandoEmailExiste_LanzaConflictException()
    {
        // Email duplicado dentro de la empresa → 409.
        ConfigurarPerfilValido();

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync(new Usuario { Id = Guid.NewGuid(), Email = "juan@empresa.com", EmpresaId = EmpresaId });

        var act = async () => await _service.InvitarAsync(
            new InvitacionRequestDto { Email = "juan@empresa.com", PerfilId = PerfilId },
            EmpresaId,
            Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AceptarInvitacionAsync_CuandoTokenValido_CreaUsuario()
    {
        // Token válido de un solo uso → crea identidad, activa y audita.
        var token = "token-valid";
        var supabaseUserId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(new Invitacion
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                Email = "juan@empresa.com",
                PerfilId = PerfilId,
                Token = token,
                Estado = "PENDING",
                FechaExpiracion = DateTime.UtcNow.AddHours(48),
                FechaCreacion = DateTime.UtcNow
            });

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync("juan@empresa.com", EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                Email = "juan@empresa.com",
                NombreCompleto = "Juan",
                Estado = EstadoUsuario.PENDING,
                Activo = true
            });

        _supabaseAuth
            .Setup(s => s.SignUpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(supabaseUserId);

        _usuarioRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(true);

        _invitacionRepository
            .Setup(r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _perfilRepository
            .Setup(r => r.GetByIdAsync(PerfilId, EmpresaId))
            .ReturnsAsync(new Perfil { Id = PerfilId, EmpresaId = EmpresaId, Nombre = "Operador", Activo = true });

        ConfigurarAuditoriaYEmail();

        var result = await _service.AceptarInvitacionAsync(token, "NuevaPassword123!");

        result.Should().NotBeNull();
        result.Email.Should().Be("juan@empresa.com");
        result.Estado.Should().Be(EstadoUsuario.ACTIVE);

        // El usuario se activa y la invitación se marca como aceptada.
        _usuarioRepository.Verify(
            r => r.UpdateAsync(It.Is<Usuario>(u => u.Estado == EstadoUsuario.ACTIVE)),
            Times.Once);

        _invitacionRepository.Verify(
            r => r.MarcarAceptadaAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task AceptarInvitacionAsync_CuandoTokenExpirado_LanzaBusinessException()
    {
        // Token expirado → BusinessException.
        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new Invitacion
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                Email = "juan@empresa.com",
                PerfilId = PerfilId,
                Token = "expirado",
                Estado = "PENDING",
                FechaExpiracion = DateTime.UtcNow.AddHours(-1), // vencido
                FechaCreacion = DateTime.UtcNow.AddDays(-2)
            });

        var act = async () => await _service.AceptarInvitacionAsync("expirado", "NuevaPassword123!");

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task AceptarInvitacionAsync_CuandoTokenYaUsado_LanzaBusinessException()
    {
        // Token ya usado (estado != PENDING) → BusinessException.
        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new Invitacion
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                Email = "juan@empresa.com",
                PerfilId = PerfilId,
                Token = "usado",
                Estado = "ACCEPTED", // ya usado
                FechaExpiracion = DateTime.UtcNow.AddHours(48),
                FechaAceptacion = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow
            });

        var act = async () => await _service.AceptarInvitacionAsync("usado", "NuevaPassword123!");

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_DesactivaYRegistraAuditoria()
    {
        // Soft delete + auditoría DEACTIVATE (nunca DELETE físico).
        var usuarioId = Guid.NewGuid();

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                Email = "juan@empresa.com",
                NombreCompleto = "Juan",
                Activo = true
            });

        _usuarioRepository
            .Setup(r => r.DeactivateAsync(usuarioId, EmpresaId))
            .ReturnsAsync(true);

        ConfigurarAuditoriaYEmail();

        var result = await _service.DeactivateAsync(usuarioId, EmpresaId);

        result.Should().BeTrue();
        _usuarioRepository.Verify(r => r.DeactivateAsync(usuarioId, EmpresaId), Times.Once);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                "usuarios", AccionAuditoria.DEACTIVATE, EmpresaId, null,
                nameof(Usuario), usuarioId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    // ── Consultas (HU-003) ─────────────────────────────────────────

    private void ConfigurarPerfilPorId(Guid perfilId) =>
        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, EmpresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = EmpresaId, Nombre = "Operador", Activo = true });

    private Usuario UsuarioBase(Guid id) => new()
    {
        Id = id,
        EmpresaId = EmpresaId,
        PerfilId = PerfilId,
        NombreCompleto = "Juan Pérez",
        Email = "juan@transnic.com",
        TipoUsuario = TipoUsuario.OPERADOR,
        Estado = EstadoUsuario.ACTIVE,
        Activo = true
    };

    [Fact]
    public async Task GetAllAsync_FiltraPorEmpresa()
    {
        var usuarios = new List<Usuario>
        {
            UsuarioBase(Guid.NewGuid()),
            UsuarioBase(Guid.NewGuid())
        };

        ConfigurarPerfilPorId(PerfilId);

        _usuarioRepository
            .Setup(r => r.GetAllAsync(EmpresaId))
            .ReturnsAsync(usuarios);

        var result = (await _service.GetAllAsync(EmpresaId)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.PerfilNombre == "Operador");

        _usuarioRepository.Verify(r => r.GetAllAsync(EmpresaId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaDto()
    {
        var id = Guid.NewGuid();
        ConfigurarPerfilPorId(PerfilId);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(id, EmpresaId))
            .ReturnsAsync(UsuarioBase(id));

        var result = await _service.GetByIdAsync(id, EmpresaId);

        result.Should().NotBeNull();
        result!.Email.Should().Be("juan@transnic.com");
        result.PerfilNombre.Should().Be("Operador");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid(), EmpresaId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_CuandoExiste_RetornaDto()
    {
        var id = Guid.NewGuid();
        ConfigurarPerfilPorId(PerfilId);

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync("juan@transnic.com", EmpresaId))
            .ReturnsAsync(UsuarioBase(id));

        var result = await _service.GetByEmailAsync("juan@transnic.com", EmpresaId);

        result.Should().NotBeNull();
        result!.Email.Should().Be("juan@transnic.com");
    }

    // ── Create (HU-003) ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CuandoValido_CreaEnPendingYRegistraAuditoria()
    {
        ConfigurarPerfilValido();

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var nuevoId = Guid.NewGuid();
        _usuarioRepository
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(nuevoId);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(new UsuarioRequestDto
        {
            PerfilId = PerfilId,
            NombreCompleto = "Juan",
            Email = "juan@transnic.com",
            TipoUsuario = TipoUsuario.OPERADOR
        }, EmpresaId);

        result.Should().NotBeNull();
        result.Estado.Should().Be(EstadoUsuario.PENDING);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "usuarios", AccionAuditoria.CREATE, EmpresaId, null,
                nameof(Usuario), nuevoId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoSuperAdmin_LanzaBusinessException()
    {
        ConfigurarPerfilValido();

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.CreateAsync(new UsuarioRequestDto
        {
            PerfilId = PerfilId,
            NombreCompleto = "Juan",
            Email = "juan@transnic.com",
            TipoUsuario = TipoUsuario.SUPER_ADMIN
        }, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*Super Admin*");
    }

    [Fact]
    public async Task CreateAsync_CuandoExcedeLimitePlan_LanzaBusinessExceptionAntesDeCrear()
    {
        // Fix re-smoke test #3: verificar el límite de usuarios del plan ANTES de
        // crear (HU-013 CA-08). Antes solo se verificaba en ReactivarAsync.
        ConfigurarPerfilValido();

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        _planLimiteService
            .Setup(r => r.VerificarLimiteUsuariosAsync(EmpresaId))
            .ThrowsAsync(new BusinessException("Límite de usuarios del plan alcanzado"));

        var act = async () => await _service.CreateAsync(new UsuarioRequestDto
        {
            PerfilId = PerfilId,
            NombreCompleto = "Juan",
            Email = "juan@transnic.com",
            TipoUsuario = TipoUsuario.OPERADOR
        }, EmpresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*Límite*");

        _usuarioRepository.Verify(
            r => r.CreateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task InvitarAsync_CuandoExcedeLimitePlan_LanzaBusinessExceptionAntesDeEnviar()
    {
        // Fix re-smoke test #3: verificar el límite de usuarios del plan al inicio
        // de la invitación (HU-013 CA-08), no solo al reactivar.
        _planLimiteService
            .Setup(r => r.VerificarLimiteUsuariosAsync(EmpresaId))
            .ThrowsAsync(new BusinessException("Límite de usuarios del plan alcanzado"));

        var act = async () => await _service.InvitarAsync(
            new InvitacionRequestDto { Email = "nuevo@transnic.com", PerfilId = PerfilId },
            EmpresaId, Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*Límite*");

        _emailService.Verify(
            e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _invitacionRepository.Verify(
            r => r.CreateAsync(It.IsAny<Invitacion>()), Times.Never);
    }

    // ── Update (HU-003) ────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaYRegistraAuditoria()
    {
        var id = Guid.NewGuid();
        ConfigurarPerfilPorId(PerfilId);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(id, EmpresaId))
            .ReturnsAsync(UsuarioBase(id));

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        _usuarioRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(id, new UsuarioRequestDto
        {
            PerfilId = PerfilId,
            NombreCompleto = "Juan Actualizado",
            Email = "juan@transnic.com",
            TipoUsuario = TipoUsuario.OPERADOR
        }, EmpresaId);

        result.Should().NotBeNull();
        result.NombreCompleto.Should().Be("Juan Actualizado");

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "usuarios", AccionAuditoria.UPDATE, EmpresaId, null,
                nameof(Usuario), id,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), new UsuarioRequestDto(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoEmailDuplicado_LanzaConflictException()
    {
        var id = Guid.NewGuid();
        ConfigurarPerfilPorId(PerfilId);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(id, EmpresaId))
            .ReturnsAsync(UsuarioBase(id));

        // Existe otro usuario con ese email (id diferente) → 409.
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync("juan@transnic.com", EmpresaId))
            .ReturnsAsync(UsuarioBase(Guid.NewGuid()));

        var act = async () => await _service.UpdateAsync(id, new UsuarioRequestDto
        {
            PerfilId = PerfilId,
            NombreCompleto = "Juan",
            Email = "juan@transnic.com",
            TipoUsuario = TipoUsuario.OPERADOR
        }, EmpresaId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _usuarioRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.DeactivateAsync(Guid.NewGuid(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Invitar / Aceptar: casos extra ─────────────────────────────

    [Fact]
    public async Task InvitarAsync_CuandoEmailInvalido_LanzaBusinessException()
    {
        // Email sin formato válido → BusinessException antes de tocar el repo.
        var act = async () => await _service.InvitarAsync(
            new InvitacionRequestDto { Email = "no-es-email", PerfilId = PerfilId },
            EmpresaId,
            Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*email*");
    }

    [Fact]
    public async Task AceptarInvitacionAsync_CuandoUsuarioNoExiste_LanzaBusinessException()
    {
        var token = "token-valido";

        _invitacionRepository
            .Setup(r => r.GetByTokenAsync(token))
            .ReturnsAsync(new Invitacion
            {
                Id = Guid.NewGuid(),
                EmpresaId = EmpresaId,
                Email = "juan@empresa.com",
                PerfilId = PerfilId,
                Token = token,
                Estado = "PENDING",
                FechaExpiracion = DateTime.UtcNow.AddHours(48)
            });

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync("juan@empresa.com", EmpresaId))
            .ReturnsAsync((Usuario?)null);

        var act = async () => await _service.AceptarInvitacionAsync(token, "NuevaPassword123!");

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── ReactivarAsync (HU-013 CA-07) ─────────────────────────────

    [Fact]
    public async Task ReactivarAsync_CuandoUsuarioNoExiste_LanzaNotFoundException()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _usuarioRepository
            .Setup(r => r.GetByIdIncluyendoInactivosAsync(usuarioId, EmpresaId))
            .ReturnsAsync((Usuario?)null);

        // Act
        var act = async () => await _service.ReactivarAsync(usuarioId, EmpresaId, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReactivarAsync_CuandoUsuarioYaActivo_LanzaBusinessException()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _usuarioRepository
            .Setup(r => r.GetByIdIncluyendoInactivosAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                NombreCompleto = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = TipoUsuario.OPERADOR,
                Estado = EstadoUsuario.ACTIVE,
                Activo = true
            });

        // Act
        var act = async () => await _service.ReactivarAsync(usuarioId, EmpresaId, Guid.NewGuid());

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*ya está activo*");
    }

    [Fact]
    public async Task ReactivarAsync_Exitoso_ReactivaYRegistraAuditoria()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var reactivadoPorId = Guid.NewGuid();

        _usuarioRepository
            .Setup(r => r.GetByIdIncluyendoInactivosAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                NombreCompleto = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = TipoUsuario.OPERADOR,
                Estado = EstadoUsuario.SUSPENDED,
                Activo = false
            });

        _planLimiteService
            .Setup(s => s.VerificarLimiteUsuariosAsync(EmpresaId))
            .Returns(Task.CompletedTask);

        _usuarioRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(true);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                NombreCompleto = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = TipoUsuario.OPERADOR,
                Estado = EstadoUsuario.ACTIVE,
                Activo = true
            });

        ConfigurarPerfilPorId(PerfilId);
        ConfigurarAuditoriaYEmail();

        // Act
        var result = await _service.ReactivarAsync(usuarioId, EmpresaId, reactivadoPorId);

        // Assert
        result.Should().NotBeNull();
        result.Activo.Should().BeTrue();
        result.Estado.Should().Be(EstadoUsuario.ACTIVE);

        _planLimiteService.Verify(
            s => s.VerificarLimiteUsuariosAsync(EmpresaId), Times.Once);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "usuarios", AccionAuditoria.REACTIVAR, EmpresaId, reactivadoPorId,
                nameof(Usuario), usuarioId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task ReactivarAsync_CuandoUpdateFalla_UsaReactivarDelRepositorio()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var reactivadoPorId = Guid.NewGuid();

        _usuarioRepository
            .Setup(r => r.GetByIdIncluyendoInactivosAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                NombreCompleto = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = TipoUsuario.OPERADOR,
                Estado = EstadoUsuario.SUSPENDED,
                Activo = false
            });

        _planLimiteService
            .Setup(s => s.VerificarLimiteUsuariosAsync(EmpresaId))
            .Returns(Task.CompletedTask);

        // UpdateAsync retorna false (usuario inactivo, UPDATE no afecta filas).
        _usuarioRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(false);

        _usuarioRepository
            .Setup(r => r.ReactivarAsync(usuarioId, EmpresaId))
            .ReturnsAsync(true);

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, EmpresaId))
            .ReturnsAsync(new Usuario
            {
                Id = usuarioId,
                EmpresaId = EmpresaId,
                PerfilId = PerfilId,
                NombreCompleto = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = TipoUsuario.OPERADOR,
                Estado = EstadoUsuario.ACTIVE,
                Activo = true
            });

        ConfigurarPerfilPorId(PerfilId);
        ConfigurarAuditoriaYEmail();

        // Act
        var result = await _service.ReactivarAsync(usuarioId, EmpresaId, reactivadoPorId);

        // Assert
        result.Should().NotBeNull();
        result.Activo.Should().BeTrue();

        _usuarioRepository.Verify(
            r => r.ReactivarAsync(usuarioId, EmpresaId), Times.Once);
    }
}
