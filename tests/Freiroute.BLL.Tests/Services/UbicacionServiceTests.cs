using System.Text;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Geo;
using Freiroute.DTO.Ubicacion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio de ubicaciones (HU-015, ADR-014).
/// Cubre geocodificación automática fail-soft (CA-03), coordenadas
/// manuales (CA-05) e importación masiva CSV (CA-06, ADR-017).
/// </summary>
public class UbicacionServiceTests
{
    private readonly Mock<IUbicacionRepository> _repo;
    private readonly Mock<IGeocodingService> _geocoding;
    private readonly Mock<IValidator<UbicacionRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly UbicacionService _service;

    public UbicacionServiceTests()
    {
        _repo = new Mock<IUbicacionRepository>();
        _geocoding = new Mock<IGeocodingService>();
        _validator = new Mock<IValidator<UbicacionRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UbicacionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new UbicacionService(
            _repo.Object, _geocoding.Object, _validator.Object,
            _auditoria.Object, Mock.Of<ILogger<UbicacionService>>());
    }

    private static Ubicacion Ubicacion(
        Guid id,
        double? latitud = null,
        double? longitud = null,
        string? direccion = "Km 12.5 Carretera Sur, Managua") => new()
    {
        Id = id,
        EmpresaId = Guid.NewGuid(),
        Nombre = "Bodega Central",
        Codigo = "BC-MGA",
        Tipo = TipoUbicacion.Almacen,
        Direccion = direccion,
        Pais = "Nicaragua",
        Ciudad = "Managua",
        Latitud = latitud,
        Longitud = longitud,
        Georeferenciada = latitud.HasValue && longitud.HasValue,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    private UbicacionRequestDto DtoConDireccion() => new()
    {
        Nombre = "Bodega Central",
        Codigo = "BC-MGA",
        Tipo = TipoUbicacion.Almacen,
        Direccion = "Km 12.5 Carretera Sur",
        Pais = "Nicaragua",
        Departamento = "Managua",
        Ciudad = "Managua"
    };

    private void SetupGetById(Guid id, Guid empresaId, Ubicacion ubicacion)
    {
        _repo.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(ubicacion);
    }

    // ── CreateAsync — geocodificación automática (CA-02/CA-03) ──

    [Fact]
    public async Task CreateAsync_GeocodeExitoso_GuardaConCoordenadas()
    {
        var empresaId = Guid.NewGuid();
        var idCreado = Guid.NewGuid();
        _geocoding.Setup(g => g.GeocodeAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new GeocodingResultDto
            {
                Latitud = 12.15,
                Longitud = -86.26,
                DireccionNormalizada = "Km 12.5 Carretera Sur, Managua, Nicaragua",
                Confianza = 0.9,
                Proveedor = "nominatim"
            });
        _repo.Setup(r => r.CreateAsync(It.IsAny<Ubicacion>())).ReturnsAsync(idCreado);
        SetupGetById(idCreado, empresaId, Ubicacion(idCreado, 12.15, -86.26));

        var resultado = await _service.CreateAsync(DtoConDireccion(), empresaId);

        resultado.Georeferenciada.Should().BeTrue();
        _repo.Verify(r => r.CreateAsync(It.Is<Ubicacion>(u =>
            u.Latitud == 12.15 && u.Longitud == -86.26 &&
            u.DireccionNormalizada == "Km 12.5 Carretera Sur, Managua, Nicaragua" &&
            u.Georeferenciada)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GeocodeNulo_GuardaSinCoordenadasFailSoft()
    {
        var empresaId = Guid.NewGuid();
        var idCreado = Guid.NewGuid();
        _geocoding.Setup(g => g.GeocodeAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((GeocodingResultDto?)null); // FAIL-SOFT: el proveedor no responde.
        _repo.Setup(r => r.CreateAsync(It.IsAny<Ubicacion>())).ReturnsAsync(idCreado);
        SetupGetById(idCreado, empresaId, Ubicacion(idCreado));

        var resultado = await _service.CreateAsync(DtoConDireccion(), empresaId);

        // CA-03: la ubicación se guarda SIN coordenadas pero la respuesta no falla.
        resultado.Georeferenciada.Should().BeFalse();
        _repo.Verify(r => r.CreateAsync(It.Is<Ubicacion>(u =>
            !u.Latitud.HasValue && !u.Longitud.HasValue && !u.Georeferenciada)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ConCoordenadasManuales_NoGeocodifica()
    {
        var empresaId = Guid.NewGuid();
        var idCreado = Guid.NewGuid();
        var dto = DtoConDireccion();
        dto.Latitud = 12.13;
        dto.Longitud = -86.26;
        _repo.Setup(r => r.CreateAsync(It.IsAny<Ubicacion>())).ReturnsAsync(idCreado);
        SetupGetById(idCreado, empresaId, Ubicacion(idCreado, 12.13, -86.26));

        var resultado = await _service.CreateAsync(dto, empresaId);

        resultado.Georeferenciada.Should().BeTrue();
        _geocoding.Verify(g => g.GeocodeAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    // ── UpdateAsync — re-geocodificación al cambiar dirección ───

    [Fact]
    public async Task UpdateAsync_CambioDireccionYGeocodeNulo_LimpiaCoordenadas()
    {
        var empresaId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existente = Ubicacion(id, 12.15, -86.26);
        // Lectura inicial (con coordenadas) y re-lectura post-update (sin ellas).
        _repo.SetupSequence(r => r.GetByIdAsync(id, empresaId))
            .ReturnsAsync(existente)
            .ReturnsAsync(Ubicacion(id));
        _geocoding.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((GeocodingResultDto?)null);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Ubicacion>())).ReturnsAsync(true);

        var dto = DtoConDireccion();
        dto.Direccion = "Nueva dirección, Otro lugar";

        var resultado = await _service.UpdateAsync(id, dto, empresaId);

        resultado.Georeferenciada.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(It.Is<Ubicacion>(u =>
            !u.Latitud.HasValue && !u.Longitud.HasValue && !u.Georeferenciada)), Times.Once);
    }

    // ── ActualizarCoordenadasAsync — ajuste manual (CA-05) ─────

    [Fact]
    public async Task ActualizarCoordenadas_LatitudInvalida_LanzaBusinessException()
    {
        var act = async () => await _service.ActualizarCoordenadasAsync(
            Guid.NewGuid(), new ActualizarCoordenadasDto { Latitud = 95, Longitud = -86 }, Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Code.Should().Be("UBICACION_LATITUD_INVALIDA");
    }

    [Fact]
    public async Task ActualizarCoordenadas_LongitudInvalida_LanzaBusinessException()
    {
        var act = async () => await _service.ActualizarCoordenadasAsync(
            Guid.NewGuid(), new ActualizarCoordenadasDto { Latitud = 12, Longitud = -190 }, Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Code.Should().Be("UBICACION_LONGITUD_INVALIDA");
    }

    // ── ImportarCsvAsync (CA-06, ADR-017) ───────────────────────

    [Fact]
    public async Task ImportarCsv_FilasValidas_ImportaTodas()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.CreateAsync(It.IsAny<Ubicacion>()))
            .ReturnsAsync(Guid.NewGuid());
        var csv = new MemoryStream(Encoding.UTF8.GetBytes(
            "Nombre;Código;Tipo;Dirección;País;Departamento;Ciudad;CódigoPostal;ContactoNombre;ContactoTeléfono;ContactoEmail;Instrucciones\r\n" +
            "Bodega Norte;BN-01;ALMACEN;Km 8 Carretera Norte;Nicaragua;Managua;Managua;;Juan;8888-0001;juan@norte.com;Muelle 1\r\n" +
            "Puerto Corinto;PCT-01;PUERTO;Puerto Corinto;Nicaragua;Chinandega;Corinto;;;operaciones@corinto.com;\r\n"));

        var importados = await _service.ImportarCsvAsync(csv, empresaId);

        importados.Should().Be(2);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Ubicacion>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ImportarCsv_FilaConTipoInvalido_OmiteYSigue()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.CreateAsync(It.IsAny<Ubicacion>()))
            .ReturnsAsync(Guid.NewGuid());
        // El validador rechaza específicamente la fila "Roto" (código de zona inválido).
        _validator
            .Setup(v => v.ValidateAsync(
                It.Is<UbicacionRequestDto>(d => d.Nombre == "Roto"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure("Codigo", "El código solo admite letras, números, guiones y guiones bajos")
            ]));
        var csv = new MemoryStream(Encoding.UTF8.GetBytes(
            "Nombre;Código;Tipo;Dirección;País;Departamento;Ciudad;CódigoPostal;ContactoNombre;ContactoTeléfono;ContactoEmail;Instrucciones\r\n" +
            "Bodega Norte;BN-01;ALMACEN;Km 8 Carretera Norte;Nicaragua;Managua;Managua;;Juan;8888-0001;juan@norte.com;Muelle 1\r\n" +
            "Roto;RO 99;ALMACEN;Sin dirección;Nicaragua;Managua;;;  ;;;\r\n"));

        var importados = await _service.ImportarCsvAsync(csv, empresaId);

        importados.Should().Be(1);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Ubicacion>()), Times.Exactly(1));
    }

    [Fact]
    public async Task ImportarCsv_ConSoloCabecera_ImportaCero()
    {
        var empresaId = Guid.NewGuid();
        var csv = new MemoryStream(Encoding.UTF8.GetBytes(
            "Nombre;Código;Tipo;Dirección;País;Departamento;Ciudad;CódigoPostal;ContactoNombre;ContactoTeléfono;ContactoEmail;Instrucciones\r\n"));

        var importados = await _service.ImportarCsvAsync(csv, empresaId);

        importados.Should().Be(0);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Ubicacion>()), Times.Never);
    }
}