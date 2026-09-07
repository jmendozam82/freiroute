using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Tarifa;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del CRUD de tarifas base (HU-020): GetAll paginado, GetById,
/// GetVigenteAsync (CA-05 null-safe), CreateAsync con recargos,
/// DeactivateAsync y DeactivateRecargoAsync (ADR-005 soft delete).
/// Complementa TarifaBaseServiceSimuladorTests (CA-04).
/// </summary>
public class TarifaBaseServiceCrudTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid TarifaId = Guid.NewGuid();
    private static readonly Guid RecargoId = Guid.NewGuid();

    private readonly Mock<ITarifaBaseRepository> _tarifaRepo;
    private readonly Mock<IZonaEntregaRepository> _zonaRepo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IValidator<TarifaBaseRequestDto>> _validator;
    private readonly TarifaBaseService _service;

    public TarifaBaseServiceCrudTests()
    {
        _tarifaRepo = new Mock<ITarifaBaseRepository>();
        _zonaRepo = new Mock<IZonaEntregaRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        _validator = new Mock<IValidator<TarifaBaseRequestDto>>();
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<TarifaBaseRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _tarifaRepo.Setup(r => r.GetRecargosAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync(new List<RecargoTarifa>());
        _zonaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync((ZonaEntrega?)null);
        _service = new TarifaBaseService(
            _tarifaRepo.Object,
            _zonaRepo.Object,
            _validator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<TarifaBaseService>>());
    }

    private static TarifaBase Tarifa() => new()
    {
        Id = TarifaId,
        EmpresaId = EmpresaId,
        Nombre = "Flete Managua-Ocotal",
        Codigo = "FM-O",
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = "ESTANDAR",
        TipoTarifa = TipoTarifa.FijoViaje,
        PrecioUnitario = 1200m,
        Moneda = "USD",
        FechaVigenciaDesde = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
        Activo = true
    };

    private static TarifaBaseRequestDto Dto() => new()
    {
        Nombre = "Flete Managua-Ocotal",
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = "ESTANDAR",
        TipoTarifa = TipoTarifa.FijoViaje,
        PrecioUnitario = 1200m,
        Moneda = "USD"
    };

    [Fact]
    public async Task GetAllAsync_ConFiltros_RetornaPaginaMapeada()
    {
        _tarifaRepo.Setup(r => r.GetAllAsync(EmpresaId, null, null, ModoTransporte.Ftl, true))
            .ReturnsAsync(new List<TarifaBase> { Tarifa(), Tarifa() });

        var result = await _service.GetAllAsync(EmpresaId, null, null, ModoTransporte.Ftl, true, 1, 20);

        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Nombre.Should().Be("Flete Managua-Ocotal");
        result.Items.First().EsVigente.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync((TarifaBase?)null);

        var result = await _service.GetByIdAsync(TarifaId, EmpresaId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetVigenteAsync_CuandoNoExiste_RetornaNullSinExcepcion()
    {
        _tarifaRepo.Setup(r => r.GetVigenteAsync(EmpresaId, null, null, ModoTransporte.Ftl, "ESTANDAR", It.IsAny<DateOnly>()))
            .ReturnsAsync((TarifaBase?)null);

        var result = await _service.GetVigenteAsync(
            EmpresaId, null, null, ModoTransporte.Ftl, "ESTANDAR", DateOnly.FromDateTime(DateTime.Today));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetVigenteAsync_CuandoExiste_RetornaTarifaVigente()
    {
        _tarifaRepo.Setup(r => r.GetVigenteAsync(EmpresaId, null, null, ModoTransporte.Ftl, "ESTANDAR", It.IsAny<DateOnly>()))
            .ReturnsAsync(Tarifa());

        var result = await _service.GetVigenteAsync(
            EmpresaId, null, null, ModoTransporte.Ftl, "ESTANDAR", DateOnly.FromDateTime(DateTime.Today));

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Flete Managua-Ocotal");
    }

    [Fact]
    public async Task CreateAsync_DtoValido_CreaTarifaConRecargosYAudita()
    {
        var dto = Dto();
        dto.Recargos.Add(new RecargoTarifaRequestDto
        {
            CodigoRecargo = CodigoRecargo.Combustible,
            Nombre = "Combustible",
            TipoCalculo = TipoCalculoRecargo.Porcentaje,
            Valor = 8m
        });
        _tarifaRepo.Setup(r => r.CreateAsync(It.IsAny<TarifaBase>()))
            .ReturnsAsync(TarifaId);
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync(Tarifa());

        var result = await _service.CreateAsync(dto, EmpresaId);

        result.Nombre.Should().Be("Flete Managua-Ocotal");
        _tarifaRepo.Verify(r => r.CreateAsync(It.Is<TarifaBase>(t => t.EmpresaId == EmpresaId && t.PrecioUnitario == 1200m)), Times.Once);
        _tarifaRepo.Verify(r => r.CreateRecargoAsync(It.Is<RecargoTarifa>(rc =>
            rc.TarifaId == TarifaId && rc.CodigoRecargo == CodigoRecargo.Combustible)), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            "tarifas_base", AccionAuditoria.CREATE, EmpresaId, null,
            "TarifaBase", TarifaId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DtoInvalido_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<TarifaBaseRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("PrecioUnitario", "El precio es obligatorio") }));

        var act = async () => await _service.CreateAsync(Dto(), EmpresaId);

        await act.Should().ThrowAsync<ValidationException>();
        _tarifaRepo.Verify(r => r.CreateAsync(It.IsAny<TarifaBase>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_RetornaTrueYAudita()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync(Tarifa());
        _tarifaRepo.Setup(r => r.DeactivateAsync(TarifaId, EmpresaId))
            .ReturnsAsync(true);

        var result = await _service.DeactivateAsync(TarifaId, EmpresaId);

        result.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "tarifas_base", AccionAuditoria.DEACTIVATE, EmpresaId, null,
            "TarifaBase", TarifaId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync((TarifaBase?)null);

        var act = async () => await _service.DeactivateAsync(TarifaId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateRecargoAsync_CuandoExiste_RetornaTrueYAudita()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync(Tarifa());
        _tarifaRepo.Setup(r => r.DeactivateRecargoAsync(RecargoId, EmpresaId))
            .ReturnsAsync(true);

        var result = await _service.DeactivateRecargoAsync(TarifaId, RecargoId, EmpresaId);

        result.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "tarifas_base", AccionAuditoria.DEACTIVATE, EmpresaId, null,
            "RecargoTarifa", RecargoId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task DeactivateRecargoAsync_CuandoRepoRetornaFalse_LanzaNotFoundException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId))
            .ReturnsAsync(Tarifa());
        _tarifaRepo.Setup(r => r.DeactivateRecargoAsync(RecargoId, EmpresaId))
            .ReturnsAsync(false);

        var act = async () => await _service.DeactivateRecargoAsync(TarifaId, RecargoId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}