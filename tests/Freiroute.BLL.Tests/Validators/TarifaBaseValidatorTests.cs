using Freiroute.BLL.Validators;
using Freiroute.DTO.Tarifa;
using Freiroute.Utility.Constants;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de tarifas base y sus recargos (HU-020, ADR-015).
/// Valida aplicación (zona/modo/servicio), modelo de precio (POR_UNIDAD
/// bloqueado en esta versión), vigencia y coherencia de recargos.
/// </summary>
public class TarifaBaseValidatorTests
{
    private readonly TarifaBaseValidator _validator = new();

    private static readonly DateOnly Desde = new(2026, 1, 1);

    private TarifaBaseRequestDto DtoValido() => new()
    {
        Nombre = "Tarifa FTL Managua-Rivas",
        Codigo = "FTL-MGA-RIV",
        ZonaOrigenId = Guid.NewGuid(),
        ZonaDestinoId = Guid.NewGuid(),
        ModoTransporte = ModoTransporte.Ftl,
        TipoServicio = TipoServicioTransporte.Estandar,
        TipoTarifa = TipoTarifa.FijoViaje,
        PrecioUnitario = 850m,
        PrecioMinimo = 500m,
        Moneda = "USD",
        FechaVigenciaDesde = Desde,
        FechaVigenciaHasta = null,
        Recargos =
        [
            new RecargoTarifaRequestDto
            {
                CodigoRecargo = CodigoRecargo.Combustible,
                Nombre = "Recargo combustible",
                TipoCalculo = TipoCalculoRecargo.Porcentaje,
                Valor = 12m
            }
        ]
    };

    [Fact]
    public void Validate_NombreVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Nombre = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_ModoTransporteInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.ModoTransporte = "TELEPORTACION";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "ModoTransporte");
    }

    [Fact]
    public void Validate_TipoServicioInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.TipoServicio = "URGENTISIMO";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TipoServicio");
    }

    [Fact]
    public void Validate_TipoTarifaPorUnidad_TieneError()
    {
        // POR_UNIDAD está bloqueado en esta versión (ADR-015).
        var dto = DtoValido();
        dto.TipoTarifa = TipoTarifa.PorUnidad;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TipoTarifa");
    }

    [Fact]
    public void Validate_PrecioUnitarioCero_TieneError()
    {
        var dto = DtoValido();
        dto.PrecioUnitario = 0m;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "PrecioUnitario");
    }

    [Fact]
    public void Validate_FechaHastaAntesDeDesde_TieneError()
    {
        var dto = DtoValido();
        dto.FechaVigenciaHasta = Desde.AddDays(-1);

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "FechaVigenciaHasta");
    }

    [Fact]
    public void Validate_RecargoPorcentajeExcede100_TieneError()
    {
        var dto = DtoValido();
        dto.Recargos[0].Valor = 101m;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recargos[0].Valor");
    }

    [Fact]
    public void Validate_RecargoConCodigoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.Recargos[0].CodigoRecargo = "DESCUENTO_LOCURA";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Recargos[0].CodigoRecargo");
    }

    [Fact]
    public void Validate_RecargoCodigoDuplicado_TieneError()
    {
        var dto = DtoValido();
        dto.Recargos.Add(new RecargoTarifaRequestDto
        {
            CodigoRecargo = CodigoRecargo.Combustible, // Repetido.
            Nombre = "Combustible 2",
            TipoCalculo = TipoCalculoRecargo.MontoFijo,
            Valor = 50m
        });

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Recargos");
    }

    [Fact]
    public void Validate_DtoValidoCompleto_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}