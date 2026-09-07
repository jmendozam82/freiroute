using Freiroute.BLL.Validators;
using Freiroute.DTO.Ubicacion;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Validators;

/// <summary>
/// Tests del validador de ubicaciones (HU-015, ADR-014).
/// Valida identificación, tipo, país, horarios HH:mm, contacto y
/// coordenadas manuales (lat/lng siempre juntas, rangos válidos).
/// </summary>
public class UbicacionValidatorTests
{
    private readonly UbicacionValidator _validator = new();

    private UbicacionRequestDto DtoValido() => new()
    {
        Nombre = "Bodega Central Managua",
        Codigo = "BC-MGA",
        Tipo = "ALMACEN",
        Direccion = "Km 12.5 Carretera Sur",
        Pais = "Nicaragua",
        Departamento = "Managua",
        Ciudad = "Managua",
        ContactoNombre = "Juan Pérez",
        ContactoTelefono = "+505 8888-1234",
        ContactoEmail = "juan@transnic.com.ni",
        HorarioApertura = "08:00",
        HorarioCierre = "17:00",
        TiempoServicioMin = 30,
        Instrucciones = "Descargar en muelle 3"
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
    public void Validate_CodigoConCaracteresInvalidos_TieneError()
    {
        var dto = DtoValido();
        dto.Codigo = "BC MGA!!";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void Validate_TipoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.Tipo = "NAVE_ESPACIAL";

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tipo");
    }

    [Fact]
    public void Validate_PaisVacio_TieneError()
    {
        var dto = DtoValido();
        dto.Pais = string.Empty;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Pais");
    }

    [Fact]
    public void Validate_HorarioAperturaSinFormatoHHmm_TieneError()
    {
        var dto = DtoValido();
        dto.HorarioApertura = "9:00";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "HorarioApertura");
    }

    [Fact]
    public void Validate_EmailContactoInvalido_TieneError()
    {
        var dto = DtoValido();
        dto.ContactoEmail = "no-es-un-email";

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "ContactoEmail");
    }

    [Fact]
    public void Validate_LatitudFueraDeRango_TieneError()
    {
        var dto = DtoValido();
        dto.Latitud = -95;
        dto.Longitud = -86.2;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Latitud");
    }

    [Fact]
    public void Validate_LongitudFueraDeRango_TieneError()
    {
        var dto = DtoValido();
        dto.Latitud = 12.1;
        dto.Longitud = 200;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Longitud");
    }

    [Fact]
    public void Validate_SoloLatitudSinLongitud_TieneError()
    {
        var dto = DtoValido();
        dto.Latitud = 12.1;
        dto.Longitud = null;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Coordenadas");
    }

    [Fact]
    public void Validate_TiempoServicioExcede600_TieneError()
    {
        var dto = DtoValido();
        dto.TiempoServicioMin = 601;

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "TiempoServicioMin");
    }

    [Fact]
    public void Validate_DtoValidoConCoordenadasManuales_SinErrores()
    {
        var dto = DtoValido();
        dto.Latitud = 12.13;
        dto.Longitud = -86.26;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}