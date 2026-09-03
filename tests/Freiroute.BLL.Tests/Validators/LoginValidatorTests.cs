using Freiroute.BLL.Validators;
using Freiroute.DTO.Auth;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de login (HU-003 CA-03 parcial): email y contraseña.
/// </summary>
public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_EmailVacio_TieneError()
    {
        var result = _validator.Validate(new LoginRequestDto
        {
            Email = string.Empty,
            Password = "MiPassword123!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailFormatoInvalido_TieneError()
    {
        var result = _validator.Validate(new LoginRequestDto
        {
            Email = "no-es-un-email",
            Password = "MiPassword123!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_PasswordMenorA8_TieneError()
    {
        var result = _validator.Validate(new LoginRequestDto
        {
            Email = "juan@empresa.com",
            Password = "corto"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_DatosValidos_SinErrores()
    {
        var result = _validator.Validate(new LoginRequestDto
        {
            Email = "juan@empresa.com",
            Password = "MiPassword123!"
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
