---
description: TDD con xUnit, Moq y FluentAssertions para Freiroute TMS. Úsalo para escribir tests unitarios BLL y tests de integración API, verificar cobertura de código (BLL ≥80%, API ≥60%), y validar criterios de aceptación de Historias de Usuario incluyendo RLS, soft delete y permisos granulares.
---

# Skill: Testing — TDD con xUnit, Moq y FluentAssertions

## Filosofía TDD en Freiroute

```
1. Escribir test que FALLA (Red)
2. Implementar el código mínimo que hace pasar el test (Green)
3. Refactorizar manteniendo los tests en verde (Refactor)
```

## Unit Tests BLL — Patrón Completo

```csharp
// tests/Freiroute.BLL.Tests/[Modulo]ServiceTests.cs
namespace Freiroute.BLL.Tests;

public class [Modulo]ServiceTests
{
    // ── Mocks ────────────────────────────────────────────────────────
    private readonly Mock<I[Modulo]Repository> _repositoryMock;
    private readonly Mock<ILogger<[Modulo]Service>> _loggerMock;
    private readonly [Modulo]Service _service;
    private readonly Guid _testEmpresaId = Guid.NewGuid();

    public [Modulo]ServiceTests()
    {
        _repositoryMock = new Mock<I[Modulo]Repository>();
        _loggerMock = new Mock<ILogger<[Modulo]Service>>();
        var validator = new [Modulo]RequestDtoValidator();

        _service = new [Modulo]Service(
            _repositoryMock.Object,
            validator,
            _loggerMock.Object);
    }

    // ── GetAllAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ShouldReturnSuccessWithData_WhenRecordsExist()
    {
        // Arrange
        var expectedData = new List<[Modulo]ResponseDto>
        {
            new() { Id = Guid.NewGuid(), EmpresaId = _testEmpresaId, Nombre = "Test 1", Activo = true },
            new() { Id = Guid.NewGuid(), EmpresaId = _testEmpresaId, Nombre = "Test 2", Activo = true }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(_testEmpresaId))
                       .ReturnsAsync(expectedData);

        // Act
        var result = await _service.GetAllAsync(_testEmpresaId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().AllSatisfy(d => d.EmpresaId.Should().Be(_testEmpresaId));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSuccessWithEmptyList_WhenNoRecordsExist()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(_testEmpresaId))
                       .ReturnsAsync(Enumerable.Empty<[Modulo]ResponseDto>());

        // Act
        var result = await _service.GetAllAsync(_testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(_testEmpresaId))
                       .ThrowsAsync(new Exception("DB connection error"));

        // Act
        var result = await _service.GetAllAsync(_testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // ── CreateAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccessWithId_WhenDtoIsValid()
    {
        // Arrange
        var dto = new [Modulo]RequestDto { Nombre = "Nuevo [Modulo]" };
        var expectedId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.CreateAsync(dto, _testEmpresaId))
                       .ReturnsAsync(expectedId);

        // Act
        var result = await _service.CreateAsync(dto, _testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(expectedId);
        _repositoryMock.Verify(r => r.CreateAsync(dto, _testEmpresaId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationFailure_WhenNombreIsEmpty()
    {
        // Arrange
        var dto = new [Modulo]RequestDto { Nombre = string.Empty };

        // Act
        var result = await _service.CreateAsync(dto, _testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<[Modulo]RequestDto>(), It.IsAny<Guid>()),
                               Times.Never);
    }

    // ── DeactivateAsync (Soft Delete) ───────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ShouldReturnSuccess_WhenRecordExists()
    {
        // Arrange
        var recordId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeactivateAsync(recordId, _testEmpresaId))
                       .ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateAsync(recordId, _testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFailure_WhenRecordNotFound()
    {
        // Arrange
        var recordId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeactivateAsync(recordId, _testEmpresaId))
                       .ReturnsAsync(false);

        // Act
        var result = await _service.DeactivateAsync(recordId, _testEmpresaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // ── Aislamiento Multi-Tenant ────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenRecordBelongsToOtherTenant()
    {
        // Arrange
        var recordId = Guid.NewGuid();
        var otroTenantId = Guid.NewGuid(); // ID de OTRO tenant
        _repositoryMock.Setup(r => r.GetByIdAsync(recordId, otroTenantId))
                       .ReturnsAsync(([ Modulo]ResponseDto?)null); // RLS/filtro bloquea acceso

        // Act
        var result = await _service.GetByIdAsync(recordId, otroTenantId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Verificar que NO se retornan datos de otro tenant
        result.Data.Should().BeNull();
    }
}
```

## Integration Tests API — Patrón con WebApplicationFactory

```csharp
// tests/Freiroute.API.Tests/[Modulo]ControllerTests.cs
namespace Freiroute.API.Tests;

public class [Modulo]ControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly string _validJwt;

    public [Modulo]ControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Reemplazar repositorio real con mock
                var mockRepo = new Mock<I[Modulo]Repository>();
                services.AddScoped<I[Modulo]Repository>(_ => mockRepo.Object);
            });
        }).CreateClient();

        _validJwt = GenerateTestJwt(empresaId: Guid.NewGuid(), rol: "ADMIN");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithValidJwt()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validJwt);

        // Act
        var response = await _client.GetAsync("/api/[modulo]");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<[Modulo]ResponseDto>>>();
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldReturn401_WithoutJwt()
    {
        // Act
        var response = await _client.GetAsync("/api/[modulo]");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithValidDto()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validJwt);
        var dto = new [Modulo]RequestDto { Nombre = "Test [Modulo]" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/[modulo]", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WithInvalidDto()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validJwt);
        var dto = new [Modulo]RequestDto { Nombre = string.Empty }; // Inválido

        // Act
        var response = await _client.PostAsJsonAsync("/api/[modulo]", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string GenerateTestJwt(Guid empresaId, string rol)
    {
        // Generar JWT de prueba con claims necesarios
        // Implementar según configuración de JWT del proyecto
        throw new NotImplementedException("Implementar helper de JWT de prueba");
    }
}
```

## Comandos para Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Solo BLL Tests con cobertura
dotnet test tests/Freiroute.BLL.Tests/ \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/BLL

# Solo API Tests con cobertura
dotnet test tests/Freiroute.API.Tests/ \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/API

# Generar reporte HTML de cobertura
reportgenerator \
    -reports:**/coverage.cobertura.xml \
    -targetdir:TestResults/Report \
    -reporttypes:Html

# Tests con output detallado
dotnet test --verbosity detailed

# Filtrar por categoría o nombre
dotnet test --filter "FullyQualifiedName~[Modulo]ServiceTests"
```

## Umbrales de Cobertura Obligatorios

| Proyecto | Umbral mínimo | Medida |
|---|---|---|
| `Freiroute.BLL.Tests` | ≥ **80%** | Líneas |
| `Freiroute.API.Tests` | ≥ **60%** | Endpoints críticos |

El pipeline CI bloqueará el merge si no se alcanzan estos umbrales.

## Convenciones de Nomenclatura de Tests

```
[Método]_Should[Resultado]_When[Condición]

Ejemplos:
✅ GetAllAsync_ShouldReturnSuccessWithData_WhenRecordsExist
✅ CreateAsync_ShouldReturnValidationFailure_WhenNombreIsEmpty
✅ DeactivateAsync_ShouldReturnFailure_WhenRecordNotFound
✅ GetByIdAsync_ShouldReturnNull_WhenRecordBelongsToOtherTenant
```
