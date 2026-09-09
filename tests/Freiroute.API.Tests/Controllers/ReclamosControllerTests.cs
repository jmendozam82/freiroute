using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Reclamo;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del módulo Claims Management (HU-032) — Sprint 5.
/// Cobertura G-12: permisos READ/CREATE/UPDATE sobre /api/reclamos.
/// </summary>
[Collection("API Tests")]
public class ReclamosControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

    public ReclamosControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenOrdenes; // ordenes:read/create/update
    }

    [Fact]
    public async Task Create_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.PostAsJsonAsync("/api/reclamos", new ReclamoRequestDto());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        // TokenSoloLectura solo tiene configuracion:read y usuarios:read →
        // RequirePermission(ordenes, CREATE) no encuentra el claim → 403.
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);
        var dto = new ReclamoRequestDto { OrdenId = Guid.NewGuid(), Tipo = "DANO", Descripcion = "Caja dañada" };

        var response = await client.PostAsJsonAsync("/api/reclamos", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ConToken_Retorna201YLocation()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var nuevoId = Guid.NewGuid();
        var dto = new ReclamoRequestDto { OrdenId = Guid.NewGuid(), Tipo = "DANO", Descripcion = "Caja dañada", MontoReclamado = 1500m };

        _factory.ReclamoService
            .Setup(s => s.CreateAsync(It.IsAny<ReclamoRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ReclamoResponseDto { Id = nuevoId, NumeroReclamo = "REC-2026-0001", Estado = EstadoReclamo.Abierto });

        var response = await client.PostAsJsonAsync("/api/reclamos", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReclamoResponseDto>>();
        result!.Data!.Estado.Should().Be(EstadoReclamo.Abierto);
    }

    [Fact]
    public async Task GetById_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();

        _factory.ReclamoService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<Guid>()))
            .ReturnsAsync(new ReclamoResponseDto { Id = id, NumeroReclamo = "REC-2026-0002", Estado = EstadoReclamo.EnRevision });

        var response = await client.GetAsync($"/api/reclamos/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReclamoResponseDto>>();
        result!.Data!.Estado.Should().Be(EstadoReclamo.EnRevision);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();

        _factory.ReclamoService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<Guid>()))
            .ReturnsAsync((ReclamoResponseDto?)null);

        var response = await client.GetAsync($"/api/reclamos/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);

        _factory.ReclamoService
            .Setup(s => s.GetAllAsync(It.IsAny<Guid>(), It.IsAny<ReclamoFiltroDto>()))
            .ReturnsAsync((new List<ReclamoListDto>
            {
                new ReclamoListDto
                {
                    Id = Guid.NewGuid(),
                    NumeroReclamo = "REC-2026-0003",
                    Tipo = "RETRASO",
                    Estado = EstadoReclamo.Abierto,
                    OrdenNumero = "ORD-2026-00001",
                    ClienteNombre = "Distribuidora ABC S.A."
                }
            }, 1));

        var response = await client.GetAsync("/api/reclamos?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Freiroute.Utility.Pagination.PagedResult<ReclamoListDto>>();
        result!.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task CambiarEstado_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new ReclamoEstadoRequestDto { EstadoNuevo = "EN_REVISION", Motivo = "Iniciando revisión" };

        _factory.ReclamoService
            .Setup(s => s.CambiarEstadoAsync(id, It.IsAny<ReclamoEstadoRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ReclamoResponseDto { Id = id, Estado = EstadoReclamo.EnRevision });

        var response = await client.PatchAsJsonAsync($"/api/reclamos/{id}/estado", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReclamoResponseDto>>();
        result!.Data!.Estado.Should().Be(EstadoReclamo.EnRevision);
    }

    [Fact]
    public async Task CambiarEstado_SinPermisoUpdate_Retorna403()
    {
        // CA-12: solo quien tenga ordenes:update puede mover el estado del reclamo.
        // TokenSoloLectura no tiene el claim → RequirePermission(ordenes, UPDATE) → 403.
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);
        var id = Guid.NewGuid();
        var dto = new ReclamoEstadoRequestDto { EstadoNuevo = "EN_REVISION", Motivo = "Iniciando revisión" };

        var response = await client.PatchAsJsonAsync($"/api/reclamos/{id}/estado", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetByCliente_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var clienteId = Guid.NewGuid();

        _factory.ReclamoService
            .Setup(s => s.GetByClienteAsync(clienteId, It.IsAny<Guid>()))
            .ReturnsAsync(new List<ReclamoListDto>
            {
                new ReclamoListDto { Id = Guid.NewGuid(), NumeroReclamo = "REC-2026-0004", Tipo = "PERDIDA", Estado = EstadoReclamo.Abierto }
            });

        var response = await client.GetAsync($"/api/reclamos/cliente/{clienteId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReclamoListDto>>>();
        result!.Data!.Should().HaveCount(1);
    }
}