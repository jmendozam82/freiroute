using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Ubicacion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del catálogo de ubicaciones (HU-015): paginación (CA-07),
/// payload del mapa (CA-04), soft delete (ADR-005) y validación de
/// latitud/longitud en el ajuste manual (CA-05).
/// </summary>
public class UbicacionServiceCrudTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid UbicacionId = Guid.NewGuid();

    private readonly Mock<IUbicacionRepository> _ubicacionRepo;
    private readonly Mock<IGeocodingService> _geocoding;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IValidator<UbicacionRequestDto>> _validator;
    private readonly UbicacionService _service;

    public UbicacionServiceCrudTests()
    {
        _ubicacionRepo = new Mock<IUbicacionRepository>();
        _geocoding = new Mock<IGeocodingService>();
        _auditoria = new Mock<IAuditoriaService>();
        _validator = new Mock<IValidator<UbicacionRequestDto>>();
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<UbicacionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new UbicacionService(
            _ubicacionRepo.Object,
            _geocoding.Object,
            _validator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<UbicacionService>>());
    }

    private static Ubicacion Ubicacion() => new()
    {
        Id = UbicacionId,
        EmpresaId = EmpresaId,
        Nombre = "Bodega Managua",
        Codigo = "BMG",
        Tipo = TipoUbicacion.Almacen,
        Direccion = "Km 7 Carretera Norte",
        Pais = "Nicaragua",
        Ciudad = "Managua",
        Latitud = 12.15,
        Longitud = -86.25,
        Georeferenciada = true,
        Activo = true
    };

    [Fact]
    public async Task GetAllAsync_ConFiltros_RetornaPaginaCorrecta()
    {
        var todas = new List<Ubicacion> { Ubicacion(), Ubicacion(), Ubicacion() };
        _ubicacionRepo.Setup(r => r.GetAllAsync(EmpresaId, TipoUbicacion.Almacen, "Bodega"))
            .ReturnsAsync(todas);

        var result = await _service.GetAllAsync(EmpresaId, TipoUbicacion.Almacen, "Bodega", 1, 2);

        result.TotalItems.Should().Be(3);
        result.Items.Should().HaveCount(2); // pageSize = 2
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);

        var pagina2 = await _service.GetAllAsync(EmpresaId, TipoUbicacion.Almacen, "Bodega", 2, 2);
        pagina2.Items.Should().HaveCount(1); // 3 - 2 = 1 restante
    }

    [Fact]
    public async Task GetAllAsync_PageSizeInvalido_UsaDefault20()
    {
        _ubicacionRepo.Setup(r => r.GetAllAsync(EmpresaId, null, null))
            .ReturnsAsync(new List<Ubicacion> { Ubicacion() });

        var result = await _service.GetAllAsync(EmpresaId, null, null, 1, 0);

        result.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetParaMapaAsync_RetornaPayloadMinimoSoloGeoreferenciadas()
    {
        _ubicacionRepo.Setup(r => r.GetGeoreferenciadasAsync(EmpresaId))
            .ReturnsAsync(new List<Ubicacion> { Ubicacion() });

        var result = await _service.GetParaMapaAsync(EmpresaId);

        result.Should().HaveCount(1);
        var item = result.First();
        item.Id.Should().Be(UbicacionId);
        item.Nombre.Should().Be("Bodega Managua");
        item.Tipo.Should().Be(TipoUbicacion.Almacen);
        item.Latitud.Should().Be(12.15);
        item.Longitud.Should().Be(-86.25);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_RetornaTrueYAudita()
    {
        _ubicacionRepo.Setup(r => r.DeactivateAsync(UbicacionId, EmpresaId))
            .ReturnsAsync(true);

        var result = await _service.DeactivateAsync(UbicacionId, EmpresaId);

        result.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync(
            "ubicaciones", AccionAuditoria.DEACTIVATE, EmpresaId, null,
            "Ubicacion", UbicacionId, null, null, null), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoRepoRetornaFalse_LanzaNotFoundException()
    {
        _ubicacionRepo.Setup(r => r.DeactivateAsync(UbicacionId, EmpresaId))
            .ReturnsAsync(false);

        var act = async () => await _service.DeactivateAsync(UbicacionId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActualizarCoordenadasAsync_LatitudFueraDeRango_LanzaBusinessException()
    {
        var act = async () => await _service.ActualizarCoordenadasAsync(
            UbicacionId, new ActualizarCoordenadasDto { Latitud = 95, Longitud = -86.25 }, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*latitud*");
    }

    [Fact]
    public async Task ActualizarCoordenadasAsync_LongitudFueraDeRango_LanzaBusinessException()
    {
        var act = async () => await _service.ActualizarCoordenadasAsync(
            UbicacionId, new ActualizarCoordenadasDto { Latitud = 12.15, Longitud = -181 }, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*longitud*");
    }
}