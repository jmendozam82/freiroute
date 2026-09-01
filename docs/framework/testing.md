# Guía de Testing — SaaS con ASP.NET Core + Supabase

> **Estándar de calidad de software para el stack**
> Cubre: Unit Tests (BLL), Integration Tests (API), y estrategia de testing con agentes de IA.
> Patrón: TDD (Test-Driven Development) — el test primero, la implementación después.

---

## 1. Filosofía de Testing en este Stack

### El Triángulo de Tests

```
           /\
          /  \
         / E2E \      ← Pocos (Cypress/Playwright) — lentos, costosos
        /--------\
       /Integration\  ← Moderados (xUnit + TestServer) — API endpoints
      /------------\
     /  Unit Tests  \ ← Muchos (xUnit + Moq) — BLL Services, Validators
    /--------------/
```

### Regla 80-60-20

| Capa | Herramienta | Objetivo de cobertura |
|---|---|---|
| BLL (Unit Tests) | xUnit + Moq | ≥ 80% |
| API (Integration Tests) | xUnit + TestServer | ≥ 60% |
| E2E (End-to-End) | Playwright (opcional) | Flujos críticos |

### TDD en la Práctica con Agentes IA

```
1. @QA escribe el test que falla → el test describe el COMPORTAMIENTO esperado
2. @BackendDev implementa lo MÍNIMO necesario para que el test pase
3. @BackendDev refactoriza sin romper el test
4. @PM revisa que el test cubra el criterio de aceptación de la HU
```

---

## 2. Unit Tests — Business Logic Layer

### Setup del Proyecto de Tests

```xml
<!-- [Proyecto].BLL.Tests/[Proyecto].BLL.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/[Proyecto].BLL/[Proyecto].BLL.csproj" />
    <ProjectReference Include="../../src/[Proyecto].Entity/[Proyecto].Entity.csproj" />
    <ProjectReference Include="../../src/[Proyecto].DTO/[Proyecto].DTO.csproj" />
  </ItemGroup>
</Project>
```

### Patrón Estándar de Unit Test (AAA)

```csharp
namespace [Proyecto].BLL.Tests;

public class ProductoServiceTests
{
    // Mocks de dependencias
    private readonly Mock<IProductoRepository> _repositoryMock;
    private readonly Mock<ILogger<ProductoService>> _loggerMock;
    private readonly ProductoService _service;

    public ProductoServiceTests()
    {
        _repositoryMock = new Mock<IProductoRepository>();
        _loggerMock = new Mock<ILogger<ProductoService>>();
        _service = new ProductoService(_repositoryMock.Object, _loggerMock.Object);
    }

    // ─── NAMING CONVENTION ───────────────────────────────────────
    // [Método]_[Escenario]_[ResultadoEsperado]
    // Ejemplos:
    //   GetAllAsync_CuandoExistenProductos_RetornaListaCompleta
    //   CreateAsync_CuandoNombreDuplicado_LanzaBusinessException
    //   DeactivateAsync_CuandoProductoNoExiste_RetornaFalse

    [Fact]
    public async Task GetAllAsync_CuandoExistenProductos_RetornaListaCompleta()
    {
        // ── ARRANGE ──
        var tenantId = Guid.NewGuid();
        var productosEsperados = new List<Producto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Producto A", TenantId = tenantId, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Producto B", TenantId = tenantId, Activo = true }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(tenantId))
            .ReturnsAsync(productosEsperados);

        // ── ACT ──
        var result = await _service.GetAllAsync(tenantId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Nombre.Should().Be("Producto A");

        _repositoryMock.Verify(r => r.GetAllAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoNombreEsNuloOVacio_LanzaValidationException()
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto { Nombre = "", Precio = 100 };
        var tenantId = Guid.NewGuid();

        // ── ACT & ASSERT ──
        var act = async () => await _service.CreateAsync(dto, tenantId);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*nombre*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateAsync_CuandoPrecioEsMenorOIgualACero_LanzaValidationException(decimal precio)
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto { Nombre = "Producto válido", Precio = precio };
        var tenantId = Guid.NewGuid();

        // ── ACT & ASSERT ──
        var act = async () => await _service.CreateAsync(dto, tenantId);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoProductoExiste_RetornaTrue()
    {
        // ── ARRANGE ──
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.DeactivateAsync(id, tenantId))
            .ReturnsAsync(true);

        // ── ACT ──
        var result = await _service.DeactivateAsync(id, tenantId);

        // ── ASSERT ──
        result.Should().BeTrue();
    }
}
```

### Tests de FluentValidation

```csharp
public class ProductoValidatorTests
{
    private readonly ProductoValidator _validator = new();

    [Fact]
    public void Validate_CuandoTodosLosCamposValidos_NoHayErrores()
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto válido",
            Descripcion = "Descripción del producto",
            Precio = 150.00m
        };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CuandoNombreVacio_TieneError()
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto { Nombre = "" };

        // ── ACT ──
        var result = _validator.Validate(dto);

        // ── ASSERT ──
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}
```

---

## 3. Integration Tests — API Endpoints

### Setup del Proyecto de Integration Tests

```xml
<!-- [Proyecto].API.Tests/[Proyecto].API.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/[Proyecto].API/[Proyecto].API.csproj" />
  </ItemGroup>
</Project>
```

### WebApplicationFactory para Tests de Integración

```csharp
// Tests/Shared/TestWebApplicationFactory.cs
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazar servicios reales con mocks para tests
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IProductoService));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddScoped<IProductoService>(_ =>
            {
                var mock = new Mock<IProductoService>();
                mock.Setup(s => s.GetAllAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductoResponseDto>
                    {
                        new() { Id = Guid.NewGuid(), Nombre = "Test Producto" }
                    });
                return mock.Object;
            });
        });
    }
}
```

### Test de Integración de API Controller

```csharp
public class ProductosControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _validJwt;

    public ProductosControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _validJwt = JwtTestHelper.GenerateTestToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            permisos: new[] { "productos:read", "productos:create" });
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _validJwt);
    }

    [Fact]
    public async Task GetAll_CuandoUsuarioAutenticado_Retorna200ConLista()
    {
        // ── ACT ──
        var response = await _client.GetAsync("/api/productos");

        // ── ASSERT ──
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductoResponseDto>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        // ── ARRANGE ──
        var clientSinAuth = new HttpClient { BaseAddress = _client.BaseAddress };

        // ── ACT ──
        var response = await clientSinAuth.GetAsync("/api/productos");

        // ── ASSERT ──
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_CuandoDtoValido_Retorna201()
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto { Nombre = "Nuevo Producto", Precio = 99.99m };

        // ── ACT ──
        var response = await _client.PostAsJsonAsync("/api/productos", dto);

        // ── ASSERT ──
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_CuandoDtoInvalido_Retorna400()
    {
        // ── ARRANGE ──
        var dto = new ProductoRequestDto { Nombre = "" }; // Nombre vacío = inválido

        // ── ACT ──
        var response = await _client.PostAsJsonAsync("/api/productos", dto);

        // ── ASSERT ──
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

---

## 4. Checklist de Tests por Módulo

Para cada módulo nuevo, verificar que existan los siguientes tests:

### BLL Tests (Unit)

- [ ] `GetAllAsync_CuandoExistenRegistros_RetornaLista`
- [ ] `GetAllAsync_CuandoNoExistenRegistros_RetornaListaVacia`
- [ ] `GetByIdAsync_CuandoIdValido_RetornaRegistro`
- [ ] `GetByIdAsync_CuandoIdInexistente_RetornaNull`
- [ ] `CreateAsync_CuandoDtoValido_RetornaId`
- [ ] `CreateAsync_CuandoCampoObligatorioVacio_LanzaValidationException`
- [ ] `UpdateAsync_CuandoDtoValido_RetornaTrue`
- [ ] `UpdateAsync_CuandoRegistroInexistente_RetornaFalse`
- [ ] `DeactivateAsync_CuandoRegistroExiste_RetornaTrue`
- [ ] `DeactivateAsync_CuandoRegistroInexistente_RetornaFalse`

### Validator Tests (Unit)

- [ ] `Validate_CuandoTodosLosCamposValidos_NoHayErrores`
- [ ] `Validate_CuandoCampoObligatorioVacio_TieneError`
- [ ] `Validate_CuandoCampoExcedeLongitudMaxima_TieneError`

### API Tests (Integration)

- [ ] `GetAll_CuandoAutenticado_Retorna200`
- [ ] `GetAll_SinToken_Retorna401`
- [ ] `GetAll_ConTokenSinPermiso_Retorna403`
- [ ] `GetById_CuandoIdExiste_Retorna200`
- [ ] `GetById_CuandoIdInexistente_Retorna404`
- [ ] `Create_CuandoDtoValido_Retorna201`
- [ ] `Create_CuandoDtoInvalido_Retorna400`
- [ ] `Create_SinPermiso_Retorna403`
- [ ] `Update_CuandoDtoValido_Retorna200`
- [ ] `Update_CuandoIdInexistente_Retorna404`
- [ ] `Deactivate_CuandoRegistroExiste_Retorna200`

---

## 5. Comandos de Testing

```bash
# Ejecutar TODOS los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Ejecutar solo los tests de BLL
dotnet test tests/[Proyecto].BLL.Tests

# Ejecutar solo los tests de API
dotnet test tests/[Proyecto].API.Tests

# Ejecutar un test específico
dotnet test --filter "DisplayName~GetAllAsync_CuandoExistenRegistros"

# Ver reporte de cobertura (requiere reportgenerator)
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

---

## 6. Convenciones de Naming para Tests

| Patrón | Ejemplo |
|---|---|
| `[Método]_[Escenario]_[Resultado]` | `GetAllAsync_CuandoExistenRegistros_RetornaLista` |
| `[Método]_[Escenario]_Lanza[Excepcion]` | `CreateAsync_CuandoNombreVacio_LanzaValidationException` |
| `[Endpoint]_[Condición]_Retorna[Código]` | `GetAll_SinToken_Retorna401` |

---

## 7. Datos de Prueba (Test Data)

### Patrón Builder para Test Data

```csharp
// Tests/Builders/ProductoBuilder.cs
public class ProductoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private string _nombre = "Producto de Prueba";
    private decimal _precio = 100m;
    private bool _activo = true;

    public ProductoBuilder ConId(Guid id) { _id = id; return this; }
    public ProductoBuilder ConTenant(Guid tenantId) { _tenantId = tenantId; return this; }
    public ProductoBuilder ConNombre(string nombre) { _nombre = nombre; return this; }
    public ProductoBuilder Inactivo() { _activo = false; return this; }

    public Producto Build() => new()
    {
        Id = _id,
        TenantId = _tenantId,
        Nombre = _nombre,
        Precio = _precio,
        Activo = _activo,
        FechaCreacion = DateTime.UtcNow
    };
}

// Uso en tests:
var producto = new ProductoBuilder()
    .ConTenant(tenantId)
    .ConNombre("Laptop Dell")
    .Build();
```

---

*testing.md — Guía de testing para proyectos SaaS con ASP.NET Core*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*
