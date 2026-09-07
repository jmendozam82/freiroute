using System.Net;
using System.Net.Http.Json;
using System.Text;
using Freiroute.DTO.Mercancia;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del TiposMercanciaController (HU-017).
/// Módulo 'configuracion' con filtro soloPeligrosas (CA-07), validación
/// HAZMAT y soft delete (ADR-005).
/// </summary>
public class TiposMercanciaControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public TiposMercanciaControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/tipos-mercancia");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConFiltroPeligrosas_Retorna200()
    {
        _factory.TipoMercanciaService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, true))
            .ReturnsAsync(new List<TipoMercanciaResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/tipos-mercancia?soloPeligrosas=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.TipoMercanciaService.Verify(
            s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, true), Times.Once);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.TipoMercanciaService
            .Setup(s => s.CreateAsync(It.IsAny<TipoMercanciaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoMercanciaResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/tipos-mercancia",
            new TipoMercanciaRequestDto { Nombre = "Combustible Diesel" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/tipos-mercancia",
            new TipoMercanciaRequestDto { Nombre = "Combustible Diesel" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Importar_ConPermisoCreate_Retorna200ConConteo()
    {
        _factory.TipoMercanciaService
            .Setup(s => s.ImportarCsvAsync(It.IsAny<Stream>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(3);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("nombre,clase_onu\nCemento,4.1\n")), "archivo", "tipos.csv" }
        };
        var response = await client.PostAsync("/api/tipos-mercancia/importar", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<int>>();
        body!.Data.Should().Be(3);
    }

    [Fact]
    public async Task Deactivate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TipoMercanciaService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/tipos-mercancia/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.TipoMercanciaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoMercanciaResponseDto { Id = id, Nombre = "Combustible Diesel" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tipos-mercancia/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<TipoMercanciaResponseDto>>();
        body!.Data!.Nombre.Should().Be("Combustible Diesel");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.TipoMercanciaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((TipoMercanciaResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tipos-mercancia/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TipoMercanciaService
            .Setup(s => s.UpdateAsync(id, It.IsAny<TipoMercanciaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoMercanciaResponseDto { Id = id, Nombre = "Combustible Diésel Premium" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/tipos-mercancia/{id}",
            new TipoMercanciaRequestDto { Nombre = "Combustible Diésel Premium" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.TipoMercanciaService.Verify(
            s => s.UpdateAsync(id, It.Is<TipoMercanciaRequestDto>(d => d.Nombre == "Combustible Diésel Premium"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }
}