using Freiroute.BLL.Validators;
using Freiroute.DTO.Suscripcion;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de pagos (HU-011).
/// Valida suscripción, monto (> 0), moneda, método de pago y período.
/// </summary>
public class PagoValidatorTests
{
    private readonly PagoValidator _validator = new();

    private PagoRequestDto DtoValido() => new()
    {
        SuscripcionId = Guid.NewGuid(),
        Monto = 100.00m,
        Moneda = "USD",
        MetodoPago = "MANUAL",
        Referencia = "REF-001",
        PeriodoDesde = new DateTime(2026, 1, 1),
        PeriodoHasta = new DateTime(2026, 1, 31)
    };

    [Fact]
    public void Validate_SuscripcionIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.SuscripcionId = Guid.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuscripcionId");
    }

    [Fact]
    public void Validate_MontoCero_TieneError()
    {
        var dto = DtoValido();
        dto.Monto = 0;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Monto");
    }

    [Fact]
    public void Validate_MontoCien_SinError()
    {
        var dto = DtoValido();
        dto.Monto = 100;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "Monto");
    }

    [Fact]
    public void Validate_MonedaVacia_TieneError()
    {
        var dto = DtoValido();
        dto.Moneda = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Moneda");
    }

    [Fact]
    public void Validate_MetodoPagoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.MetodoPago = "BITCOIN";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MetodoPago");
    }

    [Fact]
    public void Validate_MetodoPagoManual_SinError()
    {
        var dto = DtoValido();
        dto.MetodoPago = "MANUAL";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "MetodoPago");
    }

    [Fact]
    public void Validate_MetodoPagoStripe_SinError()
    {
        var dto = DtoValido();
        dto.MetodoPago = "STRIPE";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "MetodoPago");
    }

    [Fact]
    public void Validate_MetodoPagoTransferencia_SinError()
    {
        var dto = DtoValido();
        dto.MetodoPago = "TRANSFERENCIA";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "MetodoPago");
    }

    [Fact]
    public void Validate_MetodoPagoEfectivo_SinError()
    {
        var dto = DtoValido();
        dto.MetodoPago = "EFECTIVO";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "MetodoPago");
    }

    [Fact]
    public void Validate_PeriodoDesdeVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PeriodoDesde = default;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PeriodoDesde");
    }

    [Fact]
    public void Validate_PeriodoHastaVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PeriodoHasta = default;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PeriodoHasta");
    }

    [Fact]
    public void Validate_PeriodoHastaIgualDesde_TieneError()
    {
        // El fin debe ser estrictamente posterior al inicio.
        var dto = DtoValido();
        dto.PeriodoDesde = new DateTime(2026, 1, 1);
        dto.PeriodoHasta = new DateTime(2026, 1, 1);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PeriodoHasta");
    }

    [Fact]
    public void Validate_PeriodoHastaAntesDesde_TieneError()
    {
        var dto = DtoValido();
        dto.PeriodoDesde = new DateTime(2026, 2, 1);
        dto.PeriodoHasta = new DateTime(2026, 1, 1);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PeriodoHasta");
    }

    [Fact]
    public void Validate_DtoValido_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
