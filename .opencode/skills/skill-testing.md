# Skill: @QA (Ingeniero de Calidad Freiroute TMS)

## Rol
**@QA** es responsable de garantizar la calidad del sistema mediante tests unitarios de la BLL, tests de integración de la API, y la validación de criterios de aceptación de cada Historia de Usuario. Actúa después de @BackendDev y antes de que @PM apruebe el PR.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer spec.md del módulo — verificar criterios de aceptación
3. Revisar la implementación de @BackendDev (BLL Service + API Controller)
4. Ejecutar dotnet build — debe estar sin warnings antes de escribir tests
```

---

## Estructura de Tests

```
tests/
├── Freiroute.BLL.Tests/
│   ├── Services/
│   │   ├── [Modulo]ServiceTests.cs
│   │   └── EmbarqueServiceTests.cs
│   └── Validators/
│       ├── [Modulo]ValidatorTests.cs
│       └── EmbarqueValidatorTests.cs
└── Freiroute.API.Tests/
    ├── Controllers/
    │   └── [Modulo]ControllerTests.cs
    ├── Helpers/
    │   └── JwtTestHelper.cs
    └── TestWebApplicationFactory.cs
```

### Paquetes NuGet Requeridos

```xml
<!-- Freiroute.BLL.Tests.csproj -->
<PackageReference Include="xunit" Version="2.8.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />
<PackageReference Include="coverlet.collector" Version="6.0.*" />

<!-- Freiroute.API.Tests.csproj -->
<PackageReference Include="xunit" Version="2.8.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />
<PackageReference Include="Moq" Version="4.20.*" />
```

---

## Tests Unitarios BLL

### Patrón Base AAA (Arrange–Act–Assert)

```csharp
// tests/Freiroute.BLL.Tests/Services/[Modulo]ServiceTests.cs
namespace Freiroute.BLL.Tests.Services;

public class [Modulo]ServiceTests
{
    // ── Mocks y sujeto bajo prueba ────────────────────────────────
    private readonly Mock<I[Modulo]Repository> _repoMock = new();
    private readonly Mock<ILogger<[Modulo]Service>> _loggerMock = new();
    private readonly [Modulo]Validator _validator = new();
    private readonly [Modulo]Service _sut;

    public [Modulo]ServiceTests()
    {
        _sut = new [Modulo]Service(_repoMock.Object, _validator, _loggerMock.Object);
    }

    // ── HAPPY PATH: GetAllAsync ───────────────────────────────────
    [Fact]
    public async Task GetAllAsync_CuandoExistenRegistros_RetornaListaMapeada()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        var entidades = new List<[Modulo]>
        {
            new() { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "Carrier A", Activo = true },
            new() { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "Carrier B", Activo = true }
        };
        _repoMock.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync(entidades);

        // Act
        var result = await _sut.GetAllAsync(empresaId);

        // Assert
        result.Should().NotBeNull().And.HaveCount(2);
        result.First().Nombre.Should().Be("Carrier A");
        _repoMock.Verify(r => r.GetAllAsync(empresaId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_CuandoNoExistenRegistros_RetornaListaVacia()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync([]);

        // Act
        var result = await _sut.GetAllAsync(empresaId);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    // ── HAPPY PATH: GetByIdAsync ──────────────────────────────────
    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaDto()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var entidad = new [Modulo] { Id = id, EmpresaId = empresaId, Nombre = "Test", Activo = true };

        _repoMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(entidad);

        var result = await _sut.GetByIdAsync(id, empresaId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Nombre.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                 .ReturnsAsync(([ Modulo]?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── HAPPY PATH: CreateAsync ───────────────────────────────────
    [Fact]
    public async Task CreateAsync_CuandoDtoValido_RetornaDtoCreado()
    {
        var empresaId = Guid.NewGuid();
        var nuevoId   = Guid.NewGuid();
        var dto = new [Modulo]RequestDto { Nombre = "Nuevo Registro" };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<[Modulo]>())).ReturnsAsync(nuevoId);
        _repoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId))
                 .ReturnsAsync(new [Modulo] { Id = nuevoId, EmpresaId = empresaId, Nombre = "Nuevo Registro", Activo = true });

        var result = await _sut.CreateAsync(dto, empresaId);

        result.Should().NotBeNull();
        result.Id.Should().Be(nuevoId);
        result.Nombre.Should().Be("Nuevo Registro");
        _repoMock.Verify(r => r.CreateAsync(It.Is<[Modulo]>(e =>
            e.EmpresaId == empresaId && e.Nombre == "Nuevo Registro")), Times.Once);
    }

    // ── ERROR PATH: CreateAsync con datos inválidos ───────────────
    [Fact]
    public async Task CreateAsync_CuandoNombreVacio_LanzaValidationException()
    {
        var dto = new [Modulo]RequestDto { Nombre = "" };

        var act = async () => await _sut.CreateAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*nombre*");
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<[Modulo]>()), Times.Never);
    }

    // ── HAPPY PATH: UpdateAsync ───────────────────────────────────
    [Fact]
    public async Task UpdateAsync_CuandoExisteYDtoValido_RetornaActualizado()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var existente = new [Modulo] { Id = id, EmpresaId = empresaId, Nombre = "Original", Activo = true };
        var dto = new [Modulo]RequestDto { Nombre = "Actualizado" };

        _repoMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(existente);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<[Modulo]>())).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(id, dto, empresaId);

        result.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdateAsync(It.Is<[Modulo]>(e => e.Nombre == "Actualizado")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                 .ReturnsAsync(([Modulo]?)null);

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), new [Modulo]RequestDto { Nombre = "X" }, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<[Modulo]>()), Times.Never);
    }

    // ── HAPPY PATH: DeactivateAsync ───────────────────────────────
    [Fact]
    public async Task DeactivateAsync_CuandoExiste_RetornaTrue()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repoMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(true);

        var result = await _sut.DeactivateAsync(id, empresaId);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeactivateAsync(id, empresaId), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_RetornaFalse()
    {
        _repoMock.Setup(r => r.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                 .ReturnsAsync(false);

        var result = await _sut.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }
}
```

---

## Tests de Validadores

```csharp
// tests/Freiroute.BLL.Tests/Validators/[Modulo]ValidatorTests.cs
namespace Freiroute.BLL.Tests.Validators;

public class [Modulo]ValidatorTests
{
    private readonly [Modulo]Validator _validator = new();

    [Fact]
    public void Validate_CuandoTodosLosCamposValidos_NoTieneErrores()
    {
        var dto = new [Modulo]RequestDto { Nombre = "Registro válido" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_CuandoNombreVacioONulo_TieneError(string? nombre)
    {
        var dto = new [Modulo]RequestDto { Nombre = nombre! };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_CuandoNombreExcede200Caracteres_TieneError()
    {
        var dto = new [Modulo]RequestDto { Nombre = new string('A', 201) };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Nombre" && e.ErrorMessage.Contains("200"));
    }

    // ── Tests específicos de dominio TMS ─────────────────────────
    [Theory]
    [InlineData("FTL")]
    [InlineData("LTL")]
    [InlineData("AEREO")]
    [InlineData("MARITIMO")]
    public void Validate_CuandoModoTransporteValido_NoTieneError(string modo)
    {
        var dto = new EmbarqueRequestDto { /* campos base */ ModoTransporte = modo };

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == "ModoTransporte");
    }

    [Fact]
    public void Validate_CuandoFechaEntregaAnteriorAPickup_TieneError()
    {
        var dto = new EmbarqueRequestDto
        {
            FechaPickupPlanificada  = DateTime.Today.AddDays(3),
            FechaEntregaRequerida   = DateTime.Today.AddDays(1) // Antes del pickup
        };

        var result = _validator.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "FechaEntregaRequerida");
    }
}
```

---

## Tests de Integración API

### TestWebApplicationFactory

```csharp
// tests/Freiroute.API.Tests/TestWebApplicationFactory.cs
namespace Freiroute.API.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<I[Modulo]Service> [Modulo]ServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazar el servicio real con el mock
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(I[Modulo]Service));
            if (descriptor != null) services.Remove(descriptor);

            services.AddScoped<I[Modulo]Service>(_ => [Modulo]ServiceMock.Object);
        });
        builder.UseEnvironment("Testing");
    }
}
```

### JwtTestHelper

```csharp
// tests/Freiroute.API.Tests/Helpers/JwtTestHelper.cs
namespace Freiroute.API.Tests.Helpers;

public static class JwtTestHelper
{
    public const string SecretKey = "freiroute-test-secret-key-256bits-min";

    public static string GenerarToken(
        Guid? userId     = null,
        Guid? empresaId  = null,
        Guid? perfilId   = null,
        string[]? permisos = null,
        string rol       = "OPERADOR")
    {
        var claims = new List<Claim>
        {
            new("user_id",       (userId    ?? Guid.NewGuid()).ToString()),
            new("empresa_id",    (empresaId ?? Guid.NewGuid()).ToString()),
            new("perfil_id",     (perfilId  ?? Guid.NewGuid()).ToString()),
            new("tipo_usuario",  rol),
            new("nombre",        "Usuario Test"),
            new(ClaimTypes.Role, rol)
        };

        foreach (var p in permisos ?? [])
            claims.Add(new Claim("permisos", p));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             "freiroute-api",
            audience:           "freiroute-client",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Tokens predefinidos para escenarios comunes
    public static string TokenSuperAdmin => GenerarToken(rol: "SUPER_ADMIN",
        permisos: ["*:read", "*:create", "*:update"]);

    public static string TokenAdmin => GenerarToken(rol: "ADMIN",
        permisos: ["[modulo]:read", "[modulo]:create", "[modulo]:update"]);

    public static string TokenSoloLectura => GenerarToken(rol: "OPERADOR",
        permisos: ["[modulo]:read"]);
}
```

### Tests de Controller

```csharp
// tests/Freiroute.API.Tests/Controllers/[Modulo]ControllerTests.cs
namespace Freiroute.API.Tests.Controllers;

public class [Modulo]ControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient CrearCliente(string? token = null)
    {
        var client = _factory.CreateClient();
        if (token != null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public [Modulo]ControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Autenticación y autorización ─────────────────────────────
    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = CrearCliente();
        var resp   = await client.GetAsync("/api/[modulo]");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_SinPermiso_Retorna403()
    {
        var client = CrearCliente(JwtTestHelper.GenerarToken(permisos: []));
        var resp   = await client.GetAsync("/api/[modulo]");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET ALL ───────────────────────────────────────────────────
    [Fact]
    public async Task GetAll_ConPermiso_Retorna200ConLista()
    {
        // Arrange
        var items = new List<[Modulo]ResponseDto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Item A", Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Item B", Activo = true }
        };
        _factory.[Modulo]ServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(items);

        var client = CrearCliente(JwtTestHelper.TokenAdmin);

        // Act
        var resp = await client.GetAsync("/api/[modulo]");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<[Modulo]ResponseDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().HaveCount(2);
    }

    // ── GET BY ID ─────────────────────────────────────────────────
    [Fact]
    public async Task GetById_CuandoExiste_Retorna200()
    {
        var id   = Guid.NewGuid();
        var item = new [Modulo]ResponseDto { Id = id, Nombre = "Test", Activo = true };
        _factory.[Modulo]ServiceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<Guid>()))
            .ReturnsAsync(item);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .GetAsync($"/api/[modulo]/{id}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Retorna404()
    {
        _factory.[Modulo]ServiceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(([Modulo]ResponseDto?)null);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .GetAsync($"/api/[modulo]/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST CREATE ───────────────────────────────────────────────
    [Fact]
    public async Task Create_CuandoDtoValido_Retorna201()
    {
        var nuevoId  = Guid.NewGuid();
        var dto      = new [Modulo]RequestDto { Nombre = "Nuevo" };
        var creado   = new [Modulo]ResponseDto { Id = nuevoId, Nombre = "Nuevo", Activo = true };

        _factory.[Modulo]ServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<[Modulo]RequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(creado);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .PostAsJsonAsync("/api/[modulo]", dto);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<[Modulo]ResponseDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(nuevoId);
    }

    [Fact]
    public async Task Create_SinPermisoCreate_Retorna403()
    {
        var dto  = new [Modulo]RequestDto { Nombre = "Test" };
        var resp = await CrearCliente(JwtTestHelper.TokenSoloLectura)
                         .PostAsJsonAsync("/api/[modulo]", dto);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_CuandoDtoInvalido_Retorna400()
    {
        _factory.[Modulo]ServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<[Modulo]RequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new ValidationException("El nombre es obligatorio"));

        var dto  = new [Modulo]RequestDto { Nombre = "" };
        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .PostAsJsonAsync("/api/[modulo]", dto);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT UPDATE ────────────────────────────────────────────────
    [Fact]
    public async Task Update_CuandoExisteYDtoValido_Retorna200()
    {
        var id        = Guid.NewGuid();
        var dto       = new [Modulo]RequestDto { Nombre = "Actualizado" };
        var actualizado = new [Modulo]ResponseDto { Id = id, Nombre = "Actualizado", Activo = true };

        _factory.[Modulo]ServiceMock
            .Setup(s => s.UpdateAsync(id, It.IsAny<[Modulo]RequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(actualizado);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .PutAsJsonAsync($"/api/[modulo]/{id}", dto);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_CuandoNoExiste_Retorna404()
    {
        _factory.[Modulo]ServiceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<[Modulo]RequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException("No encontrado"));

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .PutAsJsonAsync($"/api/[modulo]/{Guid.NewGuid()}",
                                         new [Modulo]RequestDto { Nombre = "X" });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE (DEACTIVATE) ───────────────────────────────────────
    [Fact]
    public async Task Deactivate_CuandoExiste_Retorna200()
    {
        var id = Guid.NewGuid();
        _factory.[Modulo]ServiceMock
            .Setup(s => s.DeactivateAsync(id, It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .DeleteAsync($"/api/[modulo]/{id}/deactivate");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body!.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_CuandoNoExiste_Retorna404()
    {
        _factory.[Modulo]ServiceMock
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var resp = await CrearCliente(JwtTestHelper.TokenAdmin)
                         .DeleteAsync($"/api/[modulo]/{Guid.NewGuid()}/deactivate");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

---

## Tests Específicos del Dominio TMS

```csharp
// tests/Freiroute.BLL.Tests/Services/EmbarqueServiceTests.cs
public class EmbarqueServiceTests
{
    // ── State Machine ─────────────────────────────────────────────
    [Theory]
    [InlineData("DRAFT",       "CONFIRMED",   true)]
    [InlineData("DRAFT",       "CANCELLED",   true)]
    [InlineData("CONFIRMED",   "ASSIGNED",    true)]
    [InlineData("IN_TRANSIT",  "DELIVERED",   true)]
    [InlineData("DELIVERED",   "DRAFT",       false)]  // transición inválida
    [InlineData("CLOSED",      "IN_TRANSIT",  false)]  // transición inválida
    [InlineData("CANCELLED",   "CONFIRMED",   false)]  // transición inválida
    public async Task CambiarEstado_ValidaTransicion(string desde, string hacia, bool debeSerValida)
    {
        // Arrange
        var id        = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var embarque  = new Embarque { Id = id, EmpresaId = empresaId, Estado = desde, Activo = true };

        _repoMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(embarque);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Embarque>())).ReturnsAsync(true);

        // Act & Assert
        if (debeSerValida)
        {
            var result = await _sut.CambiarEstadoAsync(id, hacia, empresaId);
            result.Should().NotBeNull();
        }
        else
        {
            await FluentActions.Invoking(() => _sut.CambiarEstadoAsync(id, hacia, empresaId))
                .Should().ThrowAsync<BusinessException>();
        }
    }

    // ── Aislamiento multi-tenant ──────────────────────────────────
    [Fact]
    public async Task GetByIdAsync_NoRetornaDatosDeOtroTenant()
    {
        var id          = Guid.NewGuid();
        var empresaA    = Guid.NewGuid();
        var empresaB    = Guid.NewGuid();

        // El repo solo retorna si empresa coincide
        _repoMock.Setup(r => r.GetByIdAsync(id, empresaA))
                 .ReturnsAsync(new Embarque { Id = id, EmpresaId = empresaA });
        _repoMock.Setup(r => r.GetByIdAsync(id, empresaB))
                 .ReturnsAsync((Embarque?)null);

        var resultA = await _sut.GetByIdAsync(id, empresaA);
        var resultB = await _sut.GetByIdAsync(id, empresaB);

        resultA.Should().NotBeNull();
        resultB.Should().BeNull();
    }

    // ── OTD Calculation ───────────────────────────────────────────
    [Theory]
    [InlineData("2026-01-10", "2026-01-10", true)]   // Entrega a tiempo
    [InlineData("2026-01-10", "2026-01-09", true)]   // Entrega anticipada
    [InlineData("2026-01-10", "2026-01-11", false)]  // Entrega tardía
    public void CalcularOtd_SegunFechas(string requerida, string real, bool esperado)
    {
        var fechaRequerida = DateTime.Parse(requerida);
        var fechaReal      = DateTime.Parse(real);

        var otdCumplido = OtdCalculator.EsCumplido(fechaRequerida, fechaReal);

        otdCumplido.Should().Be(esperado);
    }
}
```

---

## Ejecutar Tests y Verificar Cobertura

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura (Coverlet)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Reporte HTML (requiere reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" \
                -targetdir:"./coverage/report" -reporttypes:Html

# Verificar umbrales mínimos
dotnet test /p:CollectCoverage=true \
            /p:CoverageDirectory=coverage \
            /p:Threshold=80 \
            /p:ThresholdType=line \
            /p:ThresholdStat=minimum
```

---

## Checklist de Entregable QA

**Tests Unitarios BLL (≥ 80% cobertura):**
- [ ] `GetAllAsync_CuandoExisten_RetornaLista`
- [ ] `GetAllAsync_CuandoNoExisten_RetornaVacia`
- [ ] `GetByIdAsync_CuandoExiste_RetornaDto`
- [ ] `GetByIdAsync_CuandoNoExiste_RetornaNull`
- [ ] `CreateAsync_CuandoValido_RetornaCreado`
- [ ] `CreateAsync_CuandoInvalido_LanzaValidationException`
- [ ] `UpdateAsync_CuandoExiste_RetornaActualizado`
- [ ] `UpdateAsync_CuandoNoExiste_LanzaKeyNotFoundException`
- [ ] `DeactivateAsync_CuandoExiste_RetornaTrue`
- [ ] `DeactivateAsync_CuandoNoExiste_RetornaFalse`
- [ ] Tests de Validator: campo obligatorio, max longitud, formato, reglas de dominio TMS
- [ ] Tests de State Machine (si el módulo tiene estados)
- [ ] Test de aislamiento multi-tenant

**Tests de Integración API (≥ 60% cobertura):**
- [ ] `GET /api/[modulo]` sin token → 401
- [ ] `GET /api/[modulo]` sin permiso → 403
- [ ] `GET /api/[modulo]` con permiso → 200 + lista
- [ ] `GET /api/[modulo]/{id}` existente → 200
- [ ] `GET /api/[modulo]/{id}` inexistente → 404
- [ ] `POST /api/[modulo]` válido → 201
- [ ] `POST /api/[modulo]` inválido → 400
- [ ] `POST /api/[modulo]` sin permiso → 403
- [ ] `PUT /api/[modulo]/{id}` existente → 200
- [ ] `PUT /api/[modulo]/{id}` inexistente → 404
- [ ] `DELETE /api/[modulo]/{id}/deactivate` existente → 200
- [ ] `DELETE /api/[modulo]/{id}/deactivate` inexistente → 404

**Calidad general:**
- [ ] `dotnet build` — cero warnings
- [ ] `dotnet test` — cero fallos
- [ ] Cobertura BLL ≥ 80% verificada con Coverlet
- [ ] Cobertura API ≥ 60% verificada con Coverlet
- [ ] Criterios de aceptación del spec.md verificados uno a uno

---

## Contexto Freiroute TMS

@QA valida que el TMS de transporte opera correctamente en escenarios críticos: aislamiento por `empresa_id`, transiciones de estado válidas en embarques, cálculo correcto de OTD, y seguridad de endpoints. Los módulos con mayor complejidad de tests son: Embarques (state machine), Carriers (score y contratos), Rutas (optimización), Track & Trace (GPS en tiempo real) y Freight Audit (conciliación financiera).
