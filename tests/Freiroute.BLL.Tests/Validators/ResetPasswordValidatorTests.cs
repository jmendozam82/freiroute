using Freiroute.BLL.Validators;
using Freiroute.DTO.Auth;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de reset de contraseña (HU-007 CA-05).
/// La nueva contraseña debe cumplir: mín 8 car, 1 mayúscula, 1 número, 1 especial.
/// </summary>
public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Fact]
    public void Validate_SinMayuscula_TieneError()
    {
        // "password123!" — sin mayúscula.
        var result = _validator.Validate(new ResetPasswordRequestDto
        {
            Token = "token",
            NewPassword = "password123!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("mayúscula"));
    }

    [Fact]
    public void Validate_SinNumero_TieneError()
    {
        // "Password!abc" — sin número.
        var result = _validator.Validate(new ResetPasswordRequestDto
        {
            Token = "token",
            NewPassword = "Password!abc"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("número"));
    }

    [Fact]
    public void Validate_SinCaracterEspecial_TieneError()
    {
        // "Password1234" — sin carácter especial.
        var result = _validator.Validate(new ResetPasswordRequestDto
        {
            Token = "token",
            NewPassword = "Password1234"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("especial"));
    }

    [Fact]
    public void Validate_TokenVacio_TieneError()
    {
        var result = _validator.Validate(new ResetPasswordRequestDto
        {
            Token = string.Empty,
            NewPassword = "Password123!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void Validate_PasswordCompleto_SinErrores()
    {
        // Cumple todos los requisitos (mayúscula, minúscula, número, especial, ≥8).
        var result = _validator.Validate(new ResetPasswordRequestDto
        {
            Token = "token",
            NewPassword = "Password123!"
        });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
