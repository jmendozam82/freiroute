using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Cliente;
using Freiroute.Utility.Pagination;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del ClientesController (HU-019).
/// Módulo de permisos 'clientes' (READ/CREATE/UPDATE). Cubre CRUD, contactos,
/// estado de crédito, exportación e importación CSV (ADR-017).
/// </summary>
public class ClientesControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ClientesControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["clientes:read", "clientes:create", "clientes:update"], "ADMIN");

    private static ClienteResponseDto Cliente() => new() { Id = Guid.NewGuid(), Nombre = "Transportes ABC" };

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/clientes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.ClienteService
            .Setup(s => s.GetAllAsync(
                JwtTestHelper.EmpresaTenant, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), 1, 20))
            .ReturnsAsync(new PagedResult<ClienteResponseDto>());

        var client = _factory.CrearClientConToken(TokenConEscritura); // clientes:read incluido

        var response = await client.GetAsync("/api/clientes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ConPermiso_Retorna201()
    {
        _factory.ClienteService
            .Setup(s => s.CreateAsync(It.IsAny<ClienteRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(Cliente());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/clientes",
            new ClienteRequestDto { Nombre = "Transportes ABC" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_SinPermisoClientes_Retorna403()
    {
        // TokenSoloLectura tiene permisos de 'configuracion' y 'usuarios', no de 'clientes'.
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/clientes",
            new ClienteRequestDto { Nombre = "Transportes ABC" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CambiarEstadoCredito_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.CambiarEstadoCreditoAsync(id, "BLOQUEADO", JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(Cliente());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsJsonAsync($"/api/clientes/{id}/estado-credito",
            new { EstadoCredito = "BLOQUEADO" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ClienteService.Verify(
            s => s.CambiarEstadoCreditoAsync(id, "BLOQUEADO", JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task AgregarContacto_ConPermisoUpdate_Retorna201()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.AgregarContactoAsync(id, It.IsAny<ContactoClienteRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ContactoClienteResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync($"/api/clientes/{id}/contactos",
            new ContactoClienteRequestDto { Nombre = "Ana Torres" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Exportar_ConPermisoRead_Retorna200ConCsv()
    {
        _factory.ClienteService
            .Setup(s => s.ExportarExcelAsync(JwtTestHelper.EmpresaTenant))
            .ReturnsAsync([0xEF, 0xBB, 0xBF, 0x4E]);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.GetAsync("/api/clientes/exportar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task Importar_SinArchivo_Retorna400()
    {
        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsync("/api/clientes/importar", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivateContacto_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        var contactoId = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.DeactivateContactoAsync(id, contactoId, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/clientes/{id}/contactos/{contactoId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(Cliente());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.GetAsync($"/api/clientes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<ClienteResponseDto>>();
        body!.Data!.Nombre.Should().Be("Transportes ABC");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((ClienteResponseDto?)null);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.GetAsync($"/api/clientes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.UpdateAsync(id, It.IsAny<ClienteRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ClienteResponseDto { Id = id, Nombre = "Transportes ABC S.A." });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/clientes/{id}",
            new ClienteRequestDto { Nombre = "Transportes ABC S.A." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ClienteService.Verify(
            s => s.UpdateAsync(id, It.Is<ClienteRequestDto>(d => d.Nombre == "Transportes ABC S.A."),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task Deactivate_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/clientes/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ClienteService.Verify(
            s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task UpdateContacto_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        var contactoId = Guid.NewGuid();
        _factory.ClienteService
            .Setup(s => s.UpdateContactoAsync(id, contactoId, It.IsAny<ContactoClienteRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ContactoClienteResponseDto { Id = contactoId, Nombre = "Ana Torres R." });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/clientes/{id}/contactos/{contactoId}",
            new ContactoClienteRequestDto { Nombre = "Ana Torres R." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ClienteService.Verify(
            s => s.UpdateContactoAsync(id, contactoId,
                It.Is<ContactoClienteRequestDto>(d => d.Nombre == "Ana Torres R."),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }
}