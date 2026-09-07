using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Unidad;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del TiposEmbalajeController (HU-020, G-06).
/// Módulo 'configuracion'. El código se normaliza a mayúsculas en BLL
/// (validado en TipoEmbalajeServiceTests) y solo existe soft delete.
/// </summary>
public class TiposEmbalajeControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public TiposEmbalajeControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/tipos-embalaje");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.TipoEmbalajeService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new List<TipoEmbalajeResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/tipos-embalaje");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.TipoEmbalajeService
            .Setup(s => s.CreateAsync(It.IsAny<TipoEmbalajeRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoEmbalajeResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/tipos-embalaje",
            new TipoEmbalajeRequestDto { Nombre = "Pallet", Codigo = "PAL" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/tipos-embalaje",
            new TipoEmbalajeRequestDto { Nombre = "Pallet", Codigo = "PAL" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TipoEmbalajeService
            .Setup(s => s.UpdateAsync(id, It.IsAny<TipoEmbalajeRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoEmbalajeResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/tipos-embalaje/{id}",
            new TipoEmbalajeRequestDto { Nombre = "Pallet Europeo", Codigo = "EUR" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TipoEmbalajeService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/tipos-embalaje/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.TipoEmbalajeService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TipoEmbalajeResponseDto { Id = id, Nombre = "Pallet", Codigo = "PAL" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tipos-embalaje/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<TipoEmbalajeResponseDto>>();
        body!.Data!.Codigo.Should().Be("PAL");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.TipoEmbalajeService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((TipoEmbalajeResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tipos-embalaje/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}