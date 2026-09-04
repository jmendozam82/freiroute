using Freiroute.BLL.Validators;
using Freiroute.DTO.Configuracion;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de configuración general del tenant (HU-014).
/// Valida nombre, colores HEX, URL, moneda, zona horaria, formato de fecha y email.
/// </summary>
public class ConfiguracionValidatorTests
{
    private readonly ConfiguracionValidator _validator = new();

    private ConfiguracionRequestDto DtoValido() => new()
    {
        Nombre = "Trans Nicaragua S.A.",
        RucNit = "J0310000000123",
        Direccion = "Km 12.5 Carretera Sur, Managua",
        Telefono = "+505 2222-3333",
        Industria = "Logística y Transporte",
        SitioWeb = "https://transnic.com.ni",
        ColorPrimario = "#1A73E8",
        ColorSecundario = "#0B2545",
        Moneda = "USD",
        ZonaHoraria = "America/Managua",
        FormatoFecha = "DD/MM/YYYY",
        EmailRemitente = "notificaciones@transnic.com.ni",
        NombreRemitente = "Trans Nicaragua TMS"
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
    public void Validate_NombreExcede200_TieneError()
    {
        var dto = DtoValido();
        dto.Nombre = new string('N', 201);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    // ── Campos de texto con MaxLength ──

    [Fact]
    public void Validate_RucNitExcede50_TieneError()
    {
        var dto = DtoValido();
        dto.RucNit = new string('R', 51);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RucNit");
    }

    [Fact]
    public void Validate_DireccionExcede500_TieneError()
    {
        var dto = DtoValido();
        dto.Direccion = new string('D', 501);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Direccion");
    }

    [Fact]
    public void Validate_TelefonoExcede50_TieneError()
    {
        var dto = DtoValido();
        dto.Telefono = new string('T', 51);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Telefono");
    }

    [Fact]
    public void Validate_IndustriaExcede100_TieneError()
    {
        var dto = DtoValido();
        dto.Industria = new string('I', 101);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Industria");
    }

    // ── Sitio Web ──

    [Fact]
    public void Validate_SitioWebInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.SitioWeb = "no-es-una-url";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SitioWeb");
    }

    [Fact]
    public void Validate_SitioWebHttp_SinError()
    {
        var dto = DtoValido();
        dto.SitioWeb = "http://transnic.com.ni";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "SitioWeb");
    }

    [Fact]
    public void Validate_SitioWebHttps_SinError()
    {
        var dto = DtoValido();
        dto.SitioWeb = "https://transnic.com.ni";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "SitioWeb");
    }

    [Fact]
    public void Validate_SitioWebVacio_SinError()
    {
        // Campo opcional: cuando está vacío la validación no se aplica.
        var dto = DtoValido();
        dto.SitioWeb = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "SitioWeb");
    }

    // ── Colores ──

    [Fact]
    public void Validate_ColorPrimarioInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.ColorPrimario = "#GGG000";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColorPrimario");
    }

    [Fact]
    public void Validate_ColorPrimarioValido_SinError()
    {
        var dto = DtoValido();
        dto.ColorPrimario = "#1A73E8";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ColorPrimario");
    }

    [Fact]
    public void Validate_ColorSecundarioInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.ColorSecundario = "rojo";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColorSecundario");
    }

    [Fact]
    public void Validate_ColorPrimarioVacio_SinError()
    {
        // Campo opcional.
        var dto = DtoValido();
        dto.ColorPrimario = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ColorPrimario");
    }

    // ── Moneda y Zona Horaria ──

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
        var dto = DtoValido();
        dto.Moneda = "NI";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Moneda");
    }

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
    public void Validate_FormatoFechaInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.FormatoFecha = "YYYY-MM-DD";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FormatoFecha");
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
    public void Validate_FormatoFechaMMDDYYYY_SinError()
    {
        var dto = DtoValido();
        dto.FormatoFecha = "MM/DD/YYYY";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "FormatoFecha");
    }

    [Fact]
    public void Validate_FormatoFechaYYYYMMDD_SinError()
    {
        var dto = DtoValido();
        dto.FormatoFecha = "YYYY/MM/DD";

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "FormatoFecha");
    }

    // ── Email Remitente ──

    [Fact]
    public void Validate_EmailRemitenteInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.EmailRemitente = "no-es-email";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailRemitente");
    }

    [Fact]
    public void Validate_EmailRemitenteVacio_SinError()
    {
        // Campo opcional.
        var dto = DtoValido();
        dto.EmailRemitente = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "EmailRemitente");
    }

    [Fact]
    public void Validate_NombreRemitenteExcede200_TieneError()
    {
        var dto = DtoValido();
        dto.NombreRemitente = new string('R', 201);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NombreRemitente");
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
