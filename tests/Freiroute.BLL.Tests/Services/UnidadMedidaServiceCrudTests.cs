using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Unidad;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del CRUD de unidades de medida (HU-018): GetAll con filtro por
/// tipo (CA-01), UpdateAsync con validación de símbolo único y soft delete
/// (ADR-005). Complementa la cobertura del simulador de conversión.
/// </summary>
public class UnidadMedidaServiceCrudTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid UnidadId = Guid.NewGuid();

    private readonly Mock<IUnidadMedidaRepository> _unidadRepo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IValidator<UnidadMedidaRequestDto>> _validator;
    private readonly UnidadMedidaService _service;

    public UnidadMedidaServiceCrudTests()
    {
        _unidadRepo = new Mock<IUnidadMedidaRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        _validator = new Mock<IValidator<UnidadMedidaRequestDto>>();
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UnidadMedidaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new UnidadMedidaService(
            _unidadRepo.Object,
            _validator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<UnidadMedidaService>>());
    }

    private static UnidadMedida Unidad() => new()
    {
        Id = UnidadId,
        EmpresaId = EmpresaId,
        Nombre = "Kilogramo",
        Simbolo = "kg",
        Tipo = "PESO",
        FactorConversion = 1m,
        UnidadBase = "kg",
        Activo = true
    };

    private static UnidadMedidaRequestDto Dto() => new()
    {
        Nombre = "Kilogramo",
        Simbolo = "kg",
        Tipo = "PESO",
        FactorConversion = 1m,
        UnidadBase = "kg"
    };

    [Fact]
    public async Task GetAllAsync_ConFiltroTipo_RetornaSoloUnidadesDelTipo()
    {
        _unidadRepo.Setup(r => r.GetAllAsync(EmpresaId, "PESO"))
            .ReturnsAsync(new List<UnidadMedida> { Unidad() });

        var result = await _service.GetAllAsync(EmpresaId, "PESO");

        result.Should().HaveCount(1);
        result.First().Simbolo.Should().Be("kg");
        result.First().TipoLabel.Should().Be("Peso");
        _unidadRepo.Verify(r => r.GetAllAsync(EmpresaId, "PESO"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(UnidadId, EmpresaId))
            .ReturnsAsync((UnidadMedida?)null);

        var act = async () => await _service.UpdateAsync(UnidadId, Dto(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_SimboloEnUsoPorOtraUnidad_LanzaConflictException()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(UnidadId, EmpresaId))
            .ReturnsAsync(Unidad());
        _unidadRepo.Setup(r => r.GetBySimboloAsync("lb", EmpresaId))
            .ReturnsAsync(new UnidadMedida { Id = Guid.NewGuid(), Simbolo = "lb" });

        var dto = Dto();
        dto.Simbolo = "lb";

        var act = async () => await _service.UpdateAsync(UnidadId, dto, EmpresaId);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*símbolo 'lb'*");
    }

    [Fact]
    public async Task UpdateAsync_DtoValido_ActualizaAuditaYRetornaUnidad()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(UnidadId, EmpresaId))
            .ReturnsAsync(Unidad());
        _unidadRepo.Setup(r => r.GetBySimboloAsync("kg", EmpresaId))
            .ReturnsAsync((UnidadMedida?)null);
        _unidadRepo.Setup(r => r.UpdateAsync(It.IsAny<UnidadMedida>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateAsync(UnidadId, Dto(), EmpresaId);

        result.Simbolo.Should().Be("kg");
        _unidadRepo.Verify(r => r.UpdateAsync(It.Is<UnidadMedida>(u => u.Id == UnidadId && u.Simbolo == "kg")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            "unidades_medida", AccionAuditoria.UPDATE, EmpresaId, null,
            "UnidadMedida", UnidadId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_RetornaTrueYAudita()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(UnidadId, EmpresaId))
            .ReturnsAsync(Unidad());
        _unidadRepo.Setup(r => r.DeactivateAsync(UnidadId, EmpresaId))
            .ReturnsAsync(true);

        var result = await _service.DeactivateAsync(UnidadId, EmpresaId);

        result.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "unidades_medida", AccionAuditoria.DEACTIVATE, EmpresaId, null,
            "UnidadMedida", UnidadId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(UnidadId, EmpresaId))
            .ReturnsAsync((UnidadMedida?)null);

        var act = async () => await _service.DeactivateAsync(UnidadId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}