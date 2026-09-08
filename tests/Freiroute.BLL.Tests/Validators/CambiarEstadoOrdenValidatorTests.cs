using System;
using FluentAssertions;
using FluentValidation.Results;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using Xunit;

namespace Freiroute.BLL.Tests.Validators;

public class CambiarEstadoOrdenValidatorTests
{
    private readonly CambiarEstadoOrdenValidator _validator;

    public CambiarEstadoOrdenValidatorTests()
    {
        _validator = new CambiarEstadoOrdenValidator();
    }

    [Fact]
    public void Validate_EstadoReconocidoSinMotivo_EsValido()
    {
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = "CONFIRMED" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EstadoReconocidoConMotivo_EsValido()
    {
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = "CONFIRMED", Motivo = "Todo OK" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EstadoVacio_TieneError()
    {
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstadoNuevo");
    }

    [Theory]
    [InlineData("PENDIENTE")]    // no existe en el sistema
    [InlineData("BORRADOR")]     // label en español, no el código
    [InlineData("draft")]        // case-sensitive
    public void Validate_EstadoNoReconocido_TieneError(string estado)
    {
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = estado };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstadoNuevo");
    }

    [Fact]
    public void Validate_MotivoExcede500Chars_TieneError()
    {
        var dto = new CambiarEstadoOrdenRequestDto 
        { 
            EstadoNuevo = "CONFIRMED", 
            Motivo = new string('a', 501) 
        };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_MotivoDe500Chars_EsValido()
    {
        var dto = new CambiarEstadoOrdenRequestDto 
        { 
            EstadoNuevo = "CONFIRMED", 
            Motivo = new string('a', 500) 
        };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MotivoNull_EsValido()
    {
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = "CONFIRMED", Motivo = null };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
}
