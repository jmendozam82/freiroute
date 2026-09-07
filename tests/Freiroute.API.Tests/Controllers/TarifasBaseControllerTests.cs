using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Tarifa;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del TarifasBaseController (HU-020, ADR-015).
/// Cubre el simulador de costo (CA-04/CA-05), la gestión de recargos
/// (códigos únicos) y el versionado (Update cierra la versión anterior).
/// </summary>
public class TarifasBaseControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public TarifasBaseControllerTests() => _factory = new TestWebApplicationFactory();

    public void Dispose() => _factory.Dispose();

    private static string TokenConEscritura => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"], "ADMIN");

    private static SimularCostoResponseDto Simulacion() => new()
    {
        TarifaEncontrada = true,
        CostoBase = 1200m,
        TotalRecargos = 220m,
        CostoTotal = 1420m
    };

    [Fact]
    public async Task SimularCosto_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/tarifas-base/simular-costo",
            new SimularCostoRequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SimularCosto_ConPermisoRead_Retorna200ConMontos()
    {
        _factory.TarifaBaseService
            .Setup(s => s.SimularCostoAsync(It.IsAny<SimularCostoRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(Simulacion());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/tarifas-base/simular-costo",
            new SimularCostoRequestDto { PesoKg = 1000, ValorDeclarado = 5000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<SimularCostoResponseDto>>();
        body!.Data!.TarifaEncontrada.Should().BeTrue();
        body.Data.CostoTotal.Should().Be(1420m);
    }

    [Fact]
    public async Task GetAll_ConPermisoRead_Retorna200()
    {
        _factory.TarifaBaseService
            .Setup(s => s.GetAllAsync(
                JwtTestHelper.EmpresaTenant, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>(), 1, 20))
            .ReturnsAsync(new Freiroute.Utility.Pagination.PagedResult<TarifaBaseResponseDto>());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/tarifas-base");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.PostAsJsonAsync("/api/tarifas-base",
            new TarifaBaseRequestDto { Nombre = "Flete Managua-Ocotal" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        _factory.TarifaBaseService
            .Setup(s => s.CreateAsync(It.IsAny<TarifaBaseRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TarifaBaseResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync("/api/tarifas-base",
            new TarifaBaseRequestDto { Nombre = "Flete Managua-Ocotal" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AgregarRecargo_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.AgregarRecargoAsync(id, It.IsAny<RecargoTarifaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new RecargoTarifaResponseDto());

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PostAsJsonAsync($"/api/tarifas-base/{id}/recargos",
            new RecargoTarifaRequestDto { Nombre = "Combustible" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Deactivate_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.DeactivateAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/tarifas-base/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoExiste_Retorna200ConDatos()
    {
        var id = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TarifaBaseResponseDto { Id = id, Nombre = "Flete Managua-Ocotal" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tarifas-base/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<TarifaBaseResponseDto>>();
        body!.Data!.Nombre.Should().Be("Flete Managua-Ocotal");
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var id = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.GetByIdAsync(id, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync((TarifaBaseResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/tarifas-base/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVigente_CuandoExiste_Retorna200ConTarifa()
    {
        _factory.TarifaBaseService
            .Setup(s => s.GetVigenteAsync(JwtTestHelper.EmpresaTenant,
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new TarifaBaseResponseDto { Id = Guid.NewGuid(), Nombre = "Flete Vigente" });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync(
            "/api/tarifas-base/vigente?zonaOrigenId=" + Guid.NewGuid() + "&modo=Ftl&tipoServicio=puntual");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Freiroute.Utility.ApiResponse.ApiResponse<TarifaBaseResponseDto?>>();
        body!.Data!.Nombre.Should().Be("Flete Vigente");
    }

    [Fact]
    public async Task GetVigente_CuandoNoExiste_Retorna200ConDataNull()
    {
        _factory.TarifaBaseService
            .Setup(s => s.GetVigenteAsync(JwtTestHelper.EmpresaTenant,
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((TarifaBaseResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync(
            "/api/tarifas-base/vigente?zonaOrigenId=" + Guid.NewGuid() + "&modo=Ftl");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.UpdateAsync(id, It.IsAny<TarifaBaseRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new TarifaBaseResponseDto { Id = id, Nombre = "Flete Nueva Versión" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/tarifas-base/{id}",
            new TarifaBaseRequestDto { Nombre = "Flete Nueva Versión" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.TarifaBaseService.Verify(
            s => s.UpdateAsync(id, It.Is<TarifaBaseRequestDto>(d => d.Nombre == "Flete Nueva Versión"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task UpdateRecargo_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        var recargoId = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.UpdateRecargoAsync(id, recargoId, It.IsAny<RecargoTarifaRequestDto>(), JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(new RecargoTarifaResponseDto { Id = recargoId, Nombre = "Combustible 15%" });

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PutAsJsonAsync($"/api/tarifas-base/{id}/recargos/{recargoId}",
            new RecargoTarifaRequestDto { Nombre = "Combustible 15%" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.TarifaBaseService.Verify(
            s => s.UpdateRecargoAsync(id, recargoId, It.Is<RecargoTarifaRequestDto>(d => d.Nombre == "Combustible 15%"),
                JwtTestHelper.EmpresaTenant), Times.Once);
    }

    [Fact]
    public async Task DeactivateRecargo_ConPermisoUpdate_Retorna200()
    {
        var id = Guid.NewGuid();
        var recargoId = Guid.NewGuid();
        _factory.TarifaBaseService
            .Setup(s => s.DeactivateRecargoAsync(id, recargoId, JwtTestHelper.EmpresaTenant))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConEscritura);

        var response = await client.PatchAsync($"/api/tarifas-base/{id}/recargos/{recargoId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.TarifaBaseService.Verify(
            s => s.DeactivateRecargoAsync(id, recargoId, JwtTestHelper.EmpresaTenant), Times.Once);
    }
}