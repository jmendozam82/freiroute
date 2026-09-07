using Freiroute.BLL.Validators;
using Freiroute.DTO.Unidad;
using Freiroute.Utility.Constants;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de unidades de medida (HU-018).
/// Valida tipo (PESO, VOLUMEN, LONGITUD, TEMPERATURA), factor positivo
/// y coherencia entre tipo y unidad base (kg, m3, m, C).
/// </summary>
public class UnidadMedidaValidatorTests
{
    private readonly UnidadMedidaValidator _validator = new();

    private UnidadMedidaRequestDto DtoValido() => new()
    {
        Nombre = "Kilogramo",
        Simbolo = "kg",
        Tipo = TipoMedida.Peso,
        FactorConversion = 1m,
        UnidadBase = TipoMedida.BaseKg
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
    public void Validate_SimboloVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Simbolo = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Simbolo");
    }

    [Fact]
    public void Validate_TipoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.Tipo = "PIEZAS";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Tipo");
    }

    [Fact]
    public void Validate_FactorCero_TieneError()
    {
        var dto = DtoValido();
        dto.FactorConversion = 0m;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "FactorConversion");
    }

    [Fact]
    public void Validate_UnidadBaseIncoherenteConTipo_TieneError()
    {
        // Tipo PESO con unidad base m3 es incoherente.
        var dto = DtoValido();
        dto.UnidadBase = TipoMedida.BaseM3;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "UnidadBase");
    }

    [Fact]
    public void Validate_VolumenConUnidadBaseM3_SinError()
    {
        var dto = DtoValido();
        dto.Nombre = "Metro cúbico";
        dto.Simbolo = "m3";
        dto.Tipo = TipoMedida.Volumen;
        dto.UnidadBase = TipoMedida.BaseM3;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TemperaturaConUnidadBaseC_SinError()
    {
        var dto = DtoValido();
        dto.Nombre = "Grado Celsius";
        dto.Simbolo = "C";
        dto.Tipo = TipoMedida.Temperatura;
        dto.UnidadBase = TipoMedida.BaseC;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}