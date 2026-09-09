using System;
using System.Collections.Generic;
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

    // ── Sprint 5: PO Integration (HU-028) ─────────────────────────

    [Fact]
    public async Task GetPorPo_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.GetAsync("/api/ordenes/por-po/PO-100");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPorPo_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        _factory.OrdenPoService
            .Setup(s => s.GetPorPoAsync("PO-100", It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrdenListDto>
            {
                new OrdenListDto { Id = Guid.NewGuid(), NumeroOrden = "ORD-2026-00001", Estado = OrdenEstado.Confirmed }
            });

        var response = await client.GetAsync("/api/ordenes/por-po/PO-100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrdenListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(1);
    }

    [Fact]
    public async Task VincularPo_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new OrdenPoRequestDto { NumeroPo = "PO-100", NumeroSo = "SO-200" };

        _factory.OrdenPoService
            .Setup(s => s.VincularPoAsync(id, It.IsAny<OrdenPoRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenResponseDto { Id = id, Estado = OrdenEstado.Confirmed });

        var response = await client.PatchAsJsonAsync($"/api/ordenes/{id}/po", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrdenResponseDto>>();
        result!.Data!.Id.Should().Be(id);
    }

    // ── Sprint 5: Priorización dinámica (HU-029) ──────────────────

    [Fact]
    public async Task CambiarPrioridad_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new PrioridadRequestDto { Prioridad = OrdenPrioridad.Critico, Motivo = "Cliente VIP requiere urgencia" };

        _factory.PrioridadOrdenService
            .Setup(s => s.CambiarPrioridadAsync(id, It.IsAny<PrioridadRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenResponseDto { Id = id, Estado = OrdenEstado.Confirmed, Prioridad = OrdenPrioridad.Critico });

        var response = await client.PatchAsJsonAsync($"/api/ordenes/{id}/prioridad", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrdenResponseDto>>();
        result!.Data!.Prioridad.Should().Be(OrdenPrioridad.Critico);
    }

    [Fact]
    public async Task GetCriticas_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var response = await client.GetAsync("/api/ordenes/criticas");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCriticas_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        _factory.PrioridadOrdenService
            .Setup(s => s.GetCriticasAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrdenListDto>
            {
                new OrdenListDto { Id = Guid.NewGuid(), NumeroOrden = "ORD-2026-00002", Estado = OrdenEstado.InTransit }
            });

        var response = await client.GetAsync("/api/ordenes/criticas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrdenListDto>>>();
        result!.Data!.Should().HaveCount(1);
    }

    // ── Sprint 5: SLA (HU-031 CA-02) ──────────────────────────────

    [Fact]
    public async Task GetSlaEnRiesgo_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        _factory.SlaService
            .Setup(s => s.GetEnRiesgoAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrdenListDto>
            {
                new OrdenListDto { Id = Guid.NewGuid(), NumeroOrden = "ORD-2026-00003", Estado = OrdenEstado.Assigned }
            });

        var response = await client.GetAsync("/api/ordenes/sla-en-riesgo");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrdenListDto>>>();
        result!.Data!.Should().HaveCount(1);
    }

    // ── Sprint 5: Rechazos y re-entregas (HU-030) ─────────────────

    [Fact]
    public async Task RegistrarRechazo_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var dto = new RechazoEntregaRequestDto { Motivo = "CLIENTE_AUSENTE", Descripcion = "No había nadie en el domicilio" };

        _factory.RechazoEntregaService
            .Setup(s => s.RegistrarRechazoAsync(id, It.IsAny<RechazoEntregaRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new RechazoEntregaResponseDto { Id = Guid.NewGuid(), OrdenId = id, Motivo = "CLIENTE_AUSENTE" });

        var response = await client.PostAsJsonAsync($"/api/ordenes/{id}/rechazo", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RechazoEntregaResponseDto>>();
        result!.Data!.OrdenId.Should().Be(id);
    }

    [Fact]
    public async Task CrearReentrega_ConToken_Retorna201YLocation()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        var nuevaId = Guid.NewGuid();
        var dto = new ReEntregaRequestDto { Instrucciones = "Entregar solo en horario hábil" };

        _factory.RechazoEntregaService
            .Setup(s => s.CrearReentregaAsync(id, It.IsAny<ReEntregaRequestDto?>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenResponseDto { Id = nuevaId, Estado = OrdenEstado.Confirmed });

        var response = await client.PostAsJsonAsync($"/api/ordenes/{id}/re-entrega", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrdenResponseDto>>();
        result!.Data!.Estado.Should().Be(OrdenEstado.Confirmed);
    }

    [Fact]
    public async Task GetReentregas_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();
        _factory.RechazoEntregaService
            .Setup(s => s.GetReentregasAsync(id, It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrdenListDto>
            {
                new OrdenListDto { Id = Guid.NewGuid(), NumeroOrden = "ORD-2026-00004", Estado = OrdenEstado.Confirmed }
            });

        var response = await client.GetAsync($"/api/ordenes/{id}/re-entregas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<OrdenListDto>>>();
        result!.Data!.Should().HaveCount(1);
    }
}




