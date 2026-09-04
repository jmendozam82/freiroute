using System.IO;
using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Configuracion;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del ConfiguracionController (HU-014).
/// RequirePermission sobre el módulo 'configuracion'.
/// </summary>
public class ConfiguracionControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ConfiguracionControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    // Token con read + update de 'configuracion' para las operaciones de escritura.
    private static string TokenConUpdate => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:update"],
        "ADMIN");

    private static ConfiguracionResponseDto Config() => new()
    {
        EmpresaId = JwtTestHelper.EmpresaTenant,
        Nombre = "Trans SA",
        Moneda = "USD",
        ZonaHoraria = "America/Managua",
        FormatoFecha = "DD/MM/YYYY"
    };

    private static NumeracionResponseDto Numeracion() => new()
    {
        PrefijoEmbarque = "FR",
        ConsecutivoEmbarque = 100,
        PrefijoOrden = "ORD",
        PrefijoCartaPorte = "CP"
    };

    [Fact]
    public async Task Get_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/configuracion");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ConPermisoRead_Retorna200()
    {
        _factory.ConfiguracionService
            .Setup(s => s.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Config());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // configuracion:read

        var response = await client.GetAsync("/api/configuracion");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNumeracion_ConPermisoRead_Retorna200()
    {
        _factory.ConfiguracionService
            .Setup(s => s.GetNumeracionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Numeracion());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync("/api/configuracion/numeracion");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_SinPermisoUpdate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // solo read

        var response = await client.PutAsJsonAsync("/api/configuracion",
            new ConfiguracionRequestDto { Nombre = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        _factory.ConfiguracionService
            .Setup(s => s.UpdateAsync(It.IsAny<ConfiguracionRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(Config());

        var client = _factory.CrearClientConToken(TokenConUpdate);

        var response = await client.PutAsJsonAsync("/api/configuracion",
            new ConfiguracionRequestDto { Nombre = "Trans Nicaragua SA" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateNumeracion_ConPermisoUpdate_Retorna200()
    {
        _factory.ConfiguracionService
            .Setup(s => s.UpdateNumeracionAsync(It.IsAny<NumeracionRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(Numeracion());

        var client = _factory.CrearClientConToken(TokenConUpdate);

        var response = await client.PutAsJsonAsync("/api/configuracion/numeracion",
            new NumeracionRequestDto { PrefijoEmbarque = "NIC", PrefijoOrden = "ORD", PrefijoCartaPorte = "CP" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/configuracion/logo (SubirLogo) ─────────────────────

    [Fact]
    public async Task SubirLogo_ConArchivo_Retorna200ConUrl()
    {
        var logoUrl = "https://storage.example.com/logos/abc/logo.png";
        _factory.ConfiguracionService
            .Setup(s => s.UpdateLogoAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(logoUrl);

        var client = _factory.CrearClientConToken(TokenConUpdate);

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        var stream = new MemoryStream(fileBytes);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "archivo", "logo.png");

        var response = await client.PostAsync("/api/configuracion/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(logoUrl);
    }

    [Fact]
    public async Task SubirLogo_SinArchivo_Retorna400()
    {
        var client = _factory.CrearClientConToken(TokenConUpdate);

        using var content = new MultipartFormDataContent();
        // No se añade ningún archivo — el controller detecta null y retorna 400.
        var response = await client.PostAsync("/api/configuracion/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubirLogo_SinPermisoUpdate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // solo read

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var stream = new MemoryStream(fileBytes);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "archivo", "logo.png");

        var response = await client.PostAsync("/api/configuracion/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /api/configuracion/logo (EliminarLogo) ─────────────────

    [Fact]
    public async Task EliminarLogo_ConPermisoUpdate_Retorna200()
    {
        _factory.ConfiguracionService
            .Setup(s => s.DeleteLogoAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConUpdate);

        var response = await client.DeleteAsync("/api/configuracion/logo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("eliminado");
    }

    [Fact]
    public async Task EliminarLogo_SinPermisoUpdate_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // solo read

        var response = await client.DeleteAsync("/api/configuracion/logo");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
