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
public class PlantillasOrdenControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

    public PlantillasOrdenControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenOrdenes; // módulo ordenes
    }

    [Fact]
    public async Task GetById_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/api/plantillas-orden/{id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConToken_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);

        var response = await client.GetAsync("/api/plantillas-orden");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

[Fact]
    public async Task GuardarPlantilla_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var dto = new PlantillaOrdenRequestDto
        {
            Nombre = "Plantilla 1",
            Descripcion = "Desc"
        };
        var idRetorno = Guid.NewGuid();

        _factory.PlantillaOrdenService.Setup(s => s.GuardarComoPlantillaAsync(It.IsAny<Guid>(), It.IsAny<PlantillaOrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new PlantillaOrdenResponseDto { Id = idRetorno, Nombre = dto.Nombre });

        var ordenId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync($"/api/ordenes/{ordenId}/guardar-como-plantilla", dto);
        
        // El controller responde Ok(ApiResponse<...>) → 200 (no CreatedAtAction)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PlantillaOrdenResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(idRetorno);
    }

    [Fact]
    public async Task CrearOrden_DesdePlantilla_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var plantillaId = Guid.NewGuid();
        var ordenGenerada = new OrdenResponseDto { Id = Guid.NewGuid() };

        _factory.PlantillaOrdenService.Setup(s => s.CrearOrdenDesdePlantillaAsync(plantillaId, It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(ordenGenerada);

        var response = await client.PostAsync($"/api/plantillas-orden/{plantillaId}/crear-orden", null);
        
        // El controller responde Ok(ApiResponse<...>) → 200
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrdenResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(ordenGenerada.Id);
    }

    [Fact]
    public async Task Delete_Retorna200()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var id = Guid.NewGuid();

        _factory.PlantillaOrdenService.Setup(s => s.DeactivateAsync(id, It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Ruta real del controller: DELETE plantillas-orden/{id}/deactivate (soft delete)
        var response = await client.DeleteAsync($"/api/plantillas-orden/{id}/deactivate");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}





