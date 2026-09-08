using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.Results;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using Xunit;

namespace Freiroute.BLL.Tests.Validators;

public class ConsolidarOrdenesValidatorTests
{
    private readonly ConsolidarOrdenesValidator _validator;

    public ConsolidarOrdenesValidatorTests()
    {
        _validator = new ConsolidarOrdenesValidator();
    }

    [Fact]
    public void Validate_CuandoDosOrdenesDistintas_EsValido()
    {
        var dto = new ConsolidarOrdenesRequestDto
        {
            OrdenIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CuandoUnaOrden_TieneError()
    {
        var dto = new ConsolidarOrdenesRequestDto
        {
            OrdenIds = new List<Guid> { Guid.NewGuid() }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrdenIds");
    }

    [Fact]
    public void Validate_CuandoListaVacia_TieneError()
    {
        var dto = new ConsolidarOrdenesRequestDto
        {
            OrdenIds = new List<Guid>()
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrdenIds");
    }

    [Fact]
    public void Validate_CuandoOrdenDuplicada_TieneError()
    {
        var id = Guid.NewGuid();
        var dto = new ConsolidarOrdenesRequestDto
        {
            OrdenIds = new List<Guid> { id, id }
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrdenIds" && e.ErrorMessage.Contains("misma orden"));
    }
}

