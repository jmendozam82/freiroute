using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Tarifa;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests adicionales de tarifas base (HU-020, Sprint 3, ADR-015).
/// Cubre el simulador de costo (CA-04/CA-05), la gestión de recargos
/// (códigos únicos, validaciones) y el versionado de tarifas (CA-03):
/// UpdateAsync NUNCA modifica el historial — cierra la versión actual
/// (vigencia hasta ayer) y crea una NUEVA versión desde hoy.
/// </summary>
public class TarifaBaseServiceSimuladorTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid TarifaId = Guid.NewGuid();
    private static readonly Guid RecargoId = Guid.NewGuid();

    private readonly Mock<ITarifaBaseRepository> _tarifaRepo;
    private readonly Mock<IZonaEntregaRepository> _zonaRepo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly TarifaBaseService _service;

    public TarifaBaseServiceSimuladorTests()
    {
        _tarifaRepo = new Mock<ITarifaBaseRepository>();
        _zonaRepo = new Mock<IZonaEntregaRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new TarifaBaseService(
            _tarifaRepo.Object,
            _zonaRepo.Object,
            new TarifaBaseValidator(),
            _auditoria.Object,
            Mock.Of<ILogger<TarifaBaseService>>());
    }

    private static TarifaBase TarifaFija() => new()
    {
        Id = TarifaId,
        EmpresaId = EmpresaId,
        Nombre = "Flete Managua-Ocotal",
        Codigo = "FLE-MGA-OCT",
        TipoTarifa = TipoTarifa.FijoViaje,
        PrecioUnitario = 1000m,
        PrecioMinimo = 1200m,
        Moneda = "USD",
        FechaVigenciaDesde = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        Activo = true
    };

    private static RecargoTarifa Recargo(string codigo, string tipo, decimal valor) => new()
    {
        Id = Guid.NewGuid(),
        EmpresaId = EmpresaId,
        TarifaId = TarifaId,
        CodigoRecargo = codigo,
        Nombre = codigo.ToLower(),
        TipoCalculo = tipo,
        Valor = valor,
        Activo = true
    };

    private static SimularCostoRequestDto DtoSimulacion() => new()
    {
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = TipoServicioTransporte.Estandar,
        FechaPickup = default,
        PesoKg = 1000,
        ValorDeclarado = 5000m
    };

    [Fact]
    public async Task SimularCostoAsync_CuandoNoExisteTarifaVigente_RetornaTarifaEncontradaFalse()
    {
        _tarifaRepo.Setup(r => r.GetVigenteAsync(
            EmpresaId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((TarifaBase?)null);

        var result = await _service.SimularCostoAsync(DtoSimulacion(), EmpresaId);

        result.TarifaEncontrada.Should().BeFalse();
        result.MensajeAdvertencia.Should().Contain("No existe tarifa vigente");
    }

    [Fact]
    public async Task SimularCostoAsync_CuandoTarifaFija_AplicaPrecioMinimoRecargosYSeguro()
    {
        _tarifaRepo.Setup(r => r.GetVigenteAsync(
            EmpresaId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(TarifaFija());
        _tarifaRepo.Setup(r => r.GetRecargosAsync(TarifaId, EmpresaId)).ReturnsAsync(new List<RecargoTarifa>
        {
            Recargo(CodigoRecargo.Combustible, TipoCalculoRecargo.Porcentaje, 10m),
            Recargo(CodigoRecargo.Seguro, TipoCalculoRecargo.Porcentaje, 2m)
        });

        var result = await _service.SimularCostoAsync(DtoSimulacion(), EmpresaId);

        result.TarifaEncontrada.Should().BeTrue();
        result.CostoBase.Should().Be(1200m);           // Precio mínimo aplicado (1000 < 1200).
        result.TotalRecargos.Should().Be(220m);        // 10% de 1200 + 2% del valor declarado 5000.
        result.CostoTotal.Should().Be(1420m);
        result.MensajeAdvertencia.Should().Contain("precio mínimo");
        result.MensajeAdvertencia.Should().Contain("valor declarado");
    }

    [Fact]
    public async Task SimularCostoAsync_CuandoTarifaPorKg_CalculaProductoConRecargoMontoFijo()
    {
        var tarifa = new TarifaBase
        {
            Id = TarifaId,
            EmpresaId = EmpresaId,
            Nombre = "Flete por kg",
            TipoTarifa = TipoTarifa.PorKg,
            PrecioUnitario = 2.5m,
            Moneda = "USD",
            Activo = true
        };
        _tarifaRepo.Setup(r => r.GetVigenteAsync(
            EmpresaId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(tarifa);
        _tarifaRepo.Setup(r => r.GetRecargosAsync(TarifaId, EmpresaId)).ReturnsAsync(new List<RecargoTarifa>
        {
            Recargo(CodigoRecargo.Peaje, TipoCalculoRecargo.MontoFijo, 50m)
        });

        var dto = DtoSimulacion();
        dto.PesoKg = 400;
        var result = await _service.SimularCostoAsync(dto, EmpresaId);

        result.CostoBase.Should().Be(1000m);   // 2.5 * 400.
        result.TotalRecargos.Should().Be(50m);
        result.CostoTotal.Should().Be(1050m);
    }

    [Fact]
    public async Task AgregarRecargoAsync_CuandoCodigoDuplicado_LanzaConflictException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());
        _tarifaRepo.Setup(r => r.GetRecargosAsync(TarifaId, EmpresaId)).ReturnsAsync(new List<RecargoTarifa>
        {
            Recargo(CodigoRecargo.Combustible, TipoCalculoRecargo.Porcentaje, 10m)
        });

        var act = async () => await _service.AgregarRecargoAsync(
            TarifaId, new RecargoTarifaRequestDto
            {
                CodigoRecargo = CodigoRecargo.Combustible,
                Nombre = "Combustible",
                TipoCalculo = TipoCalculoRecargo.Porcentaje,
                Valor = 10m
            }, EmpresaId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AgregarRecargoAsync_CuandoCodigoDesconocido_LanzaBusinessException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());

        var act = async () => await _service.AgregarRecargoAsync(
            TarifaId, new RecargoTarifaRequestDto
            {
                CodigoRecargo = "NO_EXISTE",
                Nombre = "X",
                TipoCalculo = TipoCalculoRecargo.MontoFijo,
                Valor = 10m
            }, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("El código del recargo no es válido.*");
    }

    [Fact]
    public async Task AgregarRecargoAsync_CuandoPorcentajeMayor100_LanzaBusinessException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());

        var act = async () => await _service.AgregarRecargoAsync(
            TarifaId, new RecargoTarifaRequestDto
            {
                CodigoRecargo = CodigoRecargo.Urgencia,
                Nombre = "Urgencia",
                TipoCalculo = TipoCalculoRecargo.Porcentaje,
                Valor = 150m
            }, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Un porcentaje no puede exceder 100%*");
    }

    [Fact]
    public async Task UpdateRecargoAsync_CuandoNoExisteRecargo_LanzaNotFoundException()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());
        _tarifaRepo.Setup(r => r.GetRecargosAsync(TarifaId, EmpresaId)).ReturnsAsync(new List<RecargoTarifa>());

        var act = async () => await _service.UpdateRecargoAsync(
            TarifaId, RecargoId, new RecargoTarifaRequestDto
            {
                CodigoRecargo = CodigoRecargo.Peaje,
                Nombre = "Peaje",
                TipoCalculo = TipoCalculoRecargo.MontoFijo,
                Valor = 10m
            }, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_CierraVersioActualYCreaNueva()
    {
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());
        _tarifaRepo.Setup(r => r.CerrarVigenciaAsync(TarifaId, EmpresaId, It.IsAny<DateOnly>())).ReturnsAsync(true);
        _tarifaRepo.Setup(r => r.CreateAsync(It.IsAny<TarifaBase>())).ReturnsAsync(TarifaId);
        _tarifaRepo.Setup(r => r.GetRecargosAsync(TarifaId, EmpresaId)).ReturnsAsync(new List<RecargoTarifa>
        {
            Recargo(CodigoRecargo.Combustible, TipoCalculoRecargo.Porcentaje, 10m)
        });
        _tarifaRepo.Setup(r => r.GetByIdAsync(TarifaId, EmpresaId)).ReturnsAsync(TarifaFija());

        var dto = new TarifaBaseRequestDto
        {
            Nombre = "Flete Managua-Ocotal v2",
            Codigo = "FLE-MGA-OCT",
            ModoTransporte = ModoTransporte.Ftl,
            TipoServicio = TipoServicioTransporte.Estandar,
            TipoTarifa = TipoTarifa.FijoViaje,
            PrecioUnitario = 1100m,
            Moneda = "USD",
            FechaVigenciaDesde = DateOnly.FromDateTime(DateTime.Today),
            Recargos = []
        };

        var result = await _service.UpdateAsync(TarifaId, dto, EmpresaId);

        result.Id.Should().Be(TarifaId);
        // El historial se conserva: la versión anterior se cerró ANTES de crear la nueva.
        _tarifaRepo.Verify(r => r.CerrarVigenciaAsync(TarifaId, EmpresaId,
            It.Is<DateOnly>(d => d == DateOnly.FromDateTime(DateTime.Today).AddDays(-1))), Times.Once);
        _tarifaRepo.Verify(r => r.CreateAsync(It.Is<TarifaBase>(t => t.FechaVigenciaDesde == DateOnly.FromDateTime(DateTime.Today))), Times.Once);
        // Los recargos activos se copian a la nueva versión.
        _tarifaRepo.Verify(r => r.CreateRecargoAsync(It.IsAny<RecargoTarifa>()), Times.Once);
    }
}