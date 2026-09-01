namespace Freiroute.API.Tests.Controllers;

using Freiroute.BLL.Services;
using Freiroute.DTO.Empresa;
using Freiroute.Utility.ApiResponse;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

/// <summary>
/// Tests de integración para EmpresasController - endpoints REST del modulo EMPRESA (HU-001).
/// Usa TestWebApplicationFactory con mocks en la capa de servicio y bypass de authorization
/// para probar el pipeline HTTP completo sin autenticación real ni conexión a BD.
/// Cobertura: POST /api/empresas (crear tenant), GET /api/empresas/{id} (buscar por ID).
/// Patrón AAA: ARRANGE → ACT → ASSERT con FluentAssertions + Moq.
/// </summary>
public class EmpresasControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _superAdminToken;
    private readonly Mock<IEmpresaService> _mockService;

    public EmpresasControllerTests(TestWebApplicationFactory factory)
    {
        _mockService = factory.GetMockService();

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _superAdminToken = JwtTestHelper.GenerateSuperAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _superAdminToken);
    }

    // ═══════════════════════════════════════════════════
    // POST /api/empresas — Crear Empresa (Tenant)
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Create_CuandoValido_Retorna201ConData()
    {
        // ARRANGE
        var nuevoId = Guid.NewGuid();
        var respuestaDto = new EmpresaResponseDto
        {
            Id = nuevoId,
            Nombre = "Transportes Express del Norte",
            Slug = "transportes-express-del-norte",
            Plan = "professional",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(respuestaDto);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Transportes Express del Norte",
            Slug = "",
            Plan = "professional"
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmpresaResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Id.Should().Be(nuevoId);
        content.Data.Nombre.Should().Contain("Express");
        content.Data.Slug.Should().Be("transportes-express-del-norte");
        content.Data.Plan.Should().Be("professional");
        content.Message.Should().Contain("exitosa");
    }

    [Fact]
    public async Task Create_SinToken_Returns401()
    {
        // ARRANGE
        var sinAuth = new HttpClient { BaseAddress = _client.BaseAddress };

        var dto = new EmpresaRequestDto
        {
            Nombre = "Test sin Auth",
            Plan = "starter"
        };

        // ACT
        var response = await sinAuth.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ConInvalidRequestBody_Retorna400BadRequest()
    {
        // ARRANGE
        var content = new StringContent("nombre=no-es-json", System.Text.Encoding.UTF8, "text/plain");

        // ACT
        var response = await _client.PostAsync("/api/empresas", content);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ConNombreVacio_ServiceLanzaValidation_Retorna400()
    {
        // ARRANGE
        var mensajeError = "El nombre es obligatorio";
        var validationEx = CreateValidationException(mensajeError, "Nombre", mensajeError);

        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ThrowsAsync(validationEx);

        var dto = new EmpresaRequestDto
        {
            Nombre = "",
            Slug = "",
            Plan = "starter"
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmpresaResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Errors.Should().ContainSingle().Which.Should().Contain("obligatorio");
    }

    [Fact]
    public async Task Create_ConSlugDuplicado_ServiceLanzaConflict_Retorna409()
    {
        // ARRANGE
        var mensajeError = "El slug mi-slug ya está en uso.";

        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ThrowsAsync(new InvalidOperationException(mensajeError));

        var dto = new EmpresaRequestDto
        {
            Nombre = "Nueva Empresa",
            Slug = "mi-slug",
            Plan = "starter"
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmpresaResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Message.Should().Contain("ya está en uso");
    }

    [Fact]
    public async Task Create_ConPlanInvalido_ServiceLanzaValidation_Retorna400()
    {
        // ARRANGE
        var mensajeError = "El plan debe ser starter, professional o enterprise";
        var validationEx = CreateValidationException(mensajeError, "Plan", mensajeError);

        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ThrowsAsync(validationEx);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Empresa Valida",
            Slug = "empresa-valida",
            Plan = "premium"
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmpresaResponseDto>>();
        content.Should().NotBeNull();
        content.Errors.Should().ContainSingle().Which.Should().Contain("starter");
    }

    [Fact]
    public async Task Create_RespuestaContieneTimestampYCamposEsperados()
    {
        // ARRANGE
        var nuevoId = Guid.NewGuid();
        var respuestaDto = new EmpresaResponseDto
        {
            Id = nuevoId,
            Nombre = "Logistica Nacional",
            Slug = "logistica-nacional",
            Plan = "starter",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(respuestaDto);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Logistica Nacional",
            Slug = "logistica-nacional",
            Plan = "starter"
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<EmpresaResponseDto>>();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        content.Should().NotBeNull();
        content!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        content.Data.Nombre.Should().Be("Logistica Nacional");
        content.Data.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DtoSeMapeaCorrectamenteAlServicio()
    {
        // ARRANGE
        var nuevoId = Guid.NewGuid();
        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(new EmpresaResponseDto { Id = nuevoId });

        var dto = new EmpresaRequestDto
        {
            Nombre = "Empresa Prueba XYZ",
            Slug = "empresa-prueba-xyz",
            Plan = "enterprise"
        };

        // ACT
        await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        _mockService.Verify(s => s.CrearAsync(It.Is<EmpresaRequestDto>(d =>
            d.Nombre == "Empresa Prueba XYZ" &&
            d.Slug == "empresa-prueba-xyz" &&
            d.Plan == "enterprise")), Times.Once);
    }

    [Fact]
    public async Task Create_PlantaPredeterminadaSiNoSeSpecificalPlan()
    {
        // ARRANGE
        var nuevoId = Guid.NewGuid();
        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(new EmpresaResponseDto { Id = nuevoId });

        var dto = new EmpresaRequestDto
        {
            Nombre = "Sin Plan Explicito",
            Slug = ""
        };

        // ACT
        await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        _mockService.Verify(s => s.CrearAsync(It.Is<EmpresaRequestDto>(d =>
            d.Plan == "starter")), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // GET /api/empresas/{id} — Buscar Empresa por ID
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ElStubRetorna200OK()
    {
        // ARRANGE — HU-001: GetById actualmente es un stub que retorna 200 incluso con ID no existente.
        // Este test documenta el comportamiento actual.
        var id = Guid.NewGuid();

        // ACT
        var response = await _client.GetAsync($"/api/empresas/{id}");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ConGuidIdInvalido_Retorna400O404()
    {
        // ARRANGE — El constraint {id:guid} del framework valida el formato del GUID.
        var invalidoId = "no-es-guid";

        // ACT
        var response = await _client.GetAsync($"/api/empresas/{invalidoId}");

        // ASSERT
        (response.StatusCode == HttpStatusCode.BadRequest ||
         response.StatusCode == HttpStatusCode.NotFound)
            .Should().BeTrue("porque no-es-guid no pasa el constraint {id:guid}");
    }

    [Fact]
    public async Task GetById_SinAutenticacion_Retorna401()
    {
        // ARRANGE — El endpoint [Authorize] requiere token válido.
        var sinAuth = new HttpClient { BaseAddress = _client.BaseAddress };
        var id = Guid.NewGuid();

        // ACT
        var response = await sinAuth.GetAsync($"/api/empresas/{id}");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════
    // Scenarios Operacionales
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task Create_ConSlugPersonalizado_EsPasadoAlServicio()
    {
        // ARRANGE
        var nuevoId = Guid.NewGuid();
        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(new EmpresaResponseDto { Id = nuevoId });

        var dto = new EmpresaRequestDto
        {
            Nombre = "Mi Transportes",
            Slug = "mi-transportes-custom",
            Plan = "enterprise"
        };

        // ACT
        await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        _mockService.Verify(s => s.CrearAsync(It.Is<EmpresaRequestDto>(d =>
            d.Slug == "mi-transportes-custom")), Times.Once);
    }

    [Fact]
    public async Task Create_ResponseStatusCodeEsCreatedEnHeadOrFull()
    {
        // ARRANGE
        _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
            .ReturnsAsync(new EmpresaResponseDto { Id = Guid.NewGuid() });

        var dto = new EmpresaRequestDto { Nombre = "Valida", Plan = "starter" };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/empresas", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "el controller usa CreatedAtAction para retornar 201 con Location header");
    }

    [Fact]
    public async Task MultipleCreateCalls_UsoConsistenteDelMock()
    {
        // ARRANGE
        for (int i = 0; i < 3; i++)
        {
            _mockService.Reset();
            _mockService.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>()))
                .ReturnsAsync(new EmpresaResponseDto { Id = Guid.NewGuid(), Nombre = $"Empresa {i}" });
        }

        // ACT & ASSERT — cada llamada al servicio recibe el DTO correcto
        for (int i = 0; i < 3; i++)
        {
            var dto = new EmpresaRequestDto { Nombre = $"Empresa {i}", Plan = "starter" };
            await _client.PostAsJsonAsync("/api/empresas", dto);

            _mockService.Verify(s => s.CrearAsync(It.Is<EmpresaRequestDto>(d =>
                d.Nombre == $"Empresa {i}")), Times.Once);
        }
    }

    private static FluentValidation.ValidationException CreateValidationException(string message, string propertyName, string errorMessage)
    {
        var ex = new FluentValidation.ValidationException(message);
        var field = typeof(FluentValidation.ValidationException).GetField("_errors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var errors = (List<FluentValidation.Results.ValidationFailure>)field.GetValue(ex)!;
            errors.Add(new FluentValidation.Results.ValidationFailure(propertyName, errorMessage));
        }
        return ex;
    }
}
