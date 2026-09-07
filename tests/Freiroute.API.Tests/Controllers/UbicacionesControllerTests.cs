using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Ubicacion;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Pagination;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del UbicacionesController (HU-015, ADR-014).
/// Verifica RLS indirecto (empresa_id del JWT con GetTenantEfectivo),
/// permisos READ/CREATE/UPDATE del módulo 'configuracion' y soft delete
/// (PATCH /deactivate — nunca DELETE).
/// </summary>
public class UbicacionesControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public UbicacionesControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/ubicaciones");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.UbicacionService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, It.IsAny<string?>(), It.IsAny<string?>(), 1, 20))
            .ReturnsAsync(new PagedResult<UbicacionResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // configuracion:read

        var response = await client.GetAsync("/api/ubicaciones?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UbicacionService.Verify(
            s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.UbicacionService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((UbicacionResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/ubicaciones/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // solo read

        var response = await client.PostAsJsonAsync("/api/ubicaciones",
            new UbicacionRequestDto { Nombre = "Bodega Central" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.UbicacionService
            .Setup(s => s.CreateAsync(It.IsAny<UbicacionRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UbicacionResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/ubicaciones",
            new UbicacionRequestDto { Nombre = "Bodega Central" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _factory.UbicacionService.Verify(
            s => s.CreateAsync(It.Is<UbicacionRequestDto>(d => d.Nombre == "Bodega Central"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task ActualizarCoordenadas_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.UbicacionService
            .Setup(s => s.ActualizarCoordenadasAsync(id, It.IsAny<ActualizarCoordenadasDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UbicacionResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsJsonAsync($"/api/ubicaciones/{id}/coordenadas",
            new ActualizarCoordenadasDto { Latitud = 12.15, Longitud = -86.25 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.UbicacionService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/ubicaciones/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.UbicacionService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UbicacionResponseDto { Id = id, Nombre = "Bodega Central" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/ubicaciones/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<UbicacionResponseDto>>();
        body.Should().NotBeNull();
        body!.Data!.Nombre.Should().Be("Bodega Central");
    }

    [Fact]
    public async Task GetParaMapa_ConPermisoRead_Retorna200ConPayload()
    {
        _factory.UbicacionService
            .Setup(s => s.GetParaMapaAsync(JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(
            [
                new UbicacionMapaDto { Id = Guid.NewGuid(), Nombre = "Bodega Central", Latitud = 12.15, Longitud = -86.25 },
                new UbicacionMapaDto { Id = Guid.NewGuid(), Nombre = "Planta Norte", Latitud = 12.11, Longitud = -86.23 }
            ]);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/ubicaciones/mapa");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<IEnumerable<UbicacionMapaDto>>>();
        body!.Data.Should().HaveCount(2);
        body.Data.First().Nombre.Should().Be("Bodega Central");
        _factory.UbicacionService.Verify(
            s => s.GetParaMapaAsync(JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.UbicacionService
            .Setup(s => s.UpdateAsync(id, It.IsAny<UbicacionRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new UbicacionResponseDto { Id = id, Nombre = "Bodega Oeste" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/ubicaciones/{id}",
            new UbicacionRequestDto { Nombre = "Bodega Oeste" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UbicacionService.Verify(
            s => s.UpdateAsync(id, It.Is<UbicacionRequestDto>(d => d.Nombre == "Bodega Oeste"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task Importar_ConPermisoCreate_Retorna200ConConteo()
    {
        _factory.UbicacionService
            .Setup(s => s.ImportarCsvAsync(It.IsAny<Stream>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(2);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("nombre,tipo,ciudad\nBodega A,ALMACEN,Managua\nBodega B,ALMACEN,León\n")), "archivo", "ubicaciones.csv" }
        };
        var response = await client.PostAsync("/api/ubicaciones/importar", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<int>>();
        body!.Data.Should().Be(2);
    }

    [Fact]
    public async Task Exportar_ConPermiso_Retorna200ConCsv()
    {
        _factory.UbicacionService
            .Setup(s => s.GetAllAsync(JwtTestHelper.EmpresaTenant, null, null, 1, int.MaxValue))
            .ReturnsAsync(new PagedResult<UbicacionResponseDto>
            {
                Items =
                [
                    new()
                    {
                        Nombre = "Almacén",
                        Tipo = "ALMACEN",
                        Pais = "Nicaragua"
                    }
                ],
                TotalItems = 1
            });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // configuracion:read

        var response = await client.GetAsync("/api/ubicaciones/exportar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("text/csv");
        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("Nombre;Código");
        csv.Should().Contain("Nicaragua");
        csv.Should().Contain(";");
        _factory.AuditoriaService.Verify(a => a.RegistrarAsync(
            "ubicaciones", AccionAuditoria.EXPORT, JwtTestHelper.EmpresaTenant,
            It.IsAny<Guid>(), "Ubicacion", null, It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task Exportar_SinPermisoRead_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSinPermisos);

        var response = await client.GetAsync("/api/ubicaciones/exportar");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}