using Freiroute.BLL.Validators;
using Freiroute.DTO.Cliente;
using Freiroute.Utility.Constants;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de clientes (shippers) y sus contactos (HU-019).
/// Valida identificación fiscal, configuración de crédito y contacto
/// principal único (CA-08).
/// </summary>
public class ClienteValidatorTests
{
    private readonly ClienteValidator _validator = new();

    private ClienteRequestDto DtoValido() => new()
    {
        Nombre = "Comercial Rivera S.A.",
        NombreComercial = "Rivera",
        RucNit = "J0310000000123",
        TipoDocumento = "RUC",
        TipoCliente = TipoCliente.Regular,
        Pais = "Nicaragua",
        Ciudad = "Managua",
        Moneda = "USD",
        CreditoDias = 30,
        LimiteCredito = 50000m,
        Contactos =
        [
            new ContactoClienteRequestDto
            {
                Nombre = "Ana Rivera",
                Cargo = "Gerente General",
                Rol = RolContacto.Gerencia,
                Email = "ana@rivera.com.ni",
                EsPrincipal = true
            }
        ]
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
    public void Validate_RucNitVacio_TieneError()
    {
        var dto = DtoValido();
        dto.RucNit = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "RucNit");
    }

    [Fact]
    public void Validate_RucNitConCaracteresInvalidos_TieneError()
    {
        var dto = DtoValido();
        dto.RucNit = "J031 0000 0001 23";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "RucNit");
    }

    [Fact]
    public void Validate_TipoDocumentoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.TipoDocumento = "CARNET";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TipoDocumento");
    }

    [Fact]
    public void Validate_TipoClienteInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.TipoCliente = "SOSPECHOSO";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TipoCliente");
    }

    [Fact]
    public void Validate_MonedaDosLetras_TieneError()
    {
        var dto = DtoValido();
        dto.Moneda = "NI";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Moneda");
    }

    [Fact]
    public void Validate_LimiteCreditoNegativo_TieneError()
    {
        var dto = DtoValido();
        dto.LimiteCredito = -100m;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "LimiteCredito");
    }

    [Fact]
    public void Validate_DosContactosPrincipales_TieneError()
    {
        var dto = DtoValido();
        dto.Contactos.Add(new ContactoClienteRequestDto
        {
            Nombre = "Luis Rivera",
            Rol = RolContacto.Logistica,
            EsPrincipal = true
        });

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Contactos");
    }

    [Fact]
    public void Validate_ContactoHijoSinNombre_TieneErrorEnIndice()
    {
        var dto = DtoValido();
        dto.Contactos[0].Nombre = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Contactos[0].Nombre");
    }

    [Fact]
    public void Validate_ContactoRolInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.Contactos[0].Rol = "CHOFER";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Contactos[0].Rol");
    }

    [Fact]
    public void Validate_DtoValidoCompleto_SinErrores()
    {
        var result = _validator.Validate(DtoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}