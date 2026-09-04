using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Plan;
using Freiroute.Entity;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del catálogo global de planes de suscripción (HU-010).
/// Cubre la unicidad del código, NotFound en update/deactivate y el
/// guard de CA-04 (no desactivar un plan con empresas suscritas activas).
/// </summary>
public class PlanServiceTests
{
    private readonly Mock<IPlanRepository> _repo;
    private readonly Mock<IValidator<PlanRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly PlanService _service;

    public PlanServiceTests()
    {
        _repo = new Mock<IPlanRepository>();
        _validator = new Mock<IValidator<PlanRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();
        _service = new PlanService(
            _repo.Object,
            _validator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<PlanService>>());

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<PlanRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    private static PlanRequestDto DtoValido() => new()
    {
        Nombre = "Professional",
        Codigo = "PROFESSIONAL",
        LimiteUsuarios = 20,
        LimiteEmbarquesMes = 1000,
        LimiteStorageGb = 10,
        PrecioMensual = 99,
        PrecioAnual = 990,
        ModulosDisponibles = ["embarques", "usuarios"]
    };

    private static Plan PlanExistente(Guid id) => new()
    {
        Id = id,
        Nombre = "Starter",
        Codigo = "STARTER",
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_ConCodigoRepetido_LanzaConflict()
    {
        _repo.Setup(r => r.GetByCodigoAsync("PROFESSIONAL")).ReturnsAsync(PlanExistente(Guid.NewGuid()));

        var act = async () => await _service.CreateAsync(DtoValido());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_Exitoso_RegistraAuditoriaYCreacion()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByCodigoAsync("PROFESSIONAL")).ReturnsAsync((Plan?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<Plan>())).ReturnsAsync(id);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(PlanExistente(id));

        var result = await _service.CreateAsync(DtoValido());

        result.Id.Should().Be(id);
        _auditoria.Verify(
            a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null,
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), null, null),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PlanNoExiste_LanzaNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Plan?)null);

        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), DtoValido());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CodigoDeOtroPlan_LanzaConflict()
    {
        var planId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(PlanExistente(planId));
        // Existe otro plan distinto con el mismo código a asignar.
        _repo.Setup(r => r.GetByCodigoAsync("PROFESSIONAL"))
            .ReturnsAsync(PlanExistente(Guid.NewGuid()));

        var act = async () => await _service.UpdateAsync(planId, DtoValido());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_Exitoso_EjecutaUpdateYAudita()
    {
        var planId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(PlanExistente(planId));
        _repo.Setup(r => r.GetByCodigoAsync("PROFESSIONAL")).ReturnsAsync((Plan?)null);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Plan>())).ReturnsAsync(true);

        var result = await _service.UpdateAsync(planId, DtoValido());

        // El update se ejecuta sobre la entidad mapeada con los datos del DTO.
        _repo.Verify(r => r.UpdateAsync(It.Is<Plan>(p =>
            p.Id == planId && p.Codigo == "PROFESSIONAL" && p.Nombre == "Professional")), Times.Once);
        // El DTO devuelto proviene de la re-lectura (PlanExistente => STARTER).
        result.Should().NotBeNull();
        _auditoria.Verify(
            a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null,
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), null, null),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_PlanNoExiste_LanzaNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Plan?)null);

        var act = async () => await _service.DeactivateAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_ConEmpresasSuscritas_LanzaBusinessError()
    {
        var planId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(PlanExistente(planId));
        _repo.Setup(r => r.CountEmpresasSuscritasAsync(planId)).ReturnsAsync(3);

        var act = async () => await _service.DeactivateAsync(planId);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeactivateAsync_SinEmpresas_Desactiva()
    {
        var planId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(PlanExistente(planId));
        _repo.Setup(r => r.CountEmpresasSuscritasAsync(planId)).ReturnsAsync(0);
        _repo.Setup(r => r.DeactivateAsync(planId)).ReturnsAsync(true);

        var result = await _service.DeactivateAsync(planId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_SoloActivosTrue_DevuelveSoloActivos()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _repo.Setup(r => r.GetAllAsync(true)).ReturnsAsync([
            PlanExistente(id1),
            new Plan { Id = id2, Nombre = "Pro", Codigo = "PRO", Activo = true, FechaCreacion = DateTime.UtcNow }
        ]);
        _repo.Setup(r => r.CountEmpresasSuscritasAsync(It.IsAny<Guid>())).ReturnsAsync(0);

        var result = (await _service.GetAllAsync(true)).ToList();

        result.Should().HaveCount(2);
        _repo.Verify(r => r.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SoloActivosFalse_DevuelveTodos()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetAllAsync(false)).ReturnsAsync([PlanExistente(id)]);
        _repo.Setup(r => r.CountEmpresasSuscritasAsync(id)).ReturnsAsync(5);

        var result = (await _service.GetAllAsync(false)).ToList();

        result.Should().HaveCount(1);
        result[0].EmpresasSuscritas.Should().Be(5);
        _repo.Verify(r => r.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_DevuelveDto()
    {
        var planId = Guid.NewGuid();
        var plan = PlanExistente(planId);
        plan.Nombre = "Enterprise";
        plan.Codigo = "ENTERPRISE";
        _repo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(plan);

        var result = await _service.GetByIdAsync(planId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(planId);
        result.Nombre.Should().Be("Enterprise");
        result.Codigo.Should().Be("ENTERPRISE");
        result.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_DevuelveNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Plan?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
