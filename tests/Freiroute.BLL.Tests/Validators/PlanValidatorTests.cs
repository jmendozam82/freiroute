using Freiroute.BLL.Validators;
using Freiroute.DTO.Plan;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de planes de suscripción (HU-010).
/// Valida nombre, código (solo mayúsculas, números, guion bajo), límites y precios.
/// </summary>
public class PlanValidatorTests
{
    private readonly PlanValidator _validator = new();

    private PlanRequestDto DtoValido() => new()
    {
        Nombre = "Plan Professional",
        Codigo = "PROFESSIONAL",
        Descripcion = "Plan intermedio para empresas en crecimiento",
        LimiteUsuarios = 25,
        LimiteEmbarquesMes = 500,
        LimiteStorageGb = 10,
        PrecioMensual = 99.99m,
        PrecioAnual = 999.99m,
        Moneda = "USD",
        ModulosDisponibles = ["ordenes", "embarques", "carriers"]
    };

    // ── Nombre ──

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
    public void Validate_NombreExcede100_TieneError()
    {
        var dto = DtoValido();
        dto.Nombre = new string('A', 101);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    // ── Código ──

    [Fact]
    public void Validate_CodigoVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoExcede50_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = new string('A', 51);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoConMinusculas_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = "professional";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoConGuion_TieneError()
    {
        // Guion medio no es permitido, solo guion bajo.
        var dto = DtoValido();
        dto.Codigo = "PRO-FESSIONAL";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_CodigoGuionBajoMayusculas_SinError()
    {
        var dto = DtoValido();
        dto.Codigo = "PLAN_PRO";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "Codigo");
    }

    // ── Descripción ──

    [Fact]
    public void Validate_DescripcionExcede500_TieneError()
    {
        var dto = DtoValido();
        dto.Descripcion = new string('D', 501);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }

    // ── Límites ──

    [Fact]
    public void Validate_LimiteUsuariosCero_TieneError()
    {
        var dto = DtoValido();
        dto.LimiteUsuarios = 0;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LimiteUsuarios");
    }

    [Fact]
    public void Validate_LimiteUsuariosMenosUno_SinError()
    {
        var dto = DtoValido();
        dto.LimiteUsuarios = -1;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "LimiteUsuarios");
    }

    [Fact]
    public void Validate_LimiteEmbarquesMesCero_TieneError()
    {
        var dto = DtoValido();
        dto.LimiteEmbarquesMes = 0;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LimiteEmbarquesMes");
    }

    [Fact]
    public void Validate_LimiteEmbarquesMenosUno_SinError()
    {
        var dto = DtoValido();
        dto.LimiteEmbarquesMes = -1;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "LimiteEmbarquesMes");
    }

    [Fact]
    public void Validate_LimiteStorageNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.LimiteStorageGb = -1;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LimiteStorageGb");
    }

    // ── Precios ──

    [Fact]
    public void Validate_PrecioMensualNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.PrecioMensual = -10;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrecioMensual");
    }

    [Fact]
    public void Validate_PrecioAnualNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.PrecioAnual = -1;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrecioAnual");
    }

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

    // ── Módulos ──

    [Fact]
    public void Validate_ModulosVacios_TieneError()
    {
        var dto = DtoValido();
        dto.ModulosDisponibles = [];

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ModulosDisponibles");
    }

    [Fact]
    public void Validate_ModuloInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.ModulosDisponibles = ["ordenes", "modulo_fantasma"];

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("ModulosDisponibles"));
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
