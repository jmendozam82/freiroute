using FluentValidation;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Geo;
using Freiroute.DTO.Zona;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio de zonas de entrega (HU-016, ADR-018).
/// Cubre point-in-polygon (ray casting), verificación por listas vía
/// reverse geocoding y la protección de borrado con tarifas activas (CA-06).
/// </summary>
public class ZonaEntregaServiceTests
{
    private const string CuadradoManagua =
        """
        {"type":"Polygon","coordinates":[[
          [-86.4,12.0],[-86.0,12.0],[-86.0,12.4],[-86.4,12.4],[-86.4,12.0]
        ]]}
        """;

    private readonly Mock<IZonaEntregaRepository> _zonaRepo;
    private readonly Mock<IUbicacionRepository> _ubicacionRepo;
    private readonly Mock<IGeocodingService> _geocoding;
    private readonly Mock<IValidator<ZonaRequestDto>> _validator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly ZonaEntregaService _service;

    public ZonaEntregaServiceTests()
    {
        _zonaRepo = new Mock<IZonaEntregaRepository>();
        _ubicacionRepo = new Mock<IUbicacionRepository>();
        _geocoding = new Mock<IGeocodingService>();
        _validator = new Mock<IValidator<ZonaRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ZonaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new ZonaEntregaService(
            _zonaRepo.Object, _ubicacionRepo.Object, _geocoding.Object,
            _validator.Object, _auditoria.Object,
            Mock.Of<ILogger<ZonaEntregaService>>());
    }

    private static ZonaEntrega ZonaPoligono(string id, string nombre) => new()
    {
        Id = Guid.NewGuid(),
        EmpresaId = Guid.NewGuid(),
        Nombre = nombre,
        Codigo = id,
        ColorHex = "#1A73E8",
        TipoDefinicion = TipoZonaDefinicion.Poligono,
        PoligonoGeoJson = CuadradoManagua,
        Ciudades = [],
        Departamentos = [],
        Paises = [],
        CodigosPostales = [],
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    private static ZonaEntrega ZonaCiudades(params string[] ciudades) => new()
    {
        Id = Guid.NewGuid(),
        EmpresaId = Guid.NewGuid(),
        Nombre = "Zona Managua",
        Codigo = "MGA",
        ColorHex = "#00D4FF",
        TipoDefinicion = TipoZonaDefinicion.Ciudades,
        PoligonoGeoJson = null,
        Ciudades = ciudades,
        Departamentos = [],
        Paises = [],
        CodigosPostales = [],
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    private void SetupUbicacionesZona(Guid zonaId)
    {
        _ubicacionRepo.Setup(r => r.GetByZonaAsync(zonaId, It.IsAny<Guid>()))
            .ReturnsAsync([]);
    }

    // ── VerificarPertenenciaAsync — POLIGONO (ADR-018) ─────────

    [Fact]
    public async Task VerificarPertenencia_PuntoDentroPoligono_RetornaZona()
    {
        var zona = ZonaPoligono("MGA-C", "Managua Centro");
        var empresaId = zona.EmpresaId;
        SetupUbicacionesZona(zona.Id);
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.15, -86.2, empresaId))
            .ReturnsAsync([zona]);
        _geocoding.Setup(g => g.ReverseGeocodeAsync(12.15, -86.2))
            .ReturnsAsync((string?)null); // Irrelevante para POLIGONO.

        var resultado = await _service.VerificarPertenenciaAsync(12.15, -86.2, empresaId);

        resultado.Should().ContainSingle(r => r.Nombre == "Managua Centro");
    }

    [Fact]
    public async Task VerificarPertenencia_PuntoFueraPoligono_NoRetornaZona()
    {
        var zona = ZonaPoligono("MGA-C", "Managua Centro");
        var empresaId = zona.EmpresaId;
        SetupUbicacionesZona(zona.Id);
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(13.5, -87.5, empresaId))
            .ReturnsAsync([zona]);
        _geocoding.Setup(g => g.ReverseGeocodeAsync(13.5, -87.5))
            .ReturnsAsync((string?)null);

        var resultado = await _service.VerificarPertenenciaAsync(13.5, -87.5, empresaId);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task VerificarPertenencia_GeoJSONInvalido_NoLanzaYNoRetorna()
    {
        var zona = ZonaPoligono("MGA-C", "Managua Centro");
        zona.PoligonoGeoJson = "{ json roto"; // Provoca JsonException al verificar.
        var empresaId = zona.EmpresaId;
        SetupUbicacionesZona(zona.Id);
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.15, -86.2, empresaId))
            .ReturnsAsync([zona]);
        _geocoding.Setup(g => g.ReverseGeocodeAsync(It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync((string?)null);

        var resultado = await _service.VerificarPertenenciaAsync(12.15, -86.2, empresaId);

        // Fail-soft: el GeoJSON inválido no puede disparar excepción.
        resultado.Should().BeEmpty();
    }

    // ── VerificarPertenenciaAsync — CIUDADES (reverse geocoding) ─

    [Fact]
    public async Task VerificarPertenencia_DireccionContieneCiudad_RetornaZona()
    {
        var zona = ZonaCiudades("Managua", "Masaya");
        var empresaId = zona.EmpresaId;
        SetupUbicacionesZona(zona.Id);
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.13, -86.26, empresaId))
            .ReturnsAsync([zona]);
        _geocoding.Setup(g => g.ReverseGeocodeAsync(12.13, -86.26))
            .ReturnsAsync("Barrio Bolonia, Managua, Nicaragua");

        var resultado = await _service.VerificarPertenenciaAsync(12.13, -86.26, empresaId);

        resultado.Should().ContainSingle(r => r.Nombre == "Zona Managua");
    }

    [Fact]
    public async Task VerificarPertenencia_DireccionSinCiudad_NoRetornaZona()
    {
        var zona = ZonaCiudades("Managua");
        var empresaId = zona.EmpresaId;
        SetupUbicacionesZona(zona.Id);
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(11.2, -85.8, empresaId))
            .ReturnsAsync([zona]);
        _geocoding.Setup(g => g.ReverseGeocodeAsync(11.2, -85.8))
            .ReturnsAsync("Rivas, Rivas, Nicaragua");

        var resultado = await _service.VerificarPertenenciaAsync(11.2, -85.8, empresaId);

        resultado.Should().BeEmpty();
    }

    // ── DeactivateAsync (CA-06) ────────────────────────────────

    [Fact]
    public async Task Deactivate_ZonaConTarifasActivas_LanzaBusinessException()
    {
        var zona = ZonaPoligono("MGA-C", "Managua Centro");
        var empresaId = zona.EmpresaId;
        _zonaRepo.Setup(r => r.GetByIdAsync(zona.Id, empresaId)).ReturnsAsync(zona);
        _zonaRepo.Setup(r => r.TieneTarifasActivasAsync(zona.Id, empresaId)).ReturnsAsync(true);

        var act = async () => await _service.DeactivateAsync(zona.Id, empresaId);

        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.Code.Should().Be("ZONA_TIENE_TARIFAS_ACTIVAS");
    }

    [Fact]
    public async Task Deactivate_ZonaSinTarifas_DesactivaYAudita()
    {
        var zona = ZonaPoligono("MGA-C", "Managua Centro");
        var empresaId = zona.EmpresaId;
        _zonaRepo.Setup(r => r.GetByIdAsync(zona.Id, empresaId)).ReturnsAsync(zona);
        _zonaRepo.Setup(r => r.TieneTarifasActivasAsync(zona.Id, empresaId)).ReturnsAsync(false);
        _zonaRepo.Setup(r => r.DeactivateAsync(zona.Id, empresaId)).ReturnsAsync(true);

        var resultado = await _service.DeactivateAsync(zona.Id, empresaId);

        resultado.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.DEACTIVATE, empresaId,
            null, "ZonaEntrega", zona.Id, null, null, null), Times.Once);
    }
}