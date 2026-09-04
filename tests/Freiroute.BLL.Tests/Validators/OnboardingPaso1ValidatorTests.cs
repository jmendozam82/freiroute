using Freiroute.BLL.Validators;
using Freiroute.DTO.Onboarding;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador del Paso 1 del onboarding: datos de la empresa (HU-012 CA-02).
/// Valida nombre (obligatorio, máx 200), RUC/NIT (máx 50), dirección (máx 500),
/// teléfono (máx 50) e industria (máx 100).
/// </summary>
public class OnboardingPaso1ValidatorTests
{
    private readonly OnboardingPaso1Validator _validator = new();

    private OnboardingPaso1RequestDto DtoValido() => new()
    {
        Nombre = "Trans Nicaragua S.A.",
        RucNit = "J0310000000123",
        Direccion = "Km 12.5 Carretera Sur, Managua",
        Telefono = "+505 2222-3333",
        Industria = "Logística y Transporte"
    };

    // ── Nombre ──

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
    public void Validate_NombreExcede200_TieneError()
    {
        var dto = DtoValido();
        dto.Nombre = new string('N', 201);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    // ── Campos con MaxLength ──

    [Theory]
    [InlineData("RucNit", 51)]
    [InlineData("Direccion", 501)]
    [InlineData("Telefono", 51)]
    [InlineData("Industria", 101)]
    public void Validate_CampoExcedeLongitudMaxima_TieneError(string propiedad, int longitud)
    {
        var dto = DtoValido();
        var valor = new string('X', longitud);

        switch (propiedad)
        {
            case "RucNit":
                dto.RucNit = valor;
                break;
            case "Direccion":
                dto.Direccion = valor;
                break;
            case "Telefono":
                dto.Telefono = valor;
                break;
            case "Industria":
                dto.Industria = valor;
                break;
        }

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propiedad);
    }

    // ── Campos opcionales vacíos ──

    [Theory]
    [InlineData("RucNit")]
    [InlineData("Direccion")]
    [InlineData("Telefono")]
    [InlineData("Industria")]
    public void Validate_CampoOpcionalVacio_SinError(string propiedad)
    {
        var dto = DtoValido();

        switch (propiedad)
        {
            case "RucNit":
                dto.RucNit = string.Empty;
                break;
            case "Direccion":
                dto.Direccion = string.Empty;
                break;
            case "Telefono":
                dto.Telefono = string.Empty;
                break;
            case "Industria":
                dto.Industria = string.Empty;
                break;
        }

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == propiedad);
    }

    // ── DTO válido completo ──

    [Fact]
    public void Validate_DtoValido_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
