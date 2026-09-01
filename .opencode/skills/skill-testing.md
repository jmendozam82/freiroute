# Skill: @QA (Quality Assurance freiroute TMS)

## Rol
**@QA** es el garante de calidad del sistema Freiroute TMS. Ejecuta y mantiene tests unitarios (BLL ≥80%) e integración (API ≥60%), valida criterios de aceptación de cada HU, y verifica que la cobertura cumpla los umbrales obligatorios antes de cada merge a `main`. Opera bajo filosofía TDD: el test que falla ES la especificación ejecutable. Actúa después de @BackendDev y aprueba o rechaza para @PM.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Revisar código implementado por @BackendDev
4. Verificar cobertura actual con `dotnet test --collect:"XPlat Code Coverage"`
5. Revisar PR pendiente y comentarios previos de QA
```

### 2. Posición en el Flujo de HU
```
@PM planifica Sprint
    → @Arquitecto define Entity + DTOs + Interfaces + ADR
    → @IngenieroDatos crea migración SQL + RLS
    → @BackendDev implementa BLL Service + FluentValidator + API Controller
    → @QA ← EJECUTA TESTS + VALIDA COBERTURA + CRITERIOS DE ACEPTACIÓN
    → @FrontendDev crea Vistas Razor con Design System Freiroute
    → @PM revisa checklist completo + aprueba PR
```

### 3. Filosofía TDD — Reglas No Negociables

#### Rule #1: El Test Primero
```
TDD Workflow obligatorio para cada método nuevo:
┌─────────────────────────────────────────────────────┐
│ 1. Escribir test que FALLA (Rojo)                  │
│ 2. Implementar lógica mínima para pasar el test     │
│ 3. Refactorizar manteniendo tests verdes (Verde)    │
│ 4. Repetir                                          │
└─────────────────────────────────────────────────────┘
```

#### Rule #2: Naming Convention Estricto
```
[Método]_[Escenario]_[ResultadoEsperado]

Ejemplos correctos:
  ✅ GetAllAsync_CuandoExistenRegistros_RetornaLista
  ✅ CreateAsync_CuandoDtoInvalido_LanzaValidationException
  ✅ DeactivateAsync_CuandoRegistroNoExiste_RetornaFalse
  ✅ UpdateAsync_CuandoEmpresaIdDiferente_NoActualiza

Ejemplos incorrectos (NUNCA):
  ❌ Test1
  ❌ prueba_crear
  ❌ getall
  ❌ test_validacion
```

#### Rule #3: Patrón AAA en Cada Test
```csharp
// ── Arrange ──────────────────────────────────────────────────────────
// Preparar datos, mocks, fixtures. Todo lo necesario ANTES de la acción.

// ── Act ──────────────────────────────────────────────────────────────
// UNA sola llamada al método bajo prueba. Nada más.

// ── Assert ───────────────────────────────────────────────────────────
// Verificar resultados con FluentAssertions. Nada de if/else manual.
```

### 4. Unit Tests — BLL Layer (≥ 80% Cobertura)

**Proyecto:** `tests/Freiroute.BLL.Tests/`  
**Herramientas:** xUnit + Moq + FluentAssertions  
**Cobertura mínima:** ≥ 80%

#### Estructura completa del Test Class
```csharp
namespace Freiroute.BLL.Tests.OrdenTests;

using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests unitarios para OrdenService — cubre reglas de negocio
/// de gestión de órdenes de transporte (EP-04).
/// </summary>
public class OrdenServiceTests
{
    private readonly Mock<IOrdenRepository> _repositoryMock;
    private readonly Mock<ILogger<OrdenService>> _loggerMock;
    private readonly OrdenService _service;

    // Constructor shared entre todos los tests del mismo clase
    public OrdenServiceTests()
    {
        _repositoryMock = new Mock<IOrdenRepository>();
        _loggerMock = new Mock<ILogger<OrdenService>>();
        _service = new OrdenService(_repositoryMock.Object, _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GETALL ASYNC — Casos positivos y negativos
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_CuandoExistenRegistros_RetornaListaCompleta()
    {
        // ARRANGE
        var empresaId = Guid.NewGuid();
        var ordenesEsperadas = new List<Orden>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                NumeroOrden = "ORD-2026-001",
                Estado = OrdenStatus.Confirmed,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                NumeroOrden = "ORD-2026-002",
                Estado = OrdenStatus.Draft,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(empresaId))
            .ReturnsAsync(ordenesEsperadas);

        // ACT
        var resultado = await _service.GetAllAsync(empresaId);

        // ASSERT
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);
        resultado.First().NumeroOrden.Should().Be("ORD-2026-001");
        _repositoryMock.Verify(r => r.GetAllAsync(empresaId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_CuandoNoHayRegistros_RetornaListaVacia()
    {
        // ARRANGE
        var empresaId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetAllAsync(empresaId))
            .ReturnsAsync(new List<Orden>());

        // ACT
        var resultado = await _service.GetAllAsync(empresaId);

        // ASSERT
        resultado.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GETBYID ASYNC
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_CuandoIdValido_RetornaOrden()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var orden = new Orden
        {
            Id = id,
            EmpresaId = empresaId,
            NumeroOrden = "ORD-2026-001",
            Activo = true
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(orden);

        // ACT
        var resultado = await _service.GetByIdAsync(id, empresaId);

        // ASSERT
        resultado.Should().NotBeNull();
        resultado!.NumeroOrden.Should().Be("ORD-2026-001");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoIdInexistente_RetornaNull()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync((Orden?)null);

        // ACT
        var resultado = await _service.GetByIdAsync(id, empresaId);

        // ASSERT
        resultado.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // CREATE ASYNC — Validaciones y reglas de negocio
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_CuandoNombreEsNuloOVacio_LanzaValidationException()
    {
        // ARRANGE
        var dto = new OrdenRequestDto { NombreCliente = "", MontoFlete = 150.00m };
        var empresaId = Guid.NewGuid();

        // ACT & ASSERT
        var act = async () => await _service.CreateAsync(dto, empresaId);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*nombre*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task CreateAsync_CuandoMontoNegativo_OIgualACero_LanzaValidationException(decimal monto)
    {
        // ARRANGE
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Transportes Express",
            MontoFlete = monto
        };
        var empresaId = Guid.NewGuid();

        // ACT & ASSERT
        var act = async () => await _service.CreateAsync(dto, empresaId);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_CuandoEstadoInicialNoEsDraft_LanzaBusinessException()
    {
        // ARRANGE
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Logística Nacional",
            Estado = OrdenStatus.InTransit, // Error: solo DRAFT permitido al crear
            MontoFlete = 500.00m
        };
        var empresaId = Guid.NewGuid();

        // ACT & ASSERT — la regla de negocio no permite crear en otro estado que no sea Draft
        var act = async () => await _service.CreateAsync(dto, empresaId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // Multi-tenant isolation test: empresa_id siempre viene del JWT
    [Fact]
    public async Task CreateAsync_EmpresaIdSiempreDelJWT_NoDelDTO()
    {
        // ARRANGE
        var empresaIdFromJwt = Guid.NewGuid();
        var dto = new OrdenRequestDto { NombreCliente = "Test" };
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Orden>()))
                       .ReturnsAsync(Guid.NewGuid());

        // ACT
        var resultado = await _service.CreateAsync(dto, empresaIdFromJwt);

        // ASSERT — verificar que se llamó al repositorio con el empresaId correcto
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<Orden>(o => o.EmpresaId == empresaIdFromJwt)), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // UPDATE ASYNC
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_CuandoDtoValido_YEntidadExiste_RetornaActualizado()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var ordenExistente = new Orden
        {
            Id = id, EmpresaId = empresaId,
            NumeroOrden = "ORD-2026-001", Estado = OrdenStatus.Draft, Activo = true
        };
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Nuevo Cliente", MontoFlete = 300.00m
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(ordenExistente);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Orden>())).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync(ordenExistente);

        // ACT
        var resultado = await _service.UpdateAsync(id, dto, empresaId);

        // ASSERT
        resultado.Should().NotBeNull();
        resultado.NombreCliente.Should().Be("Nuevo Cliente");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Orden>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoEntidadNoExiste_LanzaKeyNotFoundException()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, empresaId)).ReturnsAsync((Orden?)null);
        var dto = new OrdenRequestDto { NombreCliente = "Test" };

        // ACT & ASSERT
        var act = async () => await _service.UpdateAsync(id, dto, empresaId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // DEACTIVATE ASYNC (Soft Delete)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeactivateAsync_CuandoRegistroExiste_RetornaTrue()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(true);

        // ACT
        var resultado = await _service.DeactivateAsync(id, empresaId);

        // ASSERT
        resultado.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeactivateAsync(id, empresaId), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoRegistroInexistente_RetornaFalse()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(false);

        // ACT
        var resultado = await _service.DeactivateAsync(id, empresaId);

        // ASSERT
        resultado.Should().BeFalse();
    }

    // Nunca se llama DeleteAsync — ver ADR-005
    [Fact]
    public async Task DeactivateAsync_NuncaLlamaDelete_UsaSoloDeactivate()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(true);

        // ACT
        await _service.DeactivateAsync(id, empresaId);

        // ASSERT
        _repositoryMock.Verify(r => r.DeleteAsync(id), Times.Never);
    }
}
```

### 5. Validator Tests — FluentValidators

**Proyecto:** `tests/Freiroute.BLL.Tests/Validators/`

#### Validator Test Completo
```csharp
namespace Freiroute.BLL.Tests.OrdenTests.Validators;

using Freiroute.BLL.Validators;
using Freiroute.DTO.Orden;
using FluentAssertions;
using Xunit;

public class OrdenValidatorTests
{
    private readonly OrdenValidator _validator = new();

    [Fact]
    public void Validate_CuandoTodosLosCamposValidos_NoHayErrores()
    {
        // ARRANGE
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Transportes Express del Sur S.A.",
            MontoFlete = 1500.50m,
            PesoTotal = 2500.00m,
            Estado = OrdenStatus.Draft
        };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CuandoNombreVacio_TieneError()
    {
        // ARRANGE
        var dto = new OrdenRequestDto { NombreCliente = "" };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "NombreCliente");
    }

    [Fact]
    public void Validate_CuandoNombreExcedeMaximo_TieneError()
    {
        // ARRANGE
        var dto = new OrdenRequestDto { NombreCliente = new string('A', 201) };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "NombreCliente");
    }

    [Fact]
    public void Validate_CuandoMontoNegativo_TieneError()
    {
        // ARRANGE
        var dto = new OrdenRequestDto { MontoFlete = -500m };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "MontoFlete");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("INVALIDO")]
    [InlineData("PICKUP_SCHEDULED")]
    public void Validate_CuandoEstadoInvalido_TieneError(string? estado)
    {
        // ARRANGE
        var dto = new OrdenRequestDto { Estado = estado ?? "" };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Estado");
    }

    // Theory para múltiples valores válidos de estado
    [Theory]
    [InlineData(OrdenStatus.Draft)]
    [InlineData(OrdenStatus.Confirmed)]
    [InlineData(OrdenStatus.Assigned)]
    [InlineData(OrdenStatus.InTransit)]
    [InlineData(OrdenStatus.Delivered)]
    public void Validate_CuandoEstadoValido_NoHayError(string estado)
    {
        // ARRANGE
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Valid Test",
            MontoFlete = 100m,
            Estado = estado
        };

        // ACT
        var resultado = _validator.Validate(dto);

        // ASSERT
        resultado.IsValid.Should().BeTrue();
    }
}
```

### 6. Integration Tests — API Layer (≥ 60% Cobertura)

**Proyecto:** `tests/Freiroute.API.Tests/`  
**Herramientas:** xUnit + WebApplicationFactory + FluentAssertions  
**Cobertura mínima:** ≥ 60%

#### Test WebApplicationFactory
```csharp
// ── tests/Freiroute.API.Tests/TestWebApplicationFactory.cs ─────────────
namespace Freiroute.API.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazar services reales con mocks para aislamiento total
            var ordenDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOrdenService));
            if (ordenDescriptor != null)
                services.Remove(ordenDescriptor);

            services.AddScoped<IOrdenService>(_ =>
            {
                var mock = new Mock<IOrdenService>();
                mock.Setup(s => s.GetAllAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new List<OrdenResponseDto>
                    {
                        new() { Id = Guid.NewGuid(), NumeroOrden = "ORD-TEST-001", NombreCliente = "Test Corp" }
                    });
                return mock.Object;
            });
        });
    }
}
```

#### Controller Integration Tests
```csharp
// ── tests/Freiroute.API.Tests/OrdenControllerTests.cs ─────────────────
namespace Freiroute.API.Tests;

using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

public class OrdenControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _validJwt;

    public OrdenControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _validJwt = JwtTestHelper.GenerateTestToken(
            userId: Guid.NewGuid(),
            empresaId: Guid.NewGuid(),
            perfilId: Guid.NewGuid(),
            permisos: new[] { "ordenes:read", "ordenes:create", "ordenes:update" });

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validJwt);
    }

    // ── GET /api/ordenes ───────────────────────────────────────────────

    [Fact]
    public async Task GetAll_CuandoUsuarioAutenticado_Retorna200ConLista()
    {
        // ACT
        var response = await _client.GetAsync("/api/ordenes");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<OrdenResponseDto>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        // ARRANGE
        var clientSinAuth = new HttpClient { BaseAddress = _client.BaseAddress };

        // ACT
        var response = await clientSinAuth.GetAsync("/api/ordenes");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConTokenSinPermiso_Retorna403()
    {
        // ARRANGE
        var tokenSinPermiso = JwtTestHelper.GenerateTokenSinPermisos();
        var clientSinPermiso = new HttpClient { BaseAddress = _client.BaseAddress };
        clientSinPermiso.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenSinPermiso);

        // ACT
        var response = await clientSinPermiso.GetAsync("/api/ordenes");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/ordenes ──────────────────────────────────────────────

    [Fact]
    public async Task Create_CuandoDtoValido_Retorna201()
    {
        // ARRANGE
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Transportes Modernos",
            MontoFlete = 750.00m,
            Estado = OrdenStatus.Draft
        };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/ordenes", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_CuandoDtoInvalido_MensajeVacio_Retorna400()
    {
        // ARRANGE
        var dto = new OrdenRequestDto { NombreCliente = "", MontoFlete = 100 };

        // ACT
        var response = await _client.PostAsJsonAsync("/api/ordenes", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_SinToken_Retorna401()
    {
        // ARRANGE
        var sinAuth = new HttpClient { BaseAddress = _client.BaseAddress };
        var dto = new OrdenRequestDto { NombreCliente = "Test" };

        // ACT
        var response = await sinAuth.PostAsJsonAsync("/api/ordenes", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/ordenes/{id} ──────────────────────────────────────────

    [Fact]
    public async Task Update_CuandoDtoValido_Retorna200()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var dto = new OrdenRequestDto
        {
            NombreCliente = "Cliente Actualizado",
            MontoFlete = 1200.50m
        };

        // ACT
        var response = await _client.PutAsJsonAsync($"/api/ordenes/{id}", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_CuandoIdInexistente_Retorna404()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var dto = new OrdenRequestDto { NombreCliente = "Nuevo nombre" };

        // ACT
        var response = await _client.PutAsJsonAsync($"/api/ordenes/{id}", dto);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/ordenes/{id}/deactivate ──────────────────────────────

    [Fact]
    public async Task Deactivate_CuandoRegistroExiste_Retorna200()
    {
        // ACT
        var response = await _client.PostAsync("/api/ordenes/" + Guid.NewGuid() + "/deactivate", null);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### 7. JwtTestHelper — Token Generator para Tests

```csharp
// ── tests/Freiroute.API.Tests/JwtTestHelper.cs ────────────────────────
namespace Freiroute.API.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtTestHelper
{
    public static string GenerateTestToken(
        Guid userId,
        Guid empresaId,
        Guid perfilId,
        string[] permisos)
    {
        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("empresa_id", empresaId.ToString()),
            new Claim("perfil_id", perfilId.ToString()),
            new Claim("tipo_usuario", "ADMIN"),
            new Claim("permisos", string.Join(",", permisos))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("jwt-secret-key-for-testing-only"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "freiroute-api",
            audience: "freiroute-client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenSinPermisos() =>
        GenerateTestToken(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Array.Empty<string>());

    public static string GenerateTokenSoloLectura(Guid empresaId) =>
        GenerateTestToken(Guid.NewGuid(), empresaId, Guid.NewGuid(), new[] { "ordenes:read" });
}
```

### 8. Checklist de Tests por Módulo

#### BLL Tests (Unit) — Mínimo 80% cobertura
- [ ] `GetAllAsync_CuandoExistenRegistros_RetornaLista`
- [ ] `GetAllAsync_CuandoNoExistenRegistros_RetornaListaVacia`
- [ ] `GetByIdAsync_CuandoIdValido_RetornaRegistro`
- [ ] `GetByIdAsync_CuandoIdInexistente_RetornaNull`
- [ ] `CreateAsync_CuandoDtoValido_RetornaId`
- [ ] `CreateAsync_CuandoCampoObligatorioVacio_LanzaValidationException`
- [ ] `UpdateAsync_CuandoDtoValido_RetornaActualizado`
- [ ] `UpdateAsync_CuandoRegistroInexistente_LanzaKeyNotFoundException`
- [ ] `DeactivateAsync_CuandoRegistroExiste_RetornaTrue`
- [ ] `DeactivateAsync_CuandoRegistroInexistente_RetornaFalse`
- [ ] `DeactivateAsync_NuncaLlamaDeleteAsync` (ADR-005 verification)
- [ ] `empresa_id_siempre_del_jwt` (multi-tenant isolation)

#### Validator Tests (Unit)
- [ ] `Validate_CuandoTodosLosCamposValidos_NoHayErrores`
- [ ] `Validate_CuandoCampoObligatorioVacio_TieneError`
- [ ] `Validate_CuandoCampoExcedeLongitudMaxima_TieneError`
- [ ] `Validate_CuandoValorNumericosInválido_TieneError`
- [ ] `Validate_CuandoEstadoInvalido_TieneError`
- [ ] `[Theory]` con valores válidos de enums/constraints

#### API Tests (Integration) — Mínimo 60% cobertura
- [ ] `GET_All_CuandoAutenticado_Retorna200`
- [ ] `GET_All_SinToken_Retorna401`
- [ ] `GET_All_ConTokenSinPermiso_Retorna403`
- [ ] `GET_By_Id_CuandoExiste_Retorna200`
- [ ] `GET_By_Id_CuandoNoExiste_Retorna404`
- [ ] `POST_Create_CuandoDtoValido_Retorna201`
- [ ] `POST_Create_CuandoDtoInvalido_Retorna400`
- [ ] `POST_Create_SinToken_Retorna401`
- [ ] `PUT_Update_CuandoDtoValido_Retorna200`
- [ ] `PUT_Update_CuandoIdInexistente_Retorna404`
- [ ] `POST_Deactivate_CuandoExiste_Retorna200`

### 9. Métricas y Reportes

| Métrica | Objetivo | Herramienta |
|---|---|---|
| Cobertura tests BLL | ≥ 80% | Coverlet + reportgenerator |
| Cobertura tests API | ≥ 60% | Coverlet + reportgenerator |
| Tests críticos fallidos | 0 | CI Pipeline GitHub Actions |
| Tiempo ejecución tests BLL | < 30 segundos | Local |
| Tiempo ejecución tests API | < 60 segundos | Local |
| Violaciones de naming convention | 0 | Regex en analyzer |

#### Comandos de Ejecución
```bash
# Ejecutar TODOS los tests
dotnet test

# Solo BLL
dotnet test tests/Freiroute.BLL.Tests

# Solo API
dotnet test tests/Freiroute.API.Tests

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte HTML
reportgenerator -reports:**/coverage.cobertura.xml \
                -targetdir:coverage \
                -reporttypes:Html

# Ejecutar un test específico
dotnet test --filter "DisplayName~CreateAsync_CuandoMontoNegativo"
```

### 10. Contexto Freiroute TMS — Testing de Dominio

@QA asegura la calidad del sistema de gestión de transporte verificando específicamente:

**Multi-tenancy:**
- Cada test usa un `empresaId` único — nunca compartir datos entre tenants simulados
- Mocks deben filtrar siempre por `empresa_id` igual a como lo hace DAL Repository

**Permisos granulares:**
- Probar 401 sin token, 403 con token sin permiso, 200 con permiso adecuado
- Verificar que READ, CREATE, UPDATE funcionan correctamente

**Soft delete:**
- `DeactivateAsync` cambia `activo = false` y listados filtran `activo = true`
- Nunca llamar `DeleteAsync` — verificar con `.Verify(..., Times.Never)`

**Flujo de estados TMS:**
- Órdenes: DRAFT → CONFIRMED → CLOSED
- Embarques: DRAFT → CONFIRMED → ASSIGNED → IN_TRANSIT → DELIVERED
- Transiciones inválidas deben ser rechazadas

**Cálculos financieros:**
- Costos de flete no negativos ni excedentes
- Conversión de moneda correcta si aplica
- Redondeo decimal consistente (4 decimales)

**Archivos y Storage:**
- Upload/download de archivos a Supabase Storage con tokens temporales (signed URLs)
- POD digital y carta de porte válidos

**Filosofía de Testing:**
> "El test que falla es la especificación ejecutable. No escribimos código hasta tener un test que falle y que nos diga qué comportamiento esperar."

**Regla 80-60:**
- BLL Unit Tests: ≥ 80% cobertura
- API Integration Tests: ≥ 60% cobertura
- E2E/Playwright: Flujos críticos solamente (opcional)

### 11. Criterios de Aceptación por HU

Antes de aprobar cualquier HU, @QA valida:

- [ ] Todos los tests del checklist de este módulo pasan
- [ ] Cobertura BLL ≥ 80% y API ≥ 60%
- [ ] Criterios de aceptación del spec.md verificados manualmente
- [ ] No hay regresión en tests existentes
- [ ] Mensajes de validación en español consistentes
- [ ] Sin datos sensibles en logs de tests

---

## Dependencias entre Agentes

| Recibe de | Entrega a | Formato de handoff |
|---|---|---|
| @BackendDev | Código implementado listo para testear | PR con changes |
| @QA | Resultado de tests (pass/fail) + métricas de cobertura | Comments en PR |
| @PM | Specs y criterios de aceptación | docs/specs/HU-XXX |
