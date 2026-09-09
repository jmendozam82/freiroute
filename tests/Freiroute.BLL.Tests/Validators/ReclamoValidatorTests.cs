using FluentAssertions;
using Freiroute.BLL.Validators;
using Freiroute.DTO.Reclamo;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del ReclamoValidator (HU-032 CA-01).
/// </summary>
public class ReclamoValidatorTests
{
    private readonly ReclamoValidator _validator = new();

    [Fact]
    public void Validate_CuandoTodosLosCamposValidos_NoHayErrores()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = "DANO",
            Descripcion = "Caja aplastada durante el trayecto",
            MontoReclamado = 2500.00m,
            ReferenciasEvidencia = new List<string> { "https://storage.supabase.co/evidencia/pod.jpg" }
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("DANO")]
    [InlineData("PERDIDA")]
    [InlineData("RETRASO")]
    [InlineData("OTRO")]
    public void Validate_CuandoTipoValido_NoHayError(string tipo)
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = tipo,
            Descripcion = "Incidencia registrada por el cliente"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CuandoOrdenIdVacio_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.Empty,
            Tipo = "DANO",
            Descripcion = "Incidencia"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrdenId");
    }

    [Fact]
    public void Validate_CuandoTipoInvalido_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = "ROBO_ARMADO",
            Descripcion = "Incidencia"
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tipo");
    }

    [Fact]
    public void Validate_CuandoDescripcionVacia_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = "DANO",
            Descripcion = ""
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }

    [Fact]
    public void Validate_CuandoMontoNegativo_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = "PERDIDA",
            Descripcion = "Mercancía extraviada",
            MontoReclamado = -1.00m
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MontoReclamado");
    }

    [Fact]
    public void Validate_CuandoMontoPositivo_NoHayErrorDeMonto()
    {
        // ── ARRANGE ──
        var dto = new ReclamoRequestDto
        {
            OrdenId = Guid.NewGuid(),
            Tipo = "RETRASO",
            Descripcion = "Llegada 3 días tarde",
            MontoReclamado = 0.01m
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.Errors.Should().NotContain(e => e.PropertyName == "MontoReclamado");
    }
}