using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Permiso;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del servicio de permisos por perfil (HU-006 / ADR-009).
/// El reemplazo es una operación transaccional; el SUPER_ADMIN está blindado.
/// </summary>
public class PermisoServiceTests
{
    private readonly Mock<IPermisoRepository> _permisoRepository;
    private readonly Mock<IPerfilRepository> _perfilRepository;
    private readonly Mock<IValidator<PermisoRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<ILogger<PermisoService>> _logger;
    private readonly PermisoService _service;

    public PermisoServiceTests()
    {
        _permisoRepository = new Mock<IPermisoRepository>();
        _perfilRepository = new Mock<IPerfilRepository>();
        _validator = new Mock<IValidator<PermisoRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();
        _logger = new Mock<ILogger<PermisoService>>();

        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<PermisoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new PermisoService(
            _permisoRepository.Object,
            _perfilRepository.Object,
            _validator.Object,
            _auditoria.Object,
            _logger.Object);
    }

    private PermisoRequestDto DtoValido() => new()
    {
        PerfilId = Guid.NewGuid(),
        Modulos = new List<ModuloPermisoRequestDto>
        {
            new()
            {
                Modulo = ModuloPermiso.Embarques,
                PuedeLeer = true,
                PuedeCrear = true,
                PuedeActualizar = true
            }
        }
    };

    private void ConfigurarPerfilValido(Guid perfilId, Guid empresaId) =>
        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil
            {
                Id = perfilId,
                EmpresaId = empresaId,
                Nombre = "Perfil de prueba",
                TipoPerfil = TipoPerfil.CUSTOM,
                Activo = true
            });

    [Fact]
    public async Task ActualizarPermisosAsync_CuandoPerfilValido_ReemplazaPermisosAtomicamente()
    {
        // El reemplazo delega al repositorio (transacción DELETE + reinsert).
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        var dto = DtoValido();
        dto.PerfilId = perfilId;

        ConfigurarPerfilValido(perfilId, empresaId);

        _permisoRepository
            .Setup(r => r.ReemplazarPermisosAsync(
                perfilId, It.IsAny<IEnumerable<Permiso>>(), empresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ReemplazarPermisosAsync(perfilId, dto, empresaId);

        result.Should().BeTrue();
        _permisoRepository.Verify(
            r => r.ReemplazarPermisosAsync(
                perfilId,
                It.Is<IEnumerable<Permiso>>(pl => pl.All(p => p.EmpresaId == empresaId && p.PuedeLeer)),
                empresaId),
            Times.Once);
    }

    [Fact]
    public async Task ActualizarPermisosAsync_CuandoSuperAdmin_LanzaBusinessException()
    {
        // Blindaje: los permisos del Super Admin son inmutables.
        var empresaId = Guid.NewGuid();
        var perfilSuperAdmin = IdsSistema.PerfilSuperAdminId;
        var dto = DtoValido();
        dto.PerfilId = perfilSuperAdmin;

        ConfigurarPerfilValido(perfilSuperAdmin, empresaId);

        var act = async () =>
            await _service.ReemplazarPermisosAsync(perfilSuperAdmin, dto, empresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.WithMessage("*Super Admin*");

        // No delega el reemplazo al repo.
        _permisoRepository.Verify(
            r => r.ReemplazarPermisosAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Permiso>>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ActualizarPermisosAsync_CuandoPerfilOtraEmpresa_LanzaNotFoundException()
    {
        // El perfil no pertenece al tenant → no se puede leer (aislamiento).
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        var dto = DtoValido();
        dto.PerfilId = perfilId;

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync((Perfil?)null);

        var act = async () =>
            await _service.ReemplazarPermisosAsync(perfilId, dto, empresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActualizarPermisosAsync_RegistraAuditoriaConModulos()
    {
        // HU-006 CA-07: la operación queda auditada con los módulos modificados.
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        var dto = DtoValido();
        dto.PerfilId = perfilId;

        ConfigurarPerfilValido(perfilId, empresaId);

        _permisoRepository
            .Setup(r => r.ReemplazarPermisosAsync(
                perfilId, It.IsAny<IEnumerable<Permiso>>(), empresaId))
            .ReturnsAsync(true);

        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _service.ReemplazarPermisosAsync(perfilId, dto, empresaId);

        _auditoria.Verify(
            a => a.RegistrarAsync(
                "permisos", AccionAuditoria.UPDATE, empresaId, null,
                nameof(Permiso), perfilId,
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByPerfilAsync_CuandoPerfilActivo_RetornaPermisosMapeados()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        ConfigurarPerfilValido(perfilId, empresaId);

        _permisoRepository
            .Setup(r => r.GetByPerfilAsync(perfilId, empresaId))
            .ReturnsAsync(new List<Permiso>
            {
                new() { Id = Guid.NewGuid(), PerfilId = perfilId, Modulo = ModuloPermiso.Embarques, PuedeLeer = true, PuedeCrear = true, Activo = true }
            });

        var result = (await _service.GetByPerfilAsync(perfilId, empresaId)).ToList();

        result.Should().HaveCount(1);
        result[0].Modulo.Should().Be(ModuloPermiso.Embarques);
        result[0].PuedeLeer.Should().BeTrue();
    }

    [Fact]
    public async Task GetByPerfilAsync_CuandoPerfilInactivo_LanzaNotFoundException()
    {
        var empresaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        _perfilRepository
            .Setup(r => r.GetByIdAsync(perfilId, empresaId))
            .ReturnsAsync(new Perfil { Id = perfilId, EmpresaId = empresaId, Nombre = "Inactivo", Activo = false });

        var act = async () => await _service.GetByPerfilAsync(perfilId, empresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReemplazarPermisosAsync_CuandoDatosInvalidos_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<PermisoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Modulos", "Debe incluir al menos un módulo") }));

        var act = async () => await _service.ReemplazarPermisosAsync(
            Guid.NewGuid(), DtoValido(), Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }
}
