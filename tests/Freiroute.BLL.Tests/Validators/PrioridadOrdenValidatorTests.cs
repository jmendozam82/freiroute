using FluentAssertions;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del PrioridadOrdenValidator (HU-029 CA-09).
/// </summary>
public class PrioridadOrdenValidatorTests
{
    private readonly PrioridadOrdenValidator _validator = new();

    [Theory]
    [InlineData("CRITICO")]
    [InlineData("ALTO")]
    [InlineData("NORMAL")]
    [InlineData("BAJO")]
    public void Validate_CuandoPrioridadValida_NoHayErrores(string prioridad)
    {
        // ── ARRANGE ──
        var dto = new PrioridadRequestDto
        {
            Prioridad = prioridad,
            Motivo = "Cliente requiere atención inmediata"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CuandoPrioridadInvalida_TieneError()
    {
        // ── ARRANGE ──
        var dto = new PrioridadRequestDto { Prioridad = "URGENTE" };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prioridad");
    }

    [Fact]
    public void Validate_CuandoPrioridadVacia_TieneError()
    {
        // ── ARRANGE ──
        var dto = new PrioridadRequestDto { Prioridad = "" };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prioridad");
    }

    [Fact]
    public void Validate_CuandoMotivoExcede500Caracteres_TieneError()
    {
        // ── ARRANGE ──
        var dto = new PrioridadRequestDto
        {
            Prioridad = OrdenPrioridad.Alto,
            Motivo = new string('m', 501)
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }
}