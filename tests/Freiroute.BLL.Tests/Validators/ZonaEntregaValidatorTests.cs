using Freiroute.BLL.Validators;
using Freiroute.DTO.Zona;
using Freiroute.Utility.Constants;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de zonas de entrega (HU-016, ADR-018).
/// Valida identificación, color HEX, método de definición y GeoJSON
/// Polygon/MultiPolygon válido para el point-in-polygon de la BLL.
/// </summary>
public class ZonaEntregaValidatorTests
{
    private const string CuadradoManagua =
        """
        {"type":"Polygon","coordinates":[[
          [-86.4,12.0],[-86.0,12.0],[-86.0,12.4],[-86.4,12.4],[-86.4,12.0]
        ]]}
        """;

    private readonly ZonaEntregaValidator _validator = new();

    private ZonaRequestDto DtoPoligonoValido() => new()
    {
        Nombre = "Managua Centro",
        Codigo = "MGA-C",
        Descripcion = "Casco urbano de Managua",
        ColorHex = "#1A73E8",
        TipoDefinicion = TipoZonaDefinicion.Poligono,
        PoligonoGeoJson = CuadradoManagua
    };

    [Fact]
    public void Validate_NombreVacio_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.Nombre = string.Empty;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_CodigoConEspacio_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.Codigo = "MGA CENTRO";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_ColorHexInvalido_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.ColorHex = "azul";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "ColorHex");
    }

    [Fact]
    public void Validate_TipoDefinicionInvalido_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.TipoDefinicion = "HELIPUERTO";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TipoDefinicion");
    }

    [Fact]
    public void Validate_PoligonoSinGeoJSON_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.PoligonoGeoJson = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "PoligonoGeoJson");
    }

    [Fact]
    public void Validate_PoligonoConTypePoint_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.PoligonoGeoJson = """{"type":"Point","coordinates":[-86.2,12.15]}""";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "PoligonoGeoJson");
    }

    [Fact]
    public void Validate_PoligonoConGeoJSONInvalido_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.PoligonoGeoJson = "no-es-json";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "PoligonoGeoJson");
    }

    [Fact]
    public void Validate_CiudadesSinLista_TieneError()
    {
        var dto = DtoPoligonoValido();
        dto.TipoDefinicion = TipoZonaDefinicion.Ciudades;
        dto.PoligonoGeoJson = null;
        dto.Ciudades = [];

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Ciudades");
    }

    [Fact]
    public void Validate_CiudadesConLista_SinError()
    {
        var dto = DtoPoligonoValido();
        dto.TipoDefinicion = TipoZonaDefinicion.Ciudades;
        dto.PoligonoGeoJson = null;
        dto.Ciudades = ["Managua", "Masaya"];

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "Ciudades");
    }

    [Fact]
    public void Validate_DtoPoligonoValido_SinErrores()
    {
        var result = _validator.Validate(DtoPoligonoValido());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}