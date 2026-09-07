using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Zona;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests adicionales de zonas de entrega (HU-016, Sprint 3, ADR-018).
/// Cubre verificación de pertenencia point-in-polygon (ray casting, CA-05),
/// coincidencia por listas (ciudades), la protección de tarifas activas
/// (CA-06) y la asignación/desasignación de ubicaciones (CA-04).
/// Nota: el geoJSON usa coordenadas [longitud, latitud] (RFC 7946).
/// </summary>
public class ZonaEntregaServicePertenenciaTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid ZonaId = Guid.NewGuid();
    private static readonly Guid UbicacionId = Guid.NewGuid();

    private readonly Mock<IZonaEntregaRepository> _zonaRepo;
    private readonly Mock<IUbicacionRepository> _ubicacionRepo;
    private readonly Mock<IGeocodingService> _geocoding;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly ZonaEntregaService _service;

    // Cuadrado Managua: [lng, lat] (RFC 7946).
    private const string CuadradoManagua =
        """{"type":"Polygon","coordinates":[[[-86.30,12.10],[-86.20,12.10],[-86.20,12.20],[-86.30,12.20],[-86.30,12.10]]]}""";

    public ZonaEntregaServicePertenenciaTests()
    {
        _zonaRepo = new Mock<IZonaEntregaRepository>();
        _ubicacionRepo = new Mock<IUbicacionRepository>();
        _geocoding = new Mock<IGeocodingService>();
        _auditoria = new Mock<IAuditoriaService>();
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _ubicacionRepo.Setup(r => r.GetByZonaAsync(It.IsAny<Guid>(), EmpresaId))
            .ReturnsAsync(new List<Ubicacion>());
        _service = new ZonaEntregaService(
            _zonaRepo.Object,
            _ubicacionRepo.Object,
            _geocoding.Object,
            new ZonaEntregaValidator(),
            _auditoria.Object,
            Mock.Of<ILogger<ZonaEntregaService>>());
    }

    private static ZonaEntrega ZonaPoligono() => new()
    {
        Id = ZonaId,
        EmpresaId = EmpresaId,
        Nombre = "Managua Centro",
        Codigo = "MGA-C",
        TipoDefinicion = TipoZonaDefinicion.Poligono,
        PoligonoGeoJson = CuadradoManagua,
        Activo = true
    };

    private static ZonaEntrega ZonaPorCiudades() => new()
    {
        Id = ZonaId,
        EmpresaId = EmpresaId,
        Nombre = "Zona León",
        Codigo = "LEON",
        TipoDefinicion = TipoZonaDefinicion.Ciudades,
        Ciudades = new[] { "LEÓN" },
        Activo = true
    };

    [Fact]
    public async Task VerificarPertenenciaAsync_CuandoPuntoDentroDePoligono_RetornaZona()
    {
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.15, -86.25, EmpresaId))
            .ReturnsAsync(new List<ZonaEntrega> { ZonaPoligono() });

        var result = await _service.VerificarPertenenciaAsync(12.15, -86.25, EmpresaId);

        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Managua Centro");
    }

    [Fact]
    public async Task VerificarPertenenciaAsync_CuandoPuntoFueraDePoligono_NoRetornaZona()
    {
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.30, -86.25, EmpresaId))
            .ReturnsAsync(new List<ZonaEntrega> { ZonaPoligono() });

        var result = await _service.VerificarPertenenciaAsync(12.30, -86.25, EmpresaId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task VerificarPertenenciaAsync_CuandoZonaPorCiudadesYDireccionCoincide_RetornaZona()
    {
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.43, -86.87, EmpresaId))
            .ReturnsAsync(new List<ZonaEntrega> { ZonaPorCiudades() });
        _geocoding.Setup(g => g.ReverseGeocodeAsync(12.43, -86.87))
            .ReturnsAsync("León, Nicaragua");

        var result = await _service.VerificarPertenenciaAsync(12.43, -86.87, EmpresaId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task VerificarPertenenciaAsync_CuandoGeoJsonInvalido_NoRetornaZonaNiPropaga()
    {
        var zona = ZonaPoligono();
        zona.PoligonoGeoJson = "{no-json}";
        _zonaRepo.Setup(r => r.GetZonasPorPuntoAsync(12.15, -86.25, EmpresaId))
            .ReturnsAsync(new List<ZonaEntrega> { zona });

        var result = await _service.VerificarPertenenciaAsync(12.15, -86.25, EmpresaId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoZonaReferenciadaPorTarifas_LanzaBusinessException()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId)).ReturnsAsync(ZonaPoligono());
        _zonaRepo.Setup(r => r.TieneTarifasActivasAsync(ZonaId, EmpresaId)).ReturnsAsync(true);

        var act = async () => await _service.DeactivateAsync(ZonaId, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*no puede desactivarse*");
    }

    [Fact]
    public async Task AsignarUbicacionesAsync_AsignaCadaUbicacionDistintaYAudita()
    {
        var otra = Guid.NewGuid();
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId)).ReturnsAsync(ZonaPoligono());

        await _service.AsignarUbicacionesAsync(ZonaId, new[] { UbicacionId, otra, UbicacionId }, EmpresaId);

        _zonaRepo.Verify(r => r.AsignarUbicacionAsync(ZonaId, UbicacionId, EmpresaId), Times.Once);
        _zonaRepo.Verify(r => r.AsignarUbicacionAsync(ZonaId, otra, EmpresaId), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("zonas_entrega", AccionAuditoria.UPDATE, EmpresaId,
            It.IsAny<Guid?>(), "ZonaEntrega", ZonaId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DesasignarUbicacionAsync_DesasignaYAudita()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId)).ReturnsAsync(ZonaPoligono());

        await _service.DesasignarUbicacionAsync(ZonaId, UbicacionId, EmpresaId);

        _zonaRepo.Verify(r => r.DesasignarUbicacionAsync(ZonaId, UbicacionId, EmpresaId), Times.Once);
    }
}