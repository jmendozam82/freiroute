using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del servicio de empresas/tenants (HU-001) — tabla raíz gestionada por
/// el SUPER_ADMIN. Cubre los criterios CA-01 a CA-07.
/// </summary>
public class EmpresaServiceTests
{
    private readonly Mock<IEmpresaRepository> _empresaRepository;
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<IPermisoRepository> _permisoRepository;
    private readonly Mock<IValidator<EmpresaRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<EmpresaService>> _logger;
    private readonly EmpresaService _service;

    public EmpresaServiceTests()
    {
        _empresaRepository = new Mock<IEmpresaRepository>();
        _perfilRepository = new Mock<IPerfilRepository>();
        _permisoRepository = new Mock<IPermisoRepository>();
        _validator = new Mock<IValidator<EmpresaRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<EmpresaService>>();

        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<EmpresaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new EmpresaService(
            _empresaRepository.Object,
            _perfilRepository.Object,
            _permisoRepository.Object,
            _validator.Object,
            _auditoria.Object,
            _emailService.Object,
            _logger.Object);
    }

    private EmpresaRequestDto DtoValido() => new()
    {
        Nombre = "Trans Nicaragua S.A.",
        EmailAdmin = "admin@transnic.com",
        Pais = "Nicaragua",
        PlanSuscripcion = "PROFESSIONAL",
        ColorPrimario = "#1A73E8",
        ColorSecundario = "#0B2545"
    };

    private void ConfigurarCreacionExitosa()
    {
        _empresaRepository
            .Setup(r => r.GetByEmailAdminAsync(It.IsAny<string>()))
            .ReturnsAsync((Empresa?)null);

        _empresaRepository
            .Setup(r => r.CreateAsync(It.IsAny<Empresa>()))
            .ReturnsAsync(Guid.NewGuid());

        // Perfiles base plantilla en la empresa raíz (los 5 tipos).
        _perfilRepository
            .Setup(r => r.GetByTipoAsync(
                It.IsAny<string>(), IdsSistema.EmpresaRaizId))
            .Returns((string tipo, Guid _) =>
                Task.FromResult<Perfil?>(new Perfil
                {
                    Id = Guid.NewGuid(),
                    EmpresaId = IdsSistema.EmpresaRaizId,
                    Nombre = $"Plantilla {tipo}",
                    TipoPerfil = tipo,
                    EsSistema = true,
                    Activo = true
                }));

        _perfilRepository
            .Setup(r => r.CreateAsync(It.IsAny<Perfil>()))
            .ReturnsAsync(Guid.NewGuid());

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(It.IsAny<Guid>(), IdsSistema.EmpresaRaizId))
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

        _permisoRepository
            .Setup(r => r.CreateAsync(It.IsAny<Permiso>()))
            .ReturnsAsync(Guid.NewGuid());

        _emailService
            .Setup(e => e.EnviarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _empresaRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Empresa
            {
                Id = id,
                Nombre = "Trans Nicaragua S.A.",
                EmailAdmin = "admin@transnic.com",
                PlanSuscripcion = "PROFESSIONAL",
                Estado = EstadoEmpresa.ACTIVE,
                PrefijoEmbarque = "TR",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
    }

    [Fact]
    public async Task CreateAsync_CuandoDatosValidos_CreaEmpresaYPerfilesBase()
    {
        // CA-01, CA-02 — crea la empresa y los perfiles base del tenant.
        ConfigurarCreacionExitosa();

        var result = await _service.CreateAsync(DtoValido());

        result.Should().NotBeNull();

        // Empresa creada una vez.
        _empresaRepository.Verify(
            r => r.CreateAsync(It.IsAny<Empresa>()), Times.Once);

        // 5 perfiles base (ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE) + permisos.
        _perfilRepository.Verify(
            r => r.CreateAsync(It.Is<Perfil>(p => p.EsSistema && p.Activo)),
            Times.Exactly(IdsSistema.PerfilesBaseTenant.Length));
    }

    [Fact]
    public async Task CreateAsync_CuandoEmailDuplicado_LanzaConflictException()
    {
        // CA-06 — email_admin duplicado → 409.
        _empresaRepository
            .Setup(r => r.GetByEmailAdminAsync(It.IsAny<string>()))
            .ReturnsAsync(new Empresa { Id = Guid.NewGuid(), EmailAdmin = "admin@transnic.com" });

        var act = async () => await _service.CreateAsync(DtoValido());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_CuandoCreada_EnviaEmailBienvenida()
    {
        // CA-03 — email de bienvenida al email_admin.
        ConfigurarCreacionExitosa();

        await _service.CreateAsync(DtoValido());

        _emailService.Verify(
            e => e.EnviarAsync("admin@transnic.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoCreada_RegistraAuditoria()
    {
        // CA-05 — la operación queda en auditoría_actividad.
        ConfigurarCreacionExitosa();

        await _service.CreateAsync(DtoValido());

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "empresas", AccionAuditoria.CREATE, IdsSistema.EmpresaRaizId, null,
                nameof(Empresa), It.IsAny<Guid>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoDatosInvalidos_LanzaValidationException()
    {
        // Validación servidor → ValidationException.
        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<EmpresaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Nombre", "Obligatorio") }));

        var act = async () => await _service.CreateAsync(DtoValido());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetAllAsync_SoloPuedeAccederSuperAdmin()
    {
        // HU-001 CA-07: la tabla raíz se gestiona globalmente — el servicio
        // NO recibe empresaId. La restricción a SUPER_ADMIN se aplica en el
        // controller con [RequirePermission] + bypass de rol (test de integración).
        var empresas = new List<Empresa>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Empresa A",
                PlanSuscripcion = "STARTER",
                Activo = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Empresa B",
                PlanSuscripcion = "ENTERPRISE",
                Activo = true
            }
        };

        _empresaRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(empresas);

        var result = (await _service.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => !string.IsNullOrEmpty(e.Nombre));

        // No recibe empresa_id — se llama a GetAllAsync sin filtro de tenant.
        _empresaRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_DesactivaYRegistraAuditoria()
    {
        // Soft delete + auditoría DEACTIVATE.
        var empresaId = Guid.NewGuid();
        _empresaRepository
            .Setup(r => r.GetByIdAsync(empresaId))
            .ReturnsAsync(new Empresa { Id = empresaId, Nombre = "Trans Nicaragua S.A.", Activo = true });

        _empresaRepository
            .Setup(r => r.DeactivateAsync(empresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeactivateAsync(empresaId);

        result.Should().BeTrue();
        _empresaRepository.Verify(r => r.DeactivateAsync(empresaId), Times.Once);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                "empresas", AccionAuditoria.DEACTIVATE, IdsSistema.EmpresaRaizId, null,
                nameof(Empresa), empresaId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaYRegistraAuditoria()
    {
        var empresaId = Guid.NewGuid();

        _empresaRepository
            .Setup(r => r.GetByIdAsync(empresaId))
            .ReturnsAsync(new Empresa { Id = empresaId, Nombre = "Viejo", Activo = true });

        _empresaRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Empresa>()))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(empresaId, DtoValido());

        result.Should().NotBeNull();
        _auditoria.Verify(
            a => a.RegistrarAsync(
                "empresas", AccionAuditoria.UPDATE, IdsSistema.EmpresaRaizId, null,
                nameof(Empresa), empresaId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _empresaRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Empresa?)null);

        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), DtoValido());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _empresaRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Empresa?)null);

        var act = async () => await _service.DeactivateAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _empresaRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Empresa?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CuandoFallaPerfilesBase_LanzaBusinessException()
    {
        // Si la creación de la empresa ya ocurrió pero falla la config de perfiles
        // base → BusinessException (No propagar la excepción interna).
        var empresaId = Guid.NewGuid();

        _empresaRepository
            .Setup(r => r.GetByEmailAdminAsync(It.IsAny<string>()))
            .ReturnsAsync((Empresa?)null);

        _empresaRepository
            .Setup(r => r.CreateAsync(It.IsAny<Empresa>()))
            .ReturnsAsync(empresaId);

        // La plantilla de perfil lanza → se dispara el catch que envuelve en BusinessException.
        _perfilRepository
            .Setup(r => r.GetByTipoAsync(It.IsAny<string>(), IdsSistema.EmpresaRaizId))
            .ThrowsAsync(new InvalidOperationException("BD caída"));

        var act = async () => await _service.CreateAsync(DtoValido());

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*perfiles base*");
    }

    [Fact]
    public async Task CreateAsync_CuandoOpcionalesVacios_AsignaValoresPorDefecto()
    {
        Empresa? capturada = null;

        ConfigurarCreacionExitosa();

        // Capturar la entidad persistida para inspeccionar los defaults.
        _empresaRepository
            .Setup(r => r.CreateAsync(It.IsAny<Empresa>()))
            .Callback<Empresa>(e => capturada = e)
            .ReturnsAsync(Guid.NewGuid());

        await _service.CreateAsync(new EmpresaRequestDto
        {
            Nombre = "A",               // 1 letra → prefijo fallback "FR"
            EmailAdmin = "admin@transnic.com",
            PlanSuscripcion = "STARTER"
        });

        capturada.Should().NotBeNull();
        capturada!.ColorPrimario.Should().Be("#1A73E8");
        capturada.ColorSecundario.Should().Be("#0B2545");
        capturada.MonedaPrincipal.Should().Be("USD");
        capturada.ZonaHoraria.Should().Be("America/Managua");
        capturada.Idioma.Should().Be("es");
        capturada.FormatoFecha.Should().Be("DD/MM/YYYY");
        capturada.Estado.Should().Be(EstadoEmpresa.ACTIVE);
        // Prefijo fallback "FR" cuando el nombre tiene <2 caracteres alfanuméricos.
        capturada.PrefijoEmbarque.Should().Be("FR");
    }

    [Fact]
    public async Task GetAllAsync_RetornaListaMapeada()
    {
        var empresas = new List<Empresa>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Empresa A", Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Empresa B", Activo = true }
        };

        _empresaRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(empresas);

        var result = (await _service.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => !string.IsNullOrEmpty(e.Nombre));
    }
}
