using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Unidad;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del UnidadesMedidaController (HU-018).
/// Módulo 'configuracion'. Cubre el simulador de conversión (CA-05):
/// GET /api/unidades-medida/convertir → ApiResponse&lt;decimal&gt;.
/// </summary>
public class UnidadesMedidaControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public UnidadesMedidaControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    [Fact]
    public async Task Convertir_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/unidades-medida/convertir?valor=100&desde=kg&hacia=lb");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Convertir_ConPermisoRead_Retorna200ConResultado()
    {
        _factory.UnidadMedidaService
            .Setup(s => s.ConvertirAsync(100m, "kg", "lb", JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(220.46m);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/unidades-medida/convertir?valor=100&desde=kg&hacia=lb");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<decimal>>();
        body!.Data.Should().Be(220.46m);
        _factory.UnidadMedidaService.Verify(
            s => s.ConvertirAsync(100m, "kg", "lb", JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.UnidadMedidaService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, It.IsAny<string?>()))
            .ReturnsAsync(new List<UnidadMedidaResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/unidades-medida?tipo=PESO");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.UnidadMedidaService
            .Setup(s => s.CreateAsync(It.IsAny<UnidadMedidaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UnidadMedidaResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/unidades-medida",
            new UnidadMedidaRequestDto { Nombre = "Kilogramo", Simbolo = "kg" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/unidades-medida",
            new UnidadMedidaRequestDto { Nombre = "Kilogramo", Simbolo = "kg" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.UnidadMedidaService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/unidades-medida/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.UnidadMedidaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UnidadMedidaResponseDto { Id = id, Nombre = "Kilogramo", Simbolo = "kg" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/unidades-medida/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<UnidadMedidaResponseDto>>();
        body!.Data!.Nombre.Should().Be("Kilogramo");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.UnidadMedidaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((UnidadMedidaResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/unidades-medida/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.UnidadMedidaService
            .Setup(s => s.UpdateAsync(id, It.IsAny<UnidadMedidaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UnidadMedidaResponseDto { Id = id, Nombre = "Kilogramo", Simbolo = "kg" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/unidades-medida/{id}",
            new UnidadMedidaRequestDto { Nombre = "Kilogramo", Simbolo = "kg" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UnidadMedidaService.Verify(
            s => s.UpdateAsync(id, It.Is<UnidadMedidaRequestDto>(d => d.Simbolo == "kg"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }
}