using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Mercancia;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests unitarios del catálogo de tipos de mercancía (HU-017, Sprint 3).
/// Cubre CRUD con validaciones HAZMAT (CA-02/CA-04), filtro de peligrosas
/// (CA-07) e importación CSV fail-soft (CA-06, ADR-017).
/// </summary>
public class TipoMercanciaServiceTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid TipoId = Guid.NewGuid();

    private readonly Mock<ITipoMercanciaRepository> _repo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly TipoMercanciaService _service;

    public TipoMercanciaServiceTests()
    {
        _repo = new Mock<ITipoMercanciaRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new TipoMercanciaService(
            _repo.Object,
            new TipoMercanciaValidator(),
            _auditoria.Object,
            Mock.Of<ILogger<TipoMercanciaService>>());
    }

    private static TipoMercanciaRequestDto DtoValido() => new()
    {
        Nombre = "Gasolina",
        Codigo = "GAS",
        Categoria = "Combustibles",
        PesoMaximoKg = 1000,
        VolumenMaximoM3 = 2
    };

    private static TipoMercanciaRequestDto DtoHazmat() => new()
    {
        Nombre = "Acetileno",
        Codigo = "ACE",
        Categoria = "Gases",
        ClasePeligrosidad = "2.1",
        CodigoOnu = "UN1001",
        EsPeligroso = true
    };

    private static TipoMercancia TipoEntity(Guid id) => new()
    {
        Id = id,
        EmpresaId = EmpresaId,
        Nombre = "Gasolina",
        Codigo = "GAS",
        EsPeligroso = false,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAllAsync_CuandoSoloPeligrosas_FiltraYRetorna()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId, true))
            .ReturnsAsync(new List<TipoMercancia> { TipoEntity(TipoId) });

        var result = await _service.GetAllAsync(EmpresaId, soloPeligrosas: true);

        result.Should().HaveCount(1);
        _repo.Verify(r => r.GetAllAsync(EmpresaId, true), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _repo.Setup(r => r.GetByIdAsync(TipoId, EmpresaId)).ReturnsAsync((TipoMercancia?)null);

        var result = await _service.GetByIdAsync(TipoId, EmpresaId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CuandoHazmat_RetornaEsHazmatTrue()
    {
        var tipoHazmat = new TipoMercancia
        {
            Id = TipoId,
            EmpresaId = EmpresaId,
            Nombre = "Acetileno",
            ClasePeligrosidad = "2.1",
            EsPeligroso = true,
            Activo = true
        };
        _repo.Setup(r => r.CreateAsync(It.IsAny<TipoMercancia>())).ReturnsAsync(TipoId);
        _repo.Setup(r => r.GetByIdAsync(TipoId, EmpresaId)).ReturnsAsync(tipoHazmat);

        var result = await _service.CreateAsync(DtoHazmat(), EmpresaId);

        result.Id.Should().Be(TipoId);
        result.EsHazmat.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync("tipos_mercancia", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoMercancia", TipoId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoValidatorInvalido_LanzaValidationException()
    {
        // Nombre vacío → el validator HAZMAT rechaza el dto.
        var dto = new TipoMercanciaRequestDto { Nombre = string.Empty };

        var act = async () => await _service.CreateAsync(dto, EmpresaId);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(TipoId, EmpresaId)).ReturnsAsync((TipoMercancia?)null);

        var act = async () => await _service.UpdateAsync(TipoId, DtoValido(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaYAudita()
    {
        _repo.Setup(r => r.GetByIdAsync(TipoId, EmpresaId)).ReturnsAsync(TipoEntity(TipoId));
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TipoMercancia>())).ReturnsAsync(true);

        var result = await _service.UpdateAsync(TipoId, DtoValido(), EmpresaId);

        result.Id.Should().Be(TipoId);
        _auditoria.Verify(a => a.RegistrarAsync("tipos_mercancia", AccionAuditoria.UPDATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoMercancia", TipoId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_DesactivaSoftDeleteYAudita()
    {
        _repo.Setup(r => r.GetByIdAsync(TipoId, EmpresaId)).ReturnsAsync(TipoEntity(TipoId));
        _repo.Setup(r => r.DeactivateAsync(TipoId, EmpresaId)).ReturnsAsync(true);

        var ok = await _service.DeactivateAsync(TipoId, EmpresaId);

        ok.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync("tipos_mercancia", AccionAuditoria.DEACTIVATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoMercancia", TipoId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ImportarCsvAsync_FilasInvalidas_FailSoftYAudita()
    {
        // Nota: "9.9" SÍ es una clase ONU válida (regex ^[1-9](\.[0-9])?$),
        // por eso las filas inválidas usan clase "X" o nombre vacío — y el
        // proceso continúa sin lanzar (ADR-017 fail-soft).
        var csv = "Nombre;Código;Descripción;Categoría\n" +
                  "Gasolina;GAS;;Combustibles\n" +
                  "\n" +
                  ";;;\n" +
                  "Clase inválida;X;;Categoría;X;;;\n";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var importados = await _service.ImportarCsvAsync(stream, EmpresaId);

        // Filas inválidas (clase peligrosidad "X" rechazada, nombre vacío) → omitidas.
        // La fila válida "Gasolina" se importa → fail-soft no bloquea las demás.
        importados.Should().Be(1);
        _auditoria.Verify(a => a.RegistrarAsync("tipos_mercancia", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoMercancia", null, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ImportarCsvAsync_CuandoFilaValida_Importa()
    {
        // La clase de peligrosidad normaliza EsPeligroso=true; fila válida → importa.
        var csv = "Nombre;Código;Descripción;Categoría;ClasePeligrosidad\n" +
                  "Acetileno;ACE;;Gases;2.1\n";
        _repo.Setup(r => r.CreateAsync(It.IsAny<TipoMercancia>())).ReturnsAsync(Guid.NewGuid());

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var importados = await _service.ImportarCsvAsync(stream, EmpresaId);

        importados.Should().Be(1);
        _repo.Verify(r => r.CreateAsync(It.Is<TipoMercancia>(t => t.EsPeligroso && t.ClasePeligrosidad == "2.1")), Times.Once);
    }
}