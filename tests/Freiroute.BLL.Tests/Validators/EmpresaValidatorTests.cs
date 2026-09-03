using Freiroute.BLL.Validators;
using Freiroute.DTO.Empresa;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de registro de tenant (HU-001).
/// Plan: STARTER | PROFESSIONAL | ENTERPRISE. Colores en formato hex #RRGGBB.
/// </summary>
public class EmpresaValidatorTests
{
    private readonly EmpresaValidator _validator = new();

    private EmpresaRequestDto DtoValido() => new()
    {
        Nombre = "Trans Nicaragua S.A.",
        EmailAdmin = "admin@transnic.com",
        Pais = "Nicaragua",
        PlanSuscripcion = "STARTER",
        ColorPrimario = "#1A73E8",
        ColorSecundario = "#0B2545"
    };

    [Fact]
    public void Validate_NombreVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Nombre = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_EmailAdminInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.EmailAdmin = "no-es-email";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAdmin");
    }

    [Fact]
    public void Validate_PlanInvalido_TieneError()
    {
        // Valores válidos: STARTER, PROFESSIONAL, ENTERPRISE.
        var dto = DtoValido();
        dto.PlanSuscripcion = "GRATIS";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlanSuscripcion");
    }

    [Fact]
    public void Validate_ColorFormatoInvalido_TieneError()
    {
        // #GGG000 no es un hex válido (G no es hex).
        var dto = DtoValido();
        dto.ColorPrimario = "#GGG000";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColorPrimario");
    }

    [Fact]
    public void Validate_ColorFormatoValido_SinError()
    {
        // #1A73E8 es un hex válido (dígitos + mayúsculas A-F).
        var dto = DtoValido();
        dto.ColorPrimario = "#1A73E8";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ColorPrimario");
    }

    [Fact]
    public void Validate_DatosMinimosValidos_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
