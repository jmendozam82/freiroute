using FluentAssertions;
using Freiroute.BLL.Services;
using Freiroute.DTO.Empresa;
using Xunit;

namespace Freiroute.BLL.Tests.EmpresaTests;

/// <summary>
/// Tests unitarios para EmpresaValidator - reglas de validacion del DTO de creacion de tenant (HU-001).
/// Cubre: campo nombre obligatorio/minimo 3 caracteres, plan valido, y combinacion valida completa.
/// Patrón AAA con FluentAssertions para verificacion de resultados de validacion.
/// </summary>
public class EmpresaValidatorTests
{
    [Fact]
    public void Validate_CuandoDtoValido_NoHayErrores()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "Transportes del Pacifico SA",
            Slug = "transportes-pacifico",
            Plan = "professional"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue("porque el DTO cumple todas las reglas de validacion");
        resultado.Errors.Should().BeEmpty("no debe haber errores cuando todo es valido");
    }

    [Fact]
    public void Validate_CuandoNombreVacio_TieneError()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "",
            Slug = "",
            Plan = "starter"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse("porque el nombre vacio viola la regla NotEmpty");
        var errors = resultado.Errors.Where(e => e.PropertyName == "Nombre").ToList();
        errors.Should().NotBeEmpty("porque el nombre vacio viola reglas de validacion");
        errors.Should().Contain(e => e.ErrorMessage == "El nombre es obligatorio");
    }

    [Fact]
    public void Validate_CuandoNombreNulo_TieneError()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = null!,
            Slug = "",
            Plan = "starter"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        var errors = resultado.Errors.Where(e => e.PropertyName == "Nombre").ToList();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_CuandoNombreCorto_MuyCorto_TieneError()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "AB",
            Slug = "",
            Plan = "starter"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        var errors = resultado.Errors.Where(e => e.PropertyName == "Nombre").ToList();
        errors.Should().ContainSingle();
        errors[0].ErrorMessage.Should().Be("Debe tener al menos 3 caracteres");
    }

    [Theory]
    [InlineData("starter")]
    [InlineData("professional")]
    [InlineData("enterprise")]
    public void Validate_CuandoPlanValido_NoHayError(string plan)
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "Empresa Valida",
            Slug = "empresa-valida",
            Plan = plan
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue($"porque '{plan}' es un plan valido permitido");
        resultado.Errors.Should().NotContain(e => e.PropertyName == "Plan");
    }

    [Theory]
    [InlineData("premium")]
    [InlineData("free")]
    [InlineData("PROFESSIONAL")]
    [InlineData("")]
    public void Validate_CuandoPlanInvalido_TieneError(string plan)
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "Empresa Valida",
            Slug = "empresa-valida",
            Plan = plan
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse($"porque '{plan}' no es un plan valido");
        var errors = resultado.Errors.Where(e => e.PropertyName == "Plan").ToList();
        errors.Should().ContainSingle();
        errors[0].ErrorMessage.Should().Be("El plan debe ser starter, professional o enterprise");
    }

    [Fact]
    public void Validate_CuandoTodosLosCamposInvalidos_TodosLosErroresReportados()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "AB",
            Slug = "",
            Plan = "premium"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCount(2, "porque hay dos campos invalidos: Nombre (muy corto) y Plan (invalido)");
        resultado.Errors.Should().Contain(e => e.PropertyName == "Nombre");
        resultado.Errors.Should().Contain(e => e.PropertyName == "Plan");
    }

    [Fact]
    public void Validate_CuandoNombreConTresChars_ExactamenteLimiteMinimo_Pasa()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "ABC",
            Slug = "",
            Plan = "starter"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue("porque 'ABC' cumple exactamente el minimo de 3 caracteres");
    }

    [Fact]
    public void Validate_SlugNoEsValidado_EsOpcional()
    {
        // Arrange
        var validator = new EmpresaValidator();
        var dto = new EmpresaRequestDto
        {
            Nombre = "Sin Slug Validado",
            Slug = string.Empty,
            Plan = "professional"
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue("porque el slug es opcional y no tiene reglas de validacion en el validator");
        resultado.Errors.Should().NotContain(e => e.PropertyName == "Slug");
    }
}
