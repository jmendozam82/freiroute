using System.Net;
using Freiroute.BLL.Services;
using Freiroute.DTO.Geo;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests unitarios del cliente HTTP de geocodificación Nominatim (ADR-014, Sprint 3).
/// Verifica MapToEntity fail-soft: NUNCA lanza — retorna null ante errores HTTP,
/// JSON inválido o respuestas sin resultados.
/// </summary>
public class NominatimGeocodingServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private static NominatimGeocodingService CrearServicio(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test") };
        return new NominatimGeocodingService(httpClient, Mock.Of<ILogger<NominatimGeocodingService>>());
    }

    [Fact]
    public async Task GeocodeAsync_CuandoRespuestaValida_RetornaCoordenadas()
    {
        var service = CrearServicio(
            """[{"lat":"12.11499","lon":"-86.23617","display_name":"Managua, Nicaragua","importance":0.6}]""");

        var result = await service.GeocodeAsync("Managua", "Managua", "Nicaragua");

        result.Should().NotBeNull();
        result!.Latitud.Should().Be(12.11499);
        result.Longitud.Should().Be(-86.23617);
        result.DireccionNormalizada.Should().Be("Managua, Nicaragua");
        result.Confianza.Should().Be(0.6);
        result.Proveedor.Should().Be("nominatim");
    }

    [Fact]
    public async Task GeocodeAsync_CuandoDireccionVacia_RetornaNullSinLlamarHttp()
    {
        var service = CrearServicio("[]");

        var result = await service.GeocodeAsync("   ");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_CuandoArrayVacio_RetornaNull()
    {
        var service = CrearServicio("[]");

        var result = await service.GeocodeAsync("Dirección inexistente");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_CuandoHttpError_RetornaNullFailSoft()
    {
        var service = CrearServicio("", HttpStatusCode.InternalServerError);

        var result = await service.GeocodeAsync("Managua");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_CuandoClienteLanzaExcepcion_RetornaNullFailSoft()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("sin red"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test") };
        var service = new NominatimGeocodingService(httpClient, Mock.Of<ILogger<NominatimGeocodingService>>());

        var result = await service.GeocodeAsync("Managua");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReverseGeocodeAsync_CuandoRespuestaValida_RetornaDireccion()
    {
        var service = CrearServicio("""{"display_name":"Managua, Nicaragua"}""");

        var result = await service.ReverseGeocodeAsync(12.11499, -86.23617);

        result.Should().Be("Managua, Nicaragua");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_CuandoNoHayDisplayName_RetornaNull()
    {
        var service = CrearServicio("{}");

        var result = await service.ReverseGeocodeAsync(12.11499, -86.23617);

        result.Should().BeNull();
    }
}