using Freiroute.BLL.Validators;
using Freiroute.DTO.Onboarding;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador del Paso 3 del onboarding: configuración operativa (HU-012 CA-04).
/// Valida moneda ISO 4217, prefijos, formato de fecha y modos de transporte.
/// </summary>
public class OnboardingPaso3ValidatorTests
{
    private readonly OnboardingPaso3Validator _validator = new();

    private OnboardingPaso3RequestDto DtoValido() => new()
    {
        Moneda = "USD",
        ZonaHoraria = "America/Managua",
        FormatoFecha = "DD/MM/YYYY",
        PrefijoEmbarque = "FR",
        PrefijoOrden = "ORD",
        ModosTransporteActivos = ["FTL", "LTL"]
    };

    // ── Moneda ──

    [Fact]
    public void Validate_MonedaVacia_TieneError()
    {
        var dto = DtoValido();
        dto.Moneda = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Moneda");
    }

    [Fact]
    public void Validate_MonedaDosLetras_TieneError()
    {
        // ISO 4217 requiere exactamente 3 caracteres.
        var dto = DtoValido();
        dto.Moneda = "NI";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Moneda");
    }

    [Fact]
    public void Validate_MonedaTresLetras_SinError()
    {
        var dto = DtoValido();
        dto.Moneda = "NIO";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "Moneda");
    }

    // ── Zona Horaria ──

    [Fact]
    public void Validate_ZonaHorariaVacia_TieneError()
    {
        var dto = DtoValido();
        dto.ZonaHoraria = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ZonaHoraria");
    }

    // ── Formato Fecha ──

    [Fact]
    public void Validate_FormatoFechaSlash_SoloFormatosPermitidos()
    {
        // YYYY/MM/DD no está permitido según spec Sprint 2.
        var dto = DtoValido();
        dto.FormatoFecha = "YYYY/MM/DD";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FormatoFecha");
    }

    [Fact]
    public void Validate_FormatoFechaGuion_SinError()
    {
        var dto = DtoValido();
        dto.FormatoFecha = "YYYY-MM-DD";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "FormatoFecha");
    }

    [Fact]
    public void Validate_FormatoFechaDDMMYYYY_SinError()
    {
        var dto = DtoValido();
        dto.FormatoFecha = "DD/MM/YYYY";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "FormatoFecha");
    }

    [Fact]
    public void Validate_FormatoFechaVacio_SinError()
    {
        // Cuando está vacío, la validación condicional no se aplica.
        var dto = DtoValido();
        dto.FormatoFecha = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "FormatoFecha");
    }

    // ── Prefijos ──

    [Fact]
    public void Validate_PrefijoEmbarqueMinusculas_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = "fr";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoEmbarque");
    }

    [Fact]
    public void Validate_PrefijoEmbarqueVacio_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoEmbarque");
    }

    [Fact]
    public void Validate_PrefijoEmbarqueValido_SinError()
    {
        var dto = DtoValido();
        dto.PrefijoEmbarque = "FRE1";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "PrefijoEmbarque");
    }

    [Fact]
    public void Validate_PrefijoOrdenMinusculas_TieneError()
    {
        var dto = DtoValido();
        dto.PrefijoOrden = "ord";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrefijoOrden");
    }

    // ── Modos de Transporte ──

    [Fact]
    public void Validate_ModosVacios_TieneError()
    {
        var dto = DtoValido();
        dto.ModosTransporteActivos = [];

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ModosTransporteActivos");
    }

    [Fact]
    public void Validate_ModoFTL_SinError()
    {
        var dto = DtoValido();
        dto.ModosTransporteActivos = ["FTL"];

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ModosTransporteActivos");
    }

    [Fact]
    public void Validate_ModoIntermodal_SinError()
    {
        // INTERMODAL es un valor válido.
        var dto = DtoValido();
        dto.ModosTransporteActivos = ["INTERMODAL"];

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ModosTransporteActivos");
    }

    [Fact]
    public void Validate_ModoMultimodal_TieneError()
    {
        // MULTIMODAL no está en la lista de modos válidos.
        var dto = DtoValido();
        dto.ModosTransporteActivos = ["MULTIMODAL"];

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ModosTransporteActivos");
    }

    // ── DTO válido completo ──

    [Fact]
    public void Validate_DtoValido_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
