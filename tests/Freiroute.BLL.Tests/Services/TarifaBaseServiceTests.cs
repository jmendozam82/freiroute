using FluentValidation;
using FluentAssertions;
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
/// Tests de la lógica de negocio de tarifas base (HU-020, ADR-015).
/// Cubre el versionado en UpdateAsync (cierre de vigencia la víspera),
/// la copia de recargos y el simulador de costo con precio mínimo y
/// seguro sobre valor declarado (CA-04/CA-08).
/// </summary>
public class TarifaBaseServiceTests
{
    private readonly Mock<ITarifaBaseRepository> _repo;
    private readonly Mock<IZonaEntregaRepository> _zonas;
    private readonly Mock<IValidator<TarifaBaseRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly TarifaBaseService _service;

    public TarifaBaseServiceTests()
    {
        _repo = new Mock<ITarifaBaseRepository>();
        _zonas = new Mock<IZonaEntregaRepository>();
        _validator = new Mock<IValidator<TarifaBaseRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<TarifaBaseRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new TarifaBaseService(
            _repo.Object, _zonas.Object, _validator.Object,
            _auditoria.Object, Mock.Of<ILogger<TarifaBaseService>>());
    }

    private static TarifaBase Tarifa(
        Guid id,
        string tipoTarifa = TipoTarifa.PorKg,
        decimal precioUnitario = 2m,
        decimal? precioMinimo = null) => new()
    {
        Id = id,
        EmpresaId = Guid.NewGuid(),
        Nombre = "Tarifa FTL Managua-Rivas",
        ZonaOrigenId = Guid.NewGuid(),
        ZonaDestinoId = Guid.NewGuid(),
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = TipoServicioTransporte.Estandar,
        TipoTarifa = tipoTarifa,
        PrecioUnitario = precioUnitario,
        PrecioMinimo = precioMinimo,
        Moneda = "USD",
        FechaVigenciaDesde = DateOnly.FromDateTime(DateTime.Today),
        FechaVigenciaHasta = null,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    private static RecargoTarifa Recargo(
        string codigo, string tipoCalculo, decimal valor, bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        TarifaId = Guid.NewGuid(),
        CodigoRecargo = codigo,
        Nombre = codigo,
        TipoCalculo = tipoCalculo,
        Valor = valor,
        Activo = activo,
        FechaCreacion = DateTime.UtcNow
    };

    private TarifaBaseRequestDto DtoValido() => new()
    {
        Nombre = "Tarifa FTL Managua-Rivas",
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = TipoServicioTransporte.Estandar,
        TipoTarifa = TipoTarifa.FijoViaje,
        PrecioUnitario = 850m,
        Moneda = "USD",
        FechaVigenciaDesde = new DateOnly(2026, 1, 1)
    };

    private void SetupGetById(Guid id, TarifaBase tarifa, Guid empresaId)
    {
        _repo.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(tarifa);
    }

    // ── UpdateAsync — versionado (ADR-015, CA-03) ───────────────

    [Fact]
    public async Task UpdateAsync_TarifaNoExiste_LanzaNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((TarifaBase?)null);

        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), DtoValido(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_Exitoso_CierraVigenciaAYerYCreaNuevaVersion()
    {
        var empresaId = Guid.NewGuid();
        var viejaId = Guid.NewGuid();
        var nuevaId = Guid.NewGuid();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var vieja = Tarifa(viejaId, TipoTarifa.FijoViaje, 800m, 500m);
        SetupGetById(viejaId, vieja, empresaId);
        _repo.Setup(r => r.CerrarVigenciaAsync(viejaId, empresaId, It.IsAny<DateOnly>()))
            .ReturnsAsync(true);
        // Segunda llamada a GetByIdAsync (tras la create) → nueva versión.
        _repo.SetupSequence(r => r.CreateAsync(It.IsAny<TarifaBase>()))
            .ReturnsAsync(nuevaId);
        _repo.Setup(r => r.GetRecargosAsync(viejaId, empresaId))
            .ReturnsAsync(
            [
                Recargo(CodigoRecargo.Combustible, TipoCalculoRecargo.Porcentaje, 10m)
            ]);
        _repo.Setup(r => r.GetByIdAsync(nuevaId, empresaId))
            .ReturnsAsync(Tarifa(nuevaId, TipoTarifa.FijoViaje, 850m, 500m));

        var resultado = await _service.UpdateAsync(viejaId, DtoValido(), empresaId);

        resultado.Id.Should().Be(nuevaId);
        // La vigencia de la versión anterior cierra AYER (hoy - 1).
        _repo.Verify(r => r.CerrarVigenciaAsync(viejaId, empresaId, hoy.AddDays(-1)), Times.Once);
        // La nueva versión se crea con vigencia desde HOY (ignora la fecha del DTO).
        _repo.Verify(r => r.CreateAsync(It.Is<TarifaBase>(t =>
            t.FechaVigenciaDesde == hoy)), Times.Once);
        // Los recargos activos de la versión anterior se copian a la nueva.
        _repo.Verify(r => r.CreateRecargoAsync(It.Is<RecargoTarifa>(rc =>
            rc.TarifaId == nuevaId && rc.CodigoRecargo == CodigoRecargo.Combustible)), Times.Once);
        // La versión anterior nunca se modifica salvo cierre de vigencia.
        _repo.Verify(r => r.UpdateRecargoAsync(It.IsAny<RecargoTarifa>()), Times.Never);
    }

    // ── SimularCostoAsync (CA-04, CA-08) ────────────────────────

    [Fact]
    public async Task SimularCosto_SinTarifaVigente_RetornaNoEncontrada()
    {
        _repo.Setup(r => r.GetVigenteAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((TarifaBase?)null);

        var resultado = await _service.SimularCostoAsync(new SimularCostoRequestDto
        {
            ModoTransporte = ModoTransporte.Ftl,
            TipoServicio = TipoServicioTransporte.Estandar,
            PesoKg = 1000m
        }, Guid.NewGuid());

        resultado.TarifaEncontrada.Should().BeFalse();
        resultado.MensajeAdvertencia.Should().NotBeNullOrWhiteSpace();
        resultado.CostoTotal.Should().Be(0m);
    }

    [Fact]
    public async Task SimularCosto_PorKgConPrecioMinimo_AplicaMinimo()
    {
        var empresaId = Guid.NewGuid();
        var tarifa = Tarifa(Guid.NewGuid(), TipoTarifa.PorKg, 2m, 500m);
        _repo.Setup(r => r.GetVigenteAsync(empresaId, It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(tarifa);
        _repo.Setup(r => r.GetRecargosAsync(tarifa.Id, empresaId)).ReturnsAsync([]);

        var dto = new SimularCostoRequestDto
        {
            ModoTransporte = ModoTransporte.Ftl,
            TipoServicio = TipoServicioTransporte.Estandar,
            PesoKg = 100m // 100 × 2 = 200 < mínimo 500.
        };

        var resultado = await _service.SimularCostoAsync(dto, empresaId);

        resultado.TarifaEncontrada.Should().BeTrue();
        resultado.CostoBase.Should().Be(500m); // Se aplicó el precio mínimo.
        resultado.CostoTotal.Should().Be(500m);
        resultado.MensajeAdvertencia.Should().Contain("precio mínimo");
    }

    [Fact]
    public async Task SimularCosto_SeguroSobreValorDeclarado_SumaRecargo()
    {
        var empresaId = Guid.NewGuid();
        var tarifa = Tarifa(Guid.NewGuid(), TipoTarifa.PorKg, 3m);
        tarifa.PrecioMinimo = null;
        _repo.Setup(r => r.GetVigenteAsync(empresaId, It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(tarifa);
        _repo.Setup(r => r.GetRecargosAsync(tarifa.Id, empresaId))
            .ReturnsAsync(
            [
                Recargo(CodigoRecargo.Seguro, TipoCalculoRecargo.Porcentaje, 2m)
            ]);

        var dto = new SimularCostoRequestDto
        {
            ModoTransporte = ModoTransporte.Ftl,
            TipoServicio = TipoServicioTransporte.Estandar,
            PesoKg = 100m,          // 100 × 3 = 300
            ValorDeclarado = 10000m // 2% de 10000 = 200
        };

        var resultado = await _service.SimularCostoAsync(dto, empresaId);

        resultado.CostoBase.Should().Be(300m);
        resultado.RecargosAplicados.Should().ContainSingle(r => r.Nombre == CodigoRecargo.Seguro);
        resultado.TotalRecargos.Should().Be(200m);
        resultado.CostoTotal.Should().Be(500m);
        resultado.MensajeAdvertencia.Should().Contain("valor declarado");
    }

    // ── AgregarRecargoAsync (código único) ─────────────────────

    [Fact]
    public async Task AgregarRecargo_CodigoDuplicadoActivo_LanzaConflict()
    {
        var empresaId = Guid.NewGuid();
        var tarifa = Tarifa(Guid.NewGuid());
        SetupGetById(tarifa.Id, tarifa, empresaId);
        _repo.Setup(r => r.GetRecargosAsync(tarifa.Id, empresaId))
            .ReturnsAsync(
            [
                Recargo(CodigoRecargo.Combustible, TipoCalculoRecargo.Porcentaje, 10m)
            ]);

        var act = async () => await _service.AgregarRecargoAsync(tarifa.Id, new RecargoTarifaRequestDto
        {
            CodigoRecargo = CodigoRecargo.Combustible,
            Nombre = "Combustible",
            TipoCalculo = TipoCalculoRecargo.Porcentaje,
            Valor = 12m
        }, empresaId);

        await act.Should().ThrowAsync<ConflictException>();
    }
}