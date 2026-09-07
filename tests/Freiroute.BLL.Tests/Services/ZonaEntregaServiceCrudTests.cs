using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Zona;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del CRUD de zonas de entrega (HU-016 CA-01): GetAll, GetById,
/// Create y Update con validación y auditoría. Complementa
/// ZonaEntregaServicePertenenciaTests (CA-04/CA-05/CA-06).
/// </summary>
public class ZonaEntregaServiceCrudTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid ZonaId = Guid.NewGuid();

    private readonly Mock<IZonaEntregaRepository> _zonaRepo;
    private readonly Mock<IUbicacionRepository> _ubicacionRepo;
    private readonly Mock<IGeocodingService> _geocoding;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly Mock<IValidator<ZonaRequestDto>> _validator;
    private readonly ZonaEntregaService _service;

    public ZonaEntregaServiceCrudTests()
    {
        _zonaRepo = new Mock<IZonaEntregaRepository>();
        _ubicacionRepo = new Mock<IUbicacionRepository>();
        _geocoding = new Mock<IGeocodingService>();
        _auditoria = new Mock<IAuditoriaService>();
        _validator = new Mock<IValidator<ZonaRequestDto>>();
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ZonaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
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
            _validator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<ZonaEntregaService>>());
    }

    private static ZonaEntrega Zona() => new()
    {
        Id = ZonaId,
        EmpresaId = EmpresaId,
        Nombre = "Zona Norte",
        Codigo = "ZN",
        TipoDefinicion = TipoZonaDefinicion.Ciudades,
        Ciudades = new[] { "Estelí" },
        Activo = true
    };

    private static ZonaRequestDto Dto() => new()
    {
        Nombre = "Zona Norte",
        Codigo = "ZN",
        TipoDefinicion = TipoZonaDefinicion.Ciudades.ToString(),
        Ciudades = new[] { "Estelí" }
    };

    [Fact]
    public async Task GetAllAsync_CuandoHayZonas_RetornaListaMapeada()
    {
        _zonaRepo.Setup(r => r.GetAllAsync(EmpresaId))
            .ReturnsAsync(new List<ZonaEntrega> { Zona() });

        var result = await _service.GetAllAsync(EmpresaId);

        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Zona Norte");
        result.First().TotalUbicaciones.Should().Be(0);
        _zonaRepo.Verify(r => r.GetAllAsync(EmpresaId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync((ZonaEntrega?)null);

        var result = await _service.GetByIdAsync(ZonaId, EmpresaId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaZonaMapeada()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync(Zona());

        var result = await _service.GetByIdAsync(ZonaId, EmpresaId);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Zona Norte");
        result.Codigo.Should().Be("ZN");
    }

    [Fact]
    public async Task CreateAsync_DtoValido_CreaAuditaYRetornaZona()
    {
        _zonaRepo.Setup(r => r.CreateAsync(It.IsAny<ZonaEntrega>()))
            .ReturnsAsync(ZonaId);
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync(Zona());

        var result = await _service.CreateAsync(Dto(), EmpresaId);

        result.Nombre.Should().Be("Zona Norte");
        _zonaRepo.Verify(r => r.CreateAsync(It.Is<ZonaEntrega>(z => z.EmpresaId == EmpresaId && z.Nombre == "Zona Norte")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.CREATE, EmpresaId, null,
            "ZonaEntrega", ZonaId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DtoInvalido_LanzaValidationException()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ZonaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre es obligatorio") }));

        var act = async () => await _service.CreateAsync(Dto(), EmpresaId);

        await act.Should().ThrowAsync<ValidationException>();
        _zonaRepo.Verify(r => r.CreateAsync(It.IsAny<ZonaEntrega>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync((ZonaEntrega?)null);

        var act = async () => await _service.UpdateAsync(ZonaId, Dto(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DtoValido_ActualizaAuditaYRetornaZona()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync(Zona());
        _zonaRepo.Setup(r => r.UpdateAsync(It.IsAny<ZonaEntrega>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateAsync(ZonaId, Dto(), EmpresaId);

        result.Nombre.Should().Be("Zona Norte");
        _zonaRepo.Verify(r => r.UpdateAsync(It.Is<ZonaEntrega>(z => z.Id == ZonaId && z.Nombre == "Zona Norte")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.UPDATE, EmpresaId, null,
            "ZonaEntrega", ZonaId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoRepoRetornaFalse_LanzaNotFoundException()
    {
        _zonaRepo.Setup(r => r.GetByIdAsync(ZonaId, EmpresaId))
            .ReturnsAsync(Zona());
        _zonaRepo.Setup(r => r.UpdateAsync(It.IsAny<ZonaEntrega>()))
            .ReturnsAsync(false);

        var act = async () => await _service.UpdateAsync(ZonaId, Dto(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}