using Freiroute.BLL.Validators;
using Freiroute.DTO.Configuracion;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de prefijos de numeración (HU-014 CA-05).
/// Valida que los prefijos de embarque, órden y carta porte no estén vacíos,
/// no excedan 10 caracteres y solo contengan letras, números, guiones y guion bajo.
/// </summary>
public class NumeracionValidatorTests
{
    private readonly NumeracionValidator _validator = new();

    private NumeracionRequestDto DtoValido() => new()
    {
        PrefijoEmbarque = "FR-001",
        PrefijoOrden = "ABC_1",
        PrefijoCartaPorte = "ORD"
    };

    // ── PrefijoEmbarque ──

    [Fact]
    public void Validate_PrefijoEmbarqueVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoEmbarque");
    }

    [Fact]
    public void Validate_PrefijoEmbarqueExcede10_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = new string('F', 11);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoEmbarque");
    }

    // ── PrefijoOrden ──

    [Fact]
    public void Validate_PrefijoOrdenVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoOrden = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoOrden");
    }

    [Fact]
    public void Validate_PrefijoOrdenExcede10_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoOrden = new string('O', 11);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoOrden");
    }

    // ── PrefijoCartaPorte ──

    [Fact]
    public void Validate_PrefijoCartaPorteVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoCartaPorte = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoCartaPorte");
    }

    [Fact]
    public void Validate_PrefijoCartaPorteExcede10_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoCartaPorte = new string('C', 11);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoCartaPorte");
    }

    // ── Caracteres no permitidos (Theory) ──

    [Theory]
    [InlineData("FR 001")]
    [InlineData("FR#001")]
    [InlineData("ORD/1")]
    [InlineData("PRE@F")]
    [InlineData("A B")]
    public void Validate_PrefijoConCaracteresInvalidos_TieneError(string prefijoInvalido)
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = prefijoInvalido;
        dto.PrefijoOrden = prefijoInvalido;
        dto.PrefijoCartaPorte = prefijoInvalido;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoEmbarque");
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoOrden");
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoCartaPorte");
    }

    // ── Caracteres válidos (Theory) ──

    [Theory]
    [InlineData("FR-001")]
    [InlineData("ABC_1")]
    [InlineData("ORD")]
    [InlineData("a-b_c")]
    [InlineData("X1Y2Z3")]
    public void Validate_PrefijoSoloAlfanumericoGuiones_SinError(string prefijoValido)
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = prefijoValido;
        dto.PrefijoOrden = prefijoValido;
        dto.PrefijoCartaPorte = prefijoValido;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "PrefijoEmbarque");
        result.Errors.Should().NotContain(e => e.PropertyName == "PrefijoOrden");
        result.Errors.Should().NotContain(e => e.PropertyName == "PrefijoCartaPorte");
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
