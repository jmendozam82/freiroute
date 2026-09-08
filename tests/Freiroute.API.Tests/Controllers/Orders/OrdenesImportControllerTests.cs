using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.DTO.Orden;
using Moq;
using Xunit;

namespace Freiroute.API.Tests.Controllers.Orders;

[Collection("API Tests")]
public class OrdenesImportControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

public OrdenesImportControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = JwtTestHelper.TokenOrdenes; // módulo ordenes
    }

[Fact]
    public async Task DescargarPlantilla_RetornaCsvConHeadersCorrectos()
    {
        var client = _factory.CrearClientConToken(_validToken);
        var contenidoMock = System.Text.Encoding.UTF8.GetBytes("Header1,Header2");
        _factory.OrdenImportService.Setup(s => s.ObtenerPlantillaCsvAsync()).ReturnsAsync("Header1,Header2");

        var response = await client.GetAsync("/api/ordenes/importar/plantilla");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        
        var contentDisposition = response.Content.Headers.ContentDisposition?.ToString() ?? "";
        contentDisposition.Should().Contain("plantilla_importacion_ordenes.csv");
        
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Header1,Header2");
    }

    [Fact]
    public async Task ImportarCsv_CuandoNoHayArchivo_RetornaBadRequest()
    {
        var client = _factory.CrearClientConToken(_validToken);
        using var content = new MultipartFormDataContent();
        // sin archivo

        var response = await client.PostAsync("/api/ordenes/importar", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportarCsv_CuandoExtensionNoCsv_IgualProcesaFailSoftRetorna200()
    {
        // CA-05 HU-022: la importación retorna 200 con resultado fail-soft.
        // El controlador no valida extensión; los contenidos no-CSV son manejados
        // por el servicio (filas inválidas → detalle de errores). Gap menor de
        // UX: podría validarse la extensión antes de procesar (ver QA report).
        var client = _factory.CrearClientConToken(_validToken);
        using var content = new MultipartFormDataContent();
        
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "archivo.txt");

        var mockResponse = new ImportacionOrdenResultDto { TotalFilas = 1, FilasOk = 0 };
        _factory.OrdenImportService.Setup(s => s.ImportarCsvAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(mockResponse);

        var response = await client.PostAsync("/api/ordenes/importar", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ImportarCsv_CuandoValido_LlamaAServiceYRetornaOk()
    {
        var client = _factory.CrearClientConToken(_validToken);
        using var content = new MultipartFormDataContent();
        
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("ClienteId,OrigenId\n1,2"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "archivo.csv");

        var mockResponse = new ImportacionOrdenResultDto { TotalFilas = 1, FilasOk = 1, DetalleErrores = new System.Collections.Generic.List<Freiroute.DTO.Orden.ErrorFilaOrdenDto>() };
        _factory.OrdenImportService.Setup(s => s.ImportarCsvAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(mockResponse);

        var response = await client.PostAsync("/api/ordenes/importar", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}



