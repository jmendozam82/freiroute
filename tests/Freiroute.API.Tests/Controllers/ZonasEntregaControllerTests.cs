using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Zona;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del ZonasEntregaController (HU-016, ADR-018).
/// Router api/zonas-entrega. Verifica RLS (empresa_id del JWT), permisos
/// READ/CREATE/UPDATE y pertenencia point-in-polygon [lng,lat] (CA-05).
/// </summary>
public class ZonasEntregaControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ZonasEntregaControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/zonas-entrega");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.ZonaEntregaService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new List<ZonaResponseDto> { new() { Nombre = "Managua Centro" } });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/zonas-entrega");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerificarPertenencia_ConPermisoRead_Retorna200()
    {
        _factory.ZonaEntregaService
            .Setup(s => s.VerificarPertenenciaAsync(12.15, -86.25, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new List<ZonaResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/zonas-entrega/verificar-pertenencia",
            new VerificarPertenenciaZonaRequestDto { Latitud = 12.15, Longitud = -86.25 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/zonas-entrega",
            new ZonaRequestDto { Nombre = "Zona Norte" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.ZonaEntregaService
            .Setup(s => s.CreateAsync(It.IsAny<ZonaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ZonaResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/zonas-entrega",
            new ZonaRequestDto { Nombre = "Zona Norte", Codigo = "ZN" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AsignarUbicaciones_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        var ubicacionId = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.AsignarUbicacionesAsync(id, It.IsAny<Guid[]>(), JwtTestHelper.EmpresaTenant))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync($"/api/zonas-entrega/{id}/ubicaciones",
            new[] { ubicacionId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/zonas-entrega/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ZonaResponseDto { Id = id, Nombre = "Managua Centro" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/zonas-entrega/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<ZonaResponseDto>>();
        body!.Data!.Nombre.Should().Be("Managua Centro");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((ZonaResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/zonas-entrega/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.UpdateAsync(id, It.IsAny<ZonaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new ZonaResponseDto { Id = id, Nombre = "Zona Norte Actualizada" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/zonas-entrega/{id}",
            new ZonaRequestDto { Nombre = "Zona Norte Actualizada", Codigo = "ZN" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ZonaEntregaService.Verify(
            s => s.UpdateAsync(id, It.Is<ZonaRequestDto>(d => d.Nombre == "Zona Norte Actualizada"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task DesasignarUbicacion_ConPermisoUpdate_Retorna200()
    {
        var zonaId = Guid.NewGuid();
        var ubicacionId = Guid.NewGuid();
        _factory.ZonaEntregaService
            .Setup(s => s.DesasignarUbicacionAsync(zonaId, ubicacionId, JwtTestHelper.EmpresaTenant))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.DeleteAsync($"/api/zonas-entrega/{zonaId}/ubicaciones/{ubicacionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ZonaEntregaService.Verify(
            s => s.DesasignarUbicacionAsync(zonaId, ubicacionId, JwtTestHelper.EmpresaTenant), Times.Once);
    }
}