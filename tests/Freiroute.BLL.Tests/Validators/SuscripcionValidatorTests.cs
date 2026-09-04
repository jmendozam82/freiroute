using Freiroute.BLL.Validators;
using Freiroute.DTO.Suscripcion;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de suscripciones (HU-011).
/// Valida empresa, plan, ciclo (MENSUAL o ANUAL), precio pactado y moneda.
/// </summary>
public class SuscripcionValidatorTests
{
    private readonly SuscripcionValidator _validator = new();

    private SuscripcionRequestDto DtoValido() => new()
    {
        EmpresaId = Guid.NewGuid(),
        PlanId = Guid.NewGuid(),
        TipoCiclo = "MENSUAL",
        PrecioPactado = 99.99m,
        MonedaPactada = "USD"
    };

    [Fact]
    public void Validate_EmpresaIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.EmpresaId = Guid.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmpresaId");
    }

    [Fact]
    public void Validate_PlanIdVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PlanId = Guid.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void Validate_TipoCicloTrimestral_TieneError()
    {
        var dto = DtoValido();
        dto.TipoCiclo = "TRIMESTRAL";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoCiclo");
    }

    [Fact]
    public void Validate_TipoCicloMensual_SinError()
    {
        var dto = DtoValido();
        dto.TipoCiclo = "MENSUAL";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "TipoCiclo");
    }

    [Fact]
    public void Validate_TipoCicloAnual_SinError()
    {
        var dto = DtoValido();
        dto.TipoCiclo = "ANUAL";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "TipoCiclo");
    }

    [Fact]
    public void Validate_PrecioPactadoNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.PrecioPactado = -10;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrecioPactado");
    }

    [Fact]
    public void Validate_MonedaPactadaVacia_TieneError()
    {
        var dto = DtoValido();
        dto.MonedaPactada = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MonedaPactada");
    }

    [Fact]
    public void Validate_MonedaPactadaExcede10_TieneError()
    {
        var dto = DtoValido();
        dto.MonedaPactada = "USDEURGBPXX";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MonedaPactada");
    }

    [Fact]
    public void Validate_DtoValido_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
