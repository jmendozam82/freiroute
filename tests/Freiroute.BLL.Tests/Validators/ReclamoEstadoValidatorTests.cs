using FluentAssertions;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del ReclamoEstadoValidator (HU-032 CA-04/CA-12).
/// </summary>
public class ReclamoEstadoValidatorTests
{
    private readonly ReclamoEstadoValidator _validator = new();

    [Fact]
    public void Validate_CuandoEstadoNuevoYMotivoValidos_NoHayErrores()
    {
        // ── ARRANGE ──
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = EstadoReclamo.EnRevision,
            Motivo = "Se asignó ajustador para evaluar el daño"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CuandoMotivoVacio_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = EstadoReclamo.Aprobado,
            Motivo = "   "
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_CuandoEstadoNuevoVacio_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = "",
            Motivo = "Cambio de estado"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstadoNuevo");
    }

    [Fact]
    public void Validate_CuandoMotivoExcede1000Caracteres_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = EstadoReclamo.Rechazado,
            Motivo = new string('x', 1001)
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }
}