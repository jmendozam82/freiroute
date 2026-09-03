using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Perfil;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del servicio de perfiles/roles del tenant (HU-006).
/// Los perfiles del sistema (es_sistema) no se pueden desactivar; los perfiles
/// se gestionan siempre dentro del tenant (empresaId del JWT).
/// </summary>
public class PerfilServiceTests
{
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<IValidator<PerfilRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<ILogger<PerfilService>> _logger;
    private readonly PerfilService _service;

    public PerfilServiceTests()
    {
        _perfilRepository = new Mock<IPerfilRepository>();
        _validator = new Mock<IValidator<PerfilRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();
        _logger = new Mock<ILogger<PerfilService>>();

        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<PerfilRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new PerfilService(
            _perfilRepository.Object,
            _validator.Object,
            _auditoria.Object,
            _logger.Object);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoEsSistema_LanzaBusinessException()
    {
        // Los perfiles del sistema (es_sistema=true) no se desactivan.
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil
            {
                Id = perfilId,
                EmpresaId = empresaId,
                Nombre = "Administrador de Empresa",
                TipoPerfil = TipoPerfil.ADMIN,
                EsSistema = true,
                Activo = true
            });

        var act = async () => await _service.DeactivateAsync(empresaId, perfilId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*perfil del sistema*");

        // No llama al repositorio de desactivación.
        _perfilRepository.Verify(
            r => r.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CuandoValido_RetornaDtoConDatosDelPerfil()
    {
        // Debe retornar el DTO mapeado del perfil creado (HU-006 CA-02).
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        var dto = new PerfilRequestDto
        {
            Nombre = "Operador Avanzado",
            Descripcion = "Perfil personalizado",
            TipoPerfil = TipoPerfil.OPERADOR
        };

        _perfilRepository
            .Setup(r => r.CreateAsync(It.IsAny<Perfil>()))
            .ReturnsAsync(perfilId);

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil
            {
                Id = perfilId,
                EmpresaId = empresaId,
                Nombre = "Operador Avanzado",
                Descripcion = "Perfil personalizado",
                TipoPerfil = TipoPerfil.OPERADOR,
                EsSistema = false,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto, empresaId);

        result.Should().NotBeNull();
        result.Nombre.Should().Be("Operador Avanzado");
        result.TipoPerfil.Should().Be(TipoPerfil.OPERADOR);
        result.EsSistema.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_FiltradoPorEmpresaId()
    {
        // Los perfiles se obtienen siempre dentro del tenant (empresaId).
        var empresaId = Guid.NewGuid();
        var perfiles = new List<Perfil>
        {
            new() { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "Admin", TipoPerfil = TipoPerfil.ADMIN, Activo = true },
            new() { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "Operador", TipoPerfil = TipoPerfil.OPERADOR, Activo = true }
        };

        _perfilRepository
            .Setup(r => r.GetAllAsync(empresaId))
            .ReturnsAsync(perfiles);

        _perfilRepository
            .Setup(r => r.CountUsuariosAsync(It.IsAny<Guid>(), empresaId))
            .ReturnsAsync(3);

        var result = (await _service.GetAllAsync(empresaId)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.UsuariosAsignados == 3);

        // Filtrado por empresa — se pasa empresaId al repo.
        _perfilRepository.Verify(r => r.GetAllAsync(empresaId), Times.Once);
        _perfilRepository.Verify(
            r => r.CountUsuariosAsync(It.IsAny<Guid>(), empresaId), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        // Actualizar un perfil inexistente → 404.
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync((Perfil?)null);

        var act = async () => await _service.UpdateAsync(
            perfilId,
            new PerfilRequestDto { Nombre = "Foo" },
            empresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaYRegistraAuditoria()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "Nuevo", TipoPerfil = TipoPerfil.OPERADOR, Activo = true });

        _perfilRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Perfil>()))
            .ReturnsAsync(true);

        _perfilRepository
            .Setup(r => r.CountUsuariosAsync(perfilId, empresaId))
            .ReturnsAsync(2);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(
            perfilId,
            new PerfilRequestDto { Nombre = "Nuevo", TipoPerfil = TipoPerfil.OPERADOR },
            empresaId);

        result.Should().NotBeNull();
        result.Nombre.Should().Be("Nuevo");
        result.UsuariosAsignados.Should().Be(2);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "perfiles", AccionAuditoria.UPDATE, empresaId, null,
                nameof(Perfil), perfilId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoUpdateRepoFalla_LanzaNotFoundException()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "Viejo", Activo = true });

        _perfilRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Perfil>()))
            .ReturnsAsync(false);

        var act = async () => await _service.UpdateAsync(
            perfilId,
            new PerfilRequestDto { Nombre = "Nuevo" },
            empresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoDatosInvalidos_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<PerfilRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Nombre", "Obligatorio") }));

        var act = async () => await _service.UpdateAsync(
            Guid.NewGuid(),
            new PerfilRequestDto { Nombre = "" },
            Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_CuandoTipoPerfilVacio_UsaCustom()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        Perfil? capturado = null;

        _perfilRepository
            .Setup(r => r.CreateAsync(It.IsAny<Perfil>()))
            .Callback<Perfil>(p => capturado = p)
            .ReturnsAsync(perfilId);

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "X", TipoPerfil = TipoPerfil.CUSTOM, Activo = true });

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(
            new PerfilRequestDto { Nombre = "Personalizado", TipoPerfil = "" },
            empresaId);

        capturado.Should().NotBeNull();
        capturado!.TipoPerfil.Should().Be(TipoPerfil.CUSTOM);
        capturado.EmpresaId.Should().Be(empresaId);
        result.TipoPerfil.Should().Be(TipoPerfil.CUSTOM);
    }

    [Fact]
    public async Task CreateAsync_CuandoDatosInvalidos_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<PerfilRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Nombre", "Obligatorio") }));

        var act = async () => await _service.CreateAsync(
            new PerfilRequestDto { Nombre = "" },
            Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaDtoConConteoUsuarios()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "Admin", TipoPerfil = TipoPerfil.ADMIN, Activo = true });

        _perfilRepository
            .Setup(r => r.CountUsuariosAsync(perfilId, empresaId))
            .ReturnsAsync(5);

        var result = await _service.GetByIdAsync(perfilId, empresaId);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Admin");
        result.UsuariosAsignados.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _perfilRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Perfil?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoValido_DesactivaYRegistraAuditoria()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "Operador", EsSistema = false, Activo = true });

        _perfilRepository
            .Setup(r => r.DeactivateAsync(perfilId, empresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeactivateAsync(empresaId, perfilId);

        result.Should().BeTrue();
        _auditoria.Verify(
            a => a.RegistrarAsync(
                "perfiles", AccionAuditoria.DEACTIVATE, empresaId, null,
                nameof(Perfil), perfilId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _perfilRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Perfil?)null);

        var act = async () => await _service.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoRepoDevuelveFalse_LanzaNotFoundException()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "X", EsSistema = false, Activo = true });

        _perfilRepository
            .Setup(r => r.DeactivateAsync(perfilId, empresaId))
            .ReturnsAsync(false);

        var act = async () => await _service.DeactivateAsync(empresaId, perfilId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
