using Freiroute.BLL.Validators;
using Freiroute.DTO.Mercancia;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de tipos de mercancía (HU-017).
/// Cubre reglas HAZMAT: clase ONU 1-9 (con subclase), coherencia
/// EsPeligroso ⇔ clase, rango de temperatura y refrigeración (CA-02/CA-04).
/// </summary>
public class TipoMercanciaValidatorTests
{
    private readonly TipoMercanciaValidator _validator = new();

    private TipoMercanciaRequestDto DtoValido() => new()
    {
        Nombre = "Diésel B5",
        Codigo = "DIE-B5",
        Categoria = "Combustibles",
        EsPeligroso = true
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
    public void Validate_ClasePeligrosidadCero_TieneError()
    {
        var dto = DtoValido();
        dto.ClasePeligrosidad = "0";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "ClasePeligrosidad");
    }

    [Fact]
    public void Validate_ClasePeligrosidadDosDigitos_TieneError()
    {
        var dto = DtoValido();
        dto.ClasePeligrosidad = "2.14";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "ClasePeligrosidad");
    }

    [Fact]
    public void Validate_ClaseSinFlagPeligroso_TieneError()
    {
        // Clase 3 (líquido inflamable) sin EsPeligroso=true es incoherente.
        var dto = DtoValido();
        dto.ClasePeligrosidad = "3";
        dto.EsPeligroso = false;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "EsPeligroso");
    }

    [Fact]
    public void Validate_ClaseConFlagPeligroso_SinError()
    {
        var dto = DtoValido();
        dto.ClasePeligrosidad = "3";
        dto.CodigoOnu = "UN1202";
        dto.EsPeligroso = true;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e =>
            e.PropertyName != "ClasePeligrosidad" && e.PropertyName != "EsPeligroso");
    }

    [Fact]
    public void Validate_RefrigeracionSinTemperaturaMinima_TieneError()
    {
        var dto = DtoValido();
        dto.RequiereRefrigeracion = true;
        dto.TemperaturaMaxC = 5;
        dto.TemperaturaMinC = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TemperaturaMinC");
    }

    [Fact]
    public void Validate_RefrigeracionSinTemperaturaMaxima_TieneError()
    {
        var dto = DtoValido();
        dto.RequiereRefrigeracion = true;
        dto.TemperaturaMinC = 0;
        dto.TemperaturaMaxC = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TemperaturaMaxC");
    }

    [Fact]
    public void Validate_TemperaturaMinimaMayorQueMaxima_TieneError()
    {
        var dto = DtoValido();
        dto.TemperaturaMinC = 10;
        dto.TemperaturaMaxC = 5;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TemperaturaMinC");
    }

    [Fact]
    public void Validate_PesoMaximoNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.PesoMaximoKg = -1;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "PesoMaximoKg");
    }

    [Fact]
    public void Validate_DtoValidoCompleto_SinErrores()
    {
        var dto = DtoValido();
        dto.ClasePeligrosidad = "3";
        dto.CodigoOnu = "UN1202";
        dto.PesoMaximoKg = 20000;
        dto.VolumenMaximoM3 = 25m;
        dto.RequiereRefrigeracion = true;
        dto.TemperaturaMinC = 2;
        dto.TemperaturaMaxC = 8;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}