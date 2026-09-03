using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using Freiroute.DTO.Auditoria;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests unitarios del servicio transversal de auditoría (HU-008).
/// Este servicio NUNCA debe propagar excepciones: si el registro falla, se
/// loguea y la operación de negocio continúa (defensa en dos capas).
/// </summary>
public class AuditoriaServiceTests
{
    private readonly Mock<IAuditoriaRepository> _repository;
    private readonly Mock<ILogger<AuditoriaService>> _logger;
    private readonly AuditoriaService _service;

    public AuditoriaServiceTests()
    {
        _repository = new Mock<IAuditoriaRepository>(MockBehavior.Strict);
        _logger = new Mock<ILogger<AuditoriaService>>();
        _service = new AuditoriaService(_repository.Object, _logger.Object);
    }

    [Fact]
    public async Task RegistrarAsync_CuandoRepositorioFalla_NoPropagaExcepcion()
    {
        // Arrange
        _repository
            .Setup(r => r.RegistrarAsync(It.IsAny<AuditoriaActividad>()))
            .ThrowsAsync(new InvalidOperationException("Fallo de BD"));

        // Act
        var act = async () => await _service.RegistrarAsync(
            "auth", AccionAuditoria.LOGIN, Guid.NewGuid(), null,
            nameof(Usuario), null, null);

        // Assert — la auditoría nunca tumba la operación de negocio.
        await act.Should().NotThrowAsync();

        // El fallo quedó logueado como error.
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_CuandoDatosCompletos_LlamaRepositorioUnaVez()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();

        _repository
            .Setup(r => r.RegistrarAsync(It.IsAny<AuditoriaActividad>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegistrarAsync(
            "empresas", AccionAuditoria.CREATE, empresaId, usuarioId,
            nameof(Empresa), entidadId,
            new { nombre = "Trans Nicaragua S.A." },
            "127.0.0.1", "Mozilla/5.0");

        // Assert
        _repository.Verify(
            r => r.RegistrarAsync(
                It.Is<AuditoriaActividad>(a =>
                    a.EmpresaId == empresaId &&
                    a.UsuarioId == usuarioId &&
                    a.Modulo == "empresas" &&
                    a.Accion == AccionAuditoria.CREATE &&
                    a.EntidadTipo == nameof(Empresa) &&
                    a.EntidadId == entidadId &&
                    a.IpAddress == "127.0.0.1" &&
                    a.UserAgent == "Mozilla/5.0" &&
                    a.Detalles != null &&
                    a.FechaCreacion != default)),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_CuandoUsuarioIdNulo_RegistraSinUsuario()
    {
        // Arrange
        _repository
            .Setup(r => r.RegistrarAsync(It.IsAny<AuditoriaActividad>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegistrarAsync(
            "auth", AccionAuditoria.LOGIN_FAILED, Guid.Empty, null,
            nameof(Usuario), null, new { email = "nadie@empresa.com" });

        // Assert
        _repository.Verify(
            r => r.RegistrarAsync(
                It.Is<AuditoriaActividad>(a =>
                    a.UsuarioId == null &&
                    a.EmpresaId == Guid.Empty &&
                    a.Accion == AccionAuditoria.LOGIN_FAILED &&
                    a.EntidadId == null)),
            Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_CuandoSolicita_FiltraPorEmpresa()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        var repos = new PagedResult<AuditoriaActividad>
        {
            Items = new List<AuditoriaActividad>
            {
                new() { Id = Guid.NewGuid(), Modulo = "auth", Accion = "LOGIN", FechaCreacion = DateTime.UtcNow }
            },
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 20
        };

        _repository
            .Setup(r => r.GetPagedAsync(
                empresaId, "auth", null, null, null, 1, 20))
            .ReturnsAsync(repos);

        // Act
        var result = await _service.GetPagedAsync(
            empresaId, "auth", null, null, null, 1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.Items.First().Modulo.Should().Be("auth");
    }
}
