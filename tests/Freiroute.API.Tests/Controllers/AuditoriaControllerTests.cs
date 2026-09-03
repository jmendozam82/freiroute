using System.Net;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auditoria;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del AuditoriaController (HU-008).
/// RequirePermission sobre el módulo 'configuracion' (read). El log es inmutable:
/// solo GET paginado y export CSV — no existe GetById/Update/Delete (CA-06).
/// </summary>
public class AuditoriaControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AuditoriaControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private PagedResult<AuditoriaActivityResponseDto> Pagina()
    {
        var items = new List<AuditoriaActivityResponseDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmpresaId = Guid.NewGuid(),
                UsuarioId = Guid.NewGuid(),
                Modulo = "USUARIOS",
                Accion = AccionAuditoria.CREATE,
                EntidadTipo = "Usuario",
                IpAddress = "127.0.0.1",
                FechaCreacion = DateTime.UtcNow
            }
        };
        return new PagedResult<AuditoriaActivityResponseDto>
        {
            Items = items,
            TotalItems = items.Count,
            PageNumber = 1,
            PageSize = 20
        };
    }

    [Fact]
    public async Task GetAll_SinPermiso_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSinPermisos);

        var response = await client.GetAsync("/api/auditoria");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ConPermiso_Retorna200()
    {
        // TokenSoloLectura tiene 'configuracion:read' → accede al log.
        _factory.AuditoriaService
            .Setup(s => s.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Pagina());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/auditoria");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_FiltraPorEmpresa_Retorna200YEnvíaTenantAlBLL()
    {
        // El empresa_id del JWT SIEMPRE se pasa al servicio BLL (aislamiento multi-tenant).
        var empresaId = Guid.NewGuid();
        var token = JwtTestHelper.GenerateTestToken(Guid.NewGuid(), empresaId, new[] { "configuracion:read" }, "ADMIN");

        _factory.AuditoriaService
            .Setup(s => s.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Pagina());

        var client = _factory.CrearClientConToken(token);

        var response = await client.GetAsync("/api/auditoria");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // El BLL recibe exactamente el empresa_id del token, nunca otro tenant.
        _factory.AuditoriaService.Verify(
            s => s.GetPagedAsync(empresaId, null, null, null, null, 1, 20),
            Times.Once);
    }

    [Fact]
    public async Task ExportCsv_ConPermiso_Retorna200YRegistraAuditoriaDeExport()
    {
        _factory.AuditoriaService
            .Setup(s => s.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Pagina());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/auditoria/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        // CA-05: el acceso de exportación queda registrado con acción EXPORT.
        _factory.AuditoriaService.Verify(
            s => s.RegistrarAsync("auditoria", AccionAuditoria.EXPORT,
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportCsv_SinPermiso_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSinPermisos);

        var response = await client.GetAsync("/api/auditoria/export");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
