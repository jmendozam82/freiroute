using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Unidad;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests unitarios del catálogo de tipos de embalaje (HU-018, Sprint 3).
/// Cubre CRUD con código único por empresa (PLT, CAJA, TAM...) y soft delete (ADR-005).
/// </summary>
public class TipoEmbalajeServiceTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid EmbalajeId = Guid.NewGuid();

    private readonly Mock<ITipoEmbalajeRepository> _repo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly TipoEmbalajeService _service;

    public TipoEmbalajeServiceTests()
    {
        _repo = new Mock<ITipoEmbalajeRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _service = new TipoEmbalajeService(
            _repo.Object,
            new TipoEmbalajeValidator(),
            _auditoria.Object,
            Mock.Of<ILogger<TipoEmbalajeService>>());
    }

    private static TipoEmbalajeRequestDto DtoValido() => new()
    {
        Nombre = "Tarima",
        Codigo = "plt",
        Descripcion = "Pallet estándar",
        CapacidadKg = 1200,
        CapacidadM3 = 2.5m,
        Apilable = true
    };

    private static TipoEmbalaje EmbalajeEntity(Guid id) => new()
    {
        Id = id,
        EmpresaId = EmpresaId,
        Nombre = "Tarima",
        Codigo = "PLT",
        CapacidadKg = 1200,
        Apilable = true,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAllAsync_RetornaEmbalajes()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId)).ReturnsAsync(new List<TipoEmbalaje> { EmbalajeEntity(EmbalajeId) });

        var result = await _service.GetAllAsync(EmpresaId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync((TipoEmbalaje?)null);

        var result = await _service.GetByIdAsync(EmbalajeId, EmpresaId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CuandoCodigoDuplicado_LanzaConflictException()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId))
            .ReturnsAsync(new List<TipoEmbalaje> { EmbalajeEntity(EmbalajeId) });

        var act = async () => await _service.CreateAsync(DtoValido(), EmpresaId);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Ya existe un embalaje con el código 'plt'*");
    }

    [Fact]
    public async Task CreateAsync_CuandoValido_UppercaseaCodigoYAudita()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId)).ReturnsAsync(new List<TipoEmbalaje>());
        _repo.Setup(r => r.CreateAsync(It.IsAny<TipoEmbalaje>())).ReturnsAsync(EmbalajeId);
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync(EmbalajeEntity(EmbalajeId));

        var result = await _service.CreateAsync(DtoValido(), EmpresaId);

        result.Codigo.Should().Be("PLT"); // Normalizado a mayúsculas.
        _auditoria.Verify(a => a.RegistrarAsync("tipos_embalaje", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoEmbalaje", EmbalajeId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoCodigoDuplicadoEnOtro_LanzaConflictException()
    {
        var otro = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync(EmbalajeEntity(EmbalajeId));
        _repo.Setup(r => r.GetAllAsync(EmpresaId)).ReturnsAsync(new List<TipoEmbalaje>
        {
            EmbalajeEntity(EmbalajeId),
            new() { Id = otro, EmpresaId = EmpresaId, Nombre = "Caja", Codigo = "PLT", Activo = true }
        });

        var act = async () => await _service.UpdateAsync(EmbalajeId, DtoValido(), EmpresaId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaYAudita()
    {
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync(EmbalajeEntity(EmbalajeId));
        _repo.Setup(r => r.GetAllAsync(EmpresaId)).ReturnsAsync(new List<TipoEmbalaje> { EmbalajeEntity(EmbalajeId) });
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TipoEmbalaje>())).ReturnsAsync(true);
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync(EmbalajeEntity(EmbalajeId));

        var result = await _service.UpdateAsync(EmbalajeId, DtoValido(), EmpresaId);

        result.Id.Should().Be(EmbalajeId);
        _auditoria.Verify(a => a.RegistrarAsync("tipos_embalaje", AccionAuditoria.UPDATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoEmbalaje", EmbalajeId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync((TipoEmbalaje?)null);

        var act = async () => await _service.DeactivateAsync(EmbalajeId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_DesactivaSoftDelete()
    {
        _repo.Setup(r => r.GetByIdAsync(EmbalajeId, EmpresaId)).ReturnsAsync(EmbalajeEntity(EmbalajeId));
        _repo.Setup(r => r.DeactivateAsync(EmbalajeId, EmpresaId)).ReturnsAsync(true);

        var ok = await _service.DeactivateAsync(EmbalajeId, EmpresaId);

        ok.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync("tipos_embalaje", AccionAuditoria.DEACTIVATE, EmpresaId,
            It.IsAny<Guid?>(), "TipoEmbalaje", EmbalajeId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }
}