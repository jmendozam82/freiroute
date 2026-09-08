using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers.Orders;

[Collection("API Tests")]
public class OrdenesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

    public OrdenesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenOrdenes; // permisos ordenes:read/create/update
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.GetAsync("/api/ordenes");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var response = await client.GetAsync("/api/ordenes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Retorna201YLocation()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var dto = new OrdenRequestDto
        {
            ClienteId = Guid.NewGuid(),
            OrigenId = Guid.NewGuid(),
            DestinoId = Guid.NewGuid(),
            TipoMercanciaId = Guid.NewGuid(),
            UnidadMedidaId = Guid.NewGuid(),
            Cantidad = 10,
            PesoKg = 100,
            ModoTransporte = "TERRESTRE",
            Prioridad = "NORMAL",
            NivelServicio = "ESTANDAR",
            Lineas = new System.Collections.Generic.List<LineaOrdenRequestDto>
            {
                new LineaOrdenRequestDto { Descripcion = "L1", Cantidad = 10, PesoKg = 100, UnidadMedidaId = Guid.NewGuid() }
            }
        };

        var response = await client.PostAsJsonAsync("/api/ordenes", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

[Fact]
    public async Task CambiarEstado_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new CambiarEstadoOrdenRequestDto { EstadoNuevo = "CONFIRMED" };

        _factory.OrdenService.Setup(s => s.CambiarEstadoAsync(id, It.IsAny<CambiarEstadoOrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenResponseDto { Id = id, Estado = OrdenEstado.Confirmed });

        // El endpoint es PATCH (controller [HttpPatch("{id:guid}/estado")]), no POST
        var response = await client.PatchAsJsonAsync($"/api/ordenes/{id}/estado", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrdenResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Estado.Should().Be(OrdenEstado.Confirmed);
    }

    [Fact]
    public async Task Split_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new SplitOrdenRequestDto
        {
            Splits = new System.Collections.Generic.List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 1, PesoKg = 10 },
                new SplitItemDto { Cantidad = 2, PesoKg = 20 }
            }
        };

        _factory.OrdenService.Setup(s => s.SplitAsync(id, It.IsAny<SplitOrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new System.Collections.Generic.List<OrdenResponseDto>
            {
                new OrdenResponseDto { Id = Guid.NewGuid(), Estado = OrdenEstado.Confirmed }
            });

        var response = await client.PostAsJsonAsync($"/api/ordenes/{id}/split", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Consolidar_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var dto = new ConsolidarOrdenesRequestDto
        {
            OrdenIds = new System.Collections.Generic.List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        var mockResponse = new ShipmentResumenDto { Id = Guid.NewGuid() };
        _factory.OrdenService.Setup(s => s.ConsolidarAsync(It.IsAny<ConsolidarOrdenesRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(mockResponse);

        var response = await client.PostAsJsonAsync($"/api/ordenes/consolidar", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}




