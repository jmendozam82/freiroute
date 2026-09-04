using System.Net;
using System.Net.Http.Json;
using System.IO;
using Freiroute.DTO.Onboarding;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del OnboardingController (HU-012, ADR-010).
/// Los endpoints NO llevan RequirePermission: se validan por autenticación y
/// resuelven el tenant desde el JWT (GetTenantEfectivo).
/// </summary>
public class OnboardingControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public OnboardingControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    private static OnboardingEstadoResponseDto Estado() => new()
    {
        PasoActual = 2,
        PorcentajeCompletado = 40,
        Completado = false
    };

    [Fact]
    public async Task GetEstado_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/onboarding");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEstado_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GetEstadoAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Estado());

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.GetAsync("/api/onboarding");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GuardarPaso1_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso1Async(It.IsAny<OnboardingPaso1RequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso1",
            new OnboardingPaso1RequestDto { Nombre = "Trans SA", Industria = "Logística" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GuardarPaso3_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso3Async(It.IsAny<OnboardingPaso3RequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso3",
            new OnboardingPaso3RequestDto
            {
                Moneda = "NIO",
                ZonaHoraria = "America/Managua",
                ModosTransporteActivos = ["FTL", "LTL"]
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Completar_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.CompletarAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsync("/api/onboarding/completar", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/onboarding/paso2 ────────────────────────────────────

    [Fact]
    public async Task GuardarPaso2_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso2Async(It.IsAny<OnboardingPaso2RequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso2",
            new OnboardingPaso2RequestDto
            {
                ColorPrimario = "#1A73E8",
                ColorSecundario = "#0B2545",
                LogoUrl = "https://storage.example.com/logos/logo.png"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GuardarPaso2_DatosInvalidos_Retorna400()
    {
        // El servicio lanza ValidationException (FluentValidation) cuando
        // los colores están vacíos → el GlobalExceptionMiddleware responde 400.
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso2Async(It.IsAny<OnboardingPaso2RequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("ColorPrimario", "El color primario es obligatorio")
            }));

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso2",
            new OnboardingPaso2RequestDto
            {
                ColorPrimario = "",
                ColorSecundario = ""
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("color primario");
    }

    // ── POST /api/onboarding/paso4 ────────────────────────────────────

    [Fact]
    public async Task GuardarPaso4_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso4Async(
                It.IsAny<OnboardingPaso4RequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso4",
            new OnboardingPaso4RequestDto
            {
                NombreCompleto = "Admin Principal",
                Telefono = "+505 8888-0000",
                CambiarPassword = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GuardarPaso4_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/onboarding/paso4",
            new OnboardingPaso4RequestDto { NombreCompleto = "Admin" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/onboarding/paso5 ────────────────────────────────────

    [Fact]
    public async Task GuardarPaso5_ConToken_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso5Async(
                It.IsAny<OnboardingPaso5RequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso5",
            new OnboardingPaso5RequestDto
            {
                Invitaciones =
                [
                    new() { Email = "operador1@transnic.com", PerfilId = Guid.NewGuid() },
                    new() { Email = "operador2@transnic.com", PerfilId = Guid.NewGuid() }
                ]
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GuardarPaso5_ListaVacia_Retorna200()
    {
        _factory.OnboardingService
            .Setup(s => s.GuardarPaso5Async(
                It.IsAny<OnboardingPaso5RequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/onboarding/paso5",
            new OnboardingPaso5RequestDto { Invitaciones = [] });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/onboarding/logo ─────────────────────────────────────

    [Fact]
    public async Task SubirLogo_ConArchivo_Retorna200ConUrl()
    {
        var logoUrl = "https://storage.example.com/logos/abc/logo.png";
        _factory.OnboardingService
            .Setup(s => s.GuardarLogoAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(logoUrl);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        var stream = new MemoryStream(fileBytes);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "archivo", "logo.png");

        var response = await client.PostAsync("/api/onboarding/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(logoUrl);
    }

    [Fact]
    public async Task SubirLogo_SinArchivo_Retorna400()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        using var content = new MultipartFormDataContent();
        // No se añade ningún archivo — el controller detecta null y retorna 400.
        var response = await client.PostAsync("/api/onboarding/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
