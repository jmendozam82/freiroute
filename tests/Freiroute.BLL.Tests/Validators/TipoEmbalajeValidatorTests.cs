using Freiroute.BLL.Validators;
using Freiroute.DTO.Unidad;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de tipos de embalaje (HU-018).
/// Valida identificación, código corto (PLT, CAJA, TAM...) y
/// capacidades no negativas.
/// </summary>
public class TipoEmbalajeValidatorTests
{
    private readonly TipoEmbalajeValidator _validator = new();

    private TipoEmbalajeRequestDto DtoValido() => new()
    {
        Nombre = "Pallet estándar",
        Codigo = "PLT",
        Descripcion = "Pallet 1.2x1.0 m",
        CapacidadKg = 1500m,
        CapacidadM3 = 2m,
        Apilable = true
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
    public void Validate_CodigoVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoConEspacio_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = "PALLET 40";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CapacidadKgNegativa_TieneError()
    {
        var dto = DtoValido();
        dto.CapacidadKg = -10m;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "CapacidadKg");
    }

    [Fact]
    public void Validate_CapacidadM3Negativa_TieneError()
    {
        var dto = DtoValido();
        dto.CapacidadM3 = -1m;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "CapacidadM3");
    }

    [Fact]
    public void Validate_DtoValido_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}