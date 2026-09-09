using FluentAssertions;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del RechazoEntregaValidator (HU-030 CA-01).
/// </summary>
public class RechazoEntregaValidatorTests
{
    private readonly RechazoEntregaValidator _validator = new();

    [Theory]
    [InlineData("CLIENTE_AUSENTE")]
    [InlineData("DIRECCION_INCORRECTA")]
    [InlineData("MERCANCIA_DANADA")]
    [InlineData("RECHAZO_CLIENTE")]
    [InlineData("OTRO")]
    public void Validate_CuandoMotivoValido_NoHayErrores(string motivo)
    {
        // ── ARRANGE ──
        var dto = new RechazoEntregaRequestDto
        {
            Motivo = motivo,
            Descripcion = "Detalle de la incidencia en la entrega"
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
        var dto = new RechazoEntregaRequestDto { Motivo = "" };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_CuandoMotivoInvalido_TieneError()
    {
        // ── ARRANGE ──
        var dto = new RechazoEntregaRequestDto { Motivo = "NO_SE_ENCONTRO" };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_CuandoDescripcionExcede2000Caracteres_TieneError()
    {
        // ── ARRANGE ──
        var dto = new RechazoEntregaRequestDto
        {
            Motivo = "OTRO",
            Descripcion = new string('a', 2001)
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }
}