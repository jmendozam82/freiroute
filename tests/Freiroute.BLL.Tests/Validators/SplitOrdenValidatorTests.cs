using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.Results;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using Xunit;

namespace Freiroute.BLL.Tests.Validators;

public class SplitOrdenValidatorTests
{
    private readonly SplitOrdenValidator _validator;

    public SplitOrdenValidatorTests()
    {
        _validator = new SplitOrdenValidator();
    }

    [Fact]
    public void Validate_CuandoDosSplitsValidos_EsValido()
    {
        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 10, PesoKg = 100 },
                new SplitItemDto { Cantidad = 5, PesoKg = 50 }
            }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CuandoUnSoloSplit_TieneError()
    {
        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 10, PesoKg = 100 }
            }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Splits");
    }

    [Fact]
    public void Validate_CuandoOnceSplits_TieneError()
    {
        var splits = new List<SplitItemDto>();
        for (int i = 0; i < 11; i++)
        {
            splits.Add(new SplitItemDto { Cantidad = 1, PesoKg = 10 });
        }

        var dto = new SplitOrdenRequestDto { Splits = splits };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Splits");
    }

    [Fact]
    public void Validate_CuandoDiezSplits_EsValido()
    {
        var splits = new List<SplitItemDto>();
        for (int i = 0; i < 10; i++)
        {
            splits.Add(new SplitItemDto { Cantidad = 1, PesoKg = 10 });
        }

        var dto = new SplitOrdenRequestDto { Splits = splits };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CuandoSplitSinSplits_TieneError()
    {
        var dto = new SplitOrdenRequestDto { Splits = new List<SplitItemDto>() };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Splits");
    }

    [Fact]
    public void Validate_CuandoSplitCantidadCero_TieneError()
    {
        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 0, PesoKg = 100 },
                new SplitItemDto { Cantidad = 5, PesoKg = 50 }
            }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Cantidad"));
    }

    [Fact]
    public void Validate_CuandoSplitPesoCero_TieneError()
    {
        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 10, PesoKg = 0 },
                new SplitItemDto { Cantidad = 5, PesoKg = 50 }
            }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PesoKg"));
    }
}
