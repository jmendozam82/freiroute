using System.Net;
using System.Net.Http.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Utility.Exceptions;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del EmpresasController (HU-001).
/// El módulo de empresas es global del SaaS → SOLO SUPER_ADMIN (CA-07).
/// RequirePermission usa 'configuracion' + bypass total para SUPER_ADMIN.
/// </summary>
public class EmpresasControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public EmpresasControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private EmpresaResponseDto EmpresaDto() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Trans Nicaragua S.A.",
        EmailAdmin = "admin@transnic.com",
        Pais = "Nicaragua",
        PlanSuscripcion = "PROFESSIONAL",
        Estado = "ACTIVE",
        ColorPrimario = "#1A73E8",
        ColorSecundario = "#0B2545",
        PrefijoEmbarque = "TR",
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConTokenAdmin_Retorna403()
    {
        // Un ADMIN de tenant (sin permiso 'configuracion') no gestiona el SaaS → 403.
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.GetAsync("/api/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ConTokenSuperAdmin_Retorna200()
    {
        _factory.EmpresaService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<EmpresaResponseDto> { EmpresaDto(), EmpresaDto() });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.GetAsync("/api/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_DatosValidos_Retorna200()
    {
        // NOTA: el controller responde Ok() = 200 (no 201). Desviación documentada.
        _factory.EmpresaService
            .Setup(s => s.CreateAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(EmpresaDto());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PostAsJsonAsync("/api/empresas", new EmpresaRequestDto
        {
            Nombre = "Trans Nicaragua S.A.",
            EmailAdmin = "admin@transnic.com",
            Pais = "Nicaragua",
            PlanSuscripcion = "PROFESSIONAL"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_EmailDuplicado_Retorna409()
    {
        // CA-06: email_admin duplicado → ConflictException → 409.
        _factory.EmpresaService
            .Setup(s => s.CreateAsync(It.IsAny<EmpresaRequestDto>()))
            .ThrowsAsync(new ConflictException("Ya existe una empresa con ese email."));

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PostAsJsonAsync("/api/empresas", new EmpresaRequestDto
        {
            Nombre = "Trans Nicaragua S.A.",
            EmailAdmin = "admin@transnic.com",
            Pais = "Nicaragua",
            PlanSuscripcion = "STARTER"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_DatosInvalidos_Retorna400()
    {
        // Validación servidor → ValidationException → 400.
        _factory.EmpresaService
            .Setup(s => s.CreateAsync(It.IsAny<EmpresaRequestDto>()))
            .ThrowsAsync(new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Nombre", "Obligatorio") }));

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PostAsJsonAsync("/api/empresas", new EmpresaRequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactivate_Existente_Retorna200()
    {
        _factory.EmpresaService
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PatchAsync(
            $"/api/empresas/{Guid.NewGuid()}/deactivate",
            new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_NoExistente_Retorna404()
    {
        _factory.EmpresaService
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException(nameof(Freiroute.Entity.Empresa), Guid.NewGuid()));

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PatchAsync(
            $"/api/empresas/{Guid.NewGuid()}/deactivate",
            new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
