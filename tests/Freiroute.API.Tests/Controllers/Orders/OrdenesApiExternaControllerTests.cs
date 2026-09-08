using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Orden;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers.Orders;

[Collection("API Tests")]
public class OrdenesApiExternaControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

    public OrdenesApiExternaControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenAdmin;
    }

    [Fact]
    public async Task CreateFromApi_SinApiKey_Retorna401()
    {
        var client = _factory.CrearClientSinToken(); // anónimo, sin token y sin API Key
        var dto = new OrdenRequestDto();

        var response = await client.PostAsJsonAsync("/api/v1/orders", dto);
        
        // El middleware/filtro que valida el Api Key debe devolver 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFromApi_ConApiKeyInvalida_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        client.DefaultRequestHeaders.Add("X-Api-Key", "fr_invalid_key");
        var dto = new OrdenRequestDto();

        _factory.OrdenApiExternaService.Setup(s => s.ValidarApiKeyAsync("fr_invalid_key")).ReturnsAsync((Guid?)null);

        var response = await client.PostAsJsonAsync("/api/v1/orders", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFromApi_ConApiKeyValida_IgnoraJwtYRetorna201()
    {
        var client = _factory.CrearClientSinToken(); // No le pasamos el JWT
        client.DefaultRequestHeaders.Add("X-Api-Key", "fr_valid_key");
        
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
        var empresaId = Guid.NewGuid();

        _factory.OrdenApiExternaService.Setup(s => s.ValidarApiKeyAsync("fr_valid_key")).ReturnsAsync(empresaId);
        _factory.OrdenApiExternaService.Setup(s => s.CrearOrdenDesdeApiAsync(It.IsAny<OrdenRequestDto>(), empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = Guid.NewGuid(), Estado = Freiroute.Utility.Constants.OrdenEstado.Draft });

        var response = await client.PostAsJsonAsync("/api/v1/orders", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateFromApi_ConJwtYConApiKey_ElApiKeyMandaYRetorna201()
    {
        var client = _factory.CrearClientConToken(_validToken); // Le pasamos JWT
        client.DefaultRequestHeaders.Add("X-Api-Key", "fr_valid_key");
        
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
        var empresaId = Guid.NewGuid();

        _factory.OrdenApiExternaService.Setup(s => s.ValidarApiKeyAsync("fr_valid_key")).ReturnsAsync(empresaId);
        _factory.OrdenApiExternaService.Setup(s => s.CrearOrdenDesdeApiAsync(It.IsAny<OrdenRequestDto>(), empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = Guid.NewGuid(), Estado = Freiroute.Utility.Constants.OrdenEstado.Draft });

        var response = await client.PostAsJsonAsync("/api/v1/orders", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}


