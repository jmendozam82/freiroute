using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers.Orders;

/// <summary>
/// Tests de integración del ShipmentsController (HU-025 CA-06):
/// GET /api/shipments/{id}/ordenes expone las órdenes consolidadas en un shipment.
/// </summary>
[Collection("API Tests")]
public class ShipmentsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _tokenEmbarques;

    public ShipmentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _tokenEmbarques = JwtTestHelper.TokenAdmin; // TokenAdmin trae "embarques:read"
    }

    [Fact]
    public async Task GetOrdenes_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}/ordenes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrdenes_SinPermisoEmbarques_Retorna403()
    {
        // TokenOrdenes tiene permisos del módulo "ordenes" pero NO "embarques"
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenOrdenes);

        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}/ordenes");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrdenes_ConToken_Retorna200ConOrdenes()
    {
        var client = _factory.CrearClientConToken(_tokenEmbarques);
        var shipmentId = Guid.NewGuid();

        _factory.OrdenService.Setup(s => s.GetByShipmentIdAsync(shipmentId, It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrdenListDto>
            {
                new OrdenListDto { Id = Guid.NewGuid(), NumeroOrden = "ORD-2026-1", Estado = OrdenEstado.Assigned }
            });

        var response = await client.GetAsync($"/api/shipments/{shipmentId}/ordenes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OrdenListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().ContainSingle();
        result.Data![0].Estado.Should().Be(OrdenEstado.Assigned);
    }
}