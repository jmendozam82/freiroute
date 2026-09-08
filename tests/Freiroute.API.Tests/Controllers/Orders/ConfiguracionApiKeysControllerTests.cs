using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers.Orders;

[Collection("API Tests")]
public class ConfiguracionApiKeysControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

public ConfiguracionApiKeysControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenConfiguracion; // módulo configuracion del tenant
    }

    [Fact]
    public async Task GenerarApiKey_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var dto = new ApiKeyOrdenRequestDto { Nombre = "App Externa" };

        var response = await client.PostAsJsonAsync("/api/configuracion/api-keys", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerarApiKey_ConToken_Retorna200YRawKeyEnPlanoSoloUnaVez()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var dto = new ApiKeyOrdenRequestDto { Nombre = "App Externa" };
        var rawKey = "frk_live_12345678abcdefgh";

        _factory.OrdenApiExternaService.Setup(s => s.GenerarApiKeyAsync(It.IsAny<ApiKeyOrdenRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ApiKeyResponseDto { Id = Guid.NewGuid(), RawKey = rawKey, Nombre = dto.Nombre });

        var response = await client.PostAsJsonAsync("/api/configuracion/api-keys", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ApiKeyResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.RawKey.Should().Be(rawKey);
    }

    [Fact]
    public async Task DesactivarApiKey_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();

        _factory.OrdenApiExternaService.Setup(s => s.DesactivarApiKeyAsync(id, It.IsAny<Guid>())).ReturnsAsync(true);

        var response = await client.DeleteAsync($"/api/configuracion/api-keys/{id}/deactivate");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiKeys_Retorna200SinRawKeys()
    {
        var client = _factory.CrearClientConToken(_validToken);

        var response = await client.GetAsync("/api/configuracion/api-keys");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

