# CLAUDE.md — Freiroute TMS · Guía Operativa para Claude Code (Antigravity)

> **Archivo de contexto persistente — se carga en CADA sesión.**
> Contiene el estado real del proyecto: patrones concretos de código, decisiones tomadas,
> convenciones activas, gotchas conocidos y el inventario de sprints completados.
> **Leer AGENTS.md completo primero. Este archivo complementa y profundiza.**

---

## 🏷️ Identidad del Producto

**Nombre:** Freiroute TMS — Transportation Management System  
**Tipo:** SaaS Multi-Tenant · Clase mundial (referencia: Oracle TMS, SAP TM, MercuryGate, BluJay, Trimble TMS)  
**Tagline:** *"Manage every route. Move every load."*  
**Stack:** ASP.NET Core 8 · Supabase (PostgreSQL 15) · Dapper · JWT · Bootstrap 5.3

---

## 📦 Estado Real del Proyecto

### Sprints Completados

| Sprint | Épica | HUs | Tests | Cobertura BLL | Cobertura API |
|---|---|---|---|---|---|
| **SP-01** | EP-01 Auth & Multi-Tenant | HU-001→008 (HU-004 OAuth diferida) | 317 BLL + 114 API | 85.3% | 75.5% |
| **SP-02** | EP-02 Admin SaaS & Tenants | HU-009→014 (HU-004 OAuth diferida) | 438 total (0 fallos) | 85.3% | 75.5% |

### Próximo Sprint

**SP-03** → EP-03 Gestión de Maestros (Catálogos)  
**Deuda a resolver en SP-03:**
1. HU-004 OAuth (Google/Microsoft) — diferida de SP-01 y SP-02
2. `AdminController` accede directo al DAL en 4 endpoints → mover a `AdminDashboardService`
3. `SuspensionMiddleware` — middleware dedicado para tenants SUSPENDED (ahora solo validación en servicio)
4. `TenantController` MVC — falta el controller para rutas `/tenant/*`
5. README.md — verificar/crear

### Tablas y Migraciones Existentes (18 total)

**Sprint 1 (prefijo `20260101`):**
`empresas` · `perfiles` · `permisos` · `usuarios` · `invitaciones` · `auditoria_actividad` · `sesiones`

**Sprint 2 (prefijo `20260201`):**
`planes` · `suscripciones` · `pagos` · `configuracion_2fa` · `codigos_2fa_temporales`

**Post-fix (prefijo `20260202`):**
`ALTER TABLE empresas ADD COLUMN` → `industria`, `sitio_web`, `email_remitente`, `nombre_remitente`, `modos_transporte_activos`

---

## 🏗️ Arquitectura N-Tier — 8 Proyectos

### Flujo Obligatorio

```
Vista Razor → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL
```

**Violaciones que NO se permiten:**
- El Controller MVC nunca llama al DAL directamente
- La Vista nunca llama al BLL ni al API
- El BLL nunca accede a `HttpContext`
- ⚠️ DEUDA ACTIVA: `AdminController` inyecta `IEmpresaRepository` + `ISuscripcionRepository` directamente (4 endpoints) → refactorizar en SP-03

### Proyectos y Namespaces

```
src/
├── Freiroute.Entity/       → namespace Freiroute.Entity
├── Freiroute.DTO/          → namespace Freiroute.DTO.[Modulo]
├── Freiroute.DAL/          → namespace Freiroute.DAL.Interfaces / .Repositories
├── Freiroute.BLL/          → namespace Freiroute.BLL.Interfaces / .Services / .Validators / .Settings
├── Freiroute.IOC/          → namespace Freiroute.IOC
├── Freiroute.Utility/      → namespace Freiroute.Utility.[Subcarpeta]
├── Freiroute.API/          → namespace Freiroute.API
└── Freiroute.Aplicacion/   → namespace Freiroute.Aplicacion
```

### Orden de Creación en Cada Sprint (obligatorio)

```
1. Entity  →  2. DTO  →  3. DAL Interface  →  4. DAL Repository
→  5. BLL Interface  →  6. BLL Service  →  7. BLL Validator
→  8. IOC Registration  →  9. API Controller  →  10. Migración SQL
→  11. Tests BLL  →  12. Tests API  →  13. Vistas Razor
```

---

## 🗄️ Convenciones de Base de Datos

### Campos Obligatorios en Tablas de Negocio

```sql
-- Toda tabla de negocio (que tenga empresa_id) debe incluir:
id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
activo              BOOLEAN NOT NULL DEFAULT true,
fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
fecha_modificacion  TIMESTAMPTZ   -- actualizado por trigger
```

> **Excepción:** Tablas raíz/globales del SaaS (`empresas`, `planes`, `suscripciones`, `pagos`) no tienen `empresa_id` porque las gestiona el SUPER_ADMIN globalmente.

### Trigger Obligatorio en Cada Tabla

```sql
CREATE TRIGGER trg_[tabla]_fecha_modificacion
    BEFORE UPDATE ON [tabla]
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

### Índices Obligatorios

```sql
CREATE INDEX idx_[tabla]_empresa_id ON [tabla](empresa_id);
CREATE INDEX idx_[tabla]_activo     ON [tabla](activo);
-- Composite para queries frecuentes:
CREATE INDEX idx_[tabla]_empresa_activo ON [tabla](empresa_id, activo);
```

### RLS — Row Level Security

```sql
ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_[tabla]" ON [tabla]
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);
```

> El `TenantMiddleware` inyecta el tenant con:
> ```sql
> SELECT set_config('app.current_empresa_id', @val, true)
> ```

### Soft Delete — NUNCA DELETE Físico

```sql
-- Siempre:
UPDATE [tabla] SET activo = false, fecha_modificacion = NOW() WHERE id = @Id
-- JAMÁS:
DELETE FROM [tabla] WHERE id = @Id
```

---

## ⚙️ Patrones de Código C# — Con Ejemplos Reales

### Patrón Repository (DAL) — Dapper

```csharp
// Freiroute.DAL/Repositories/UsuarioRepository.cs — patrón canónico
public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnection _connection;

    public UsuarioRepository(IDbConnection connection) => _connection = connection;

    public async Task<IEnumerable<Usuario>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id                 AS Id,
                empresa_id         AS EmpresaId,
                nombre_completo    AS NombreCompleto,
                email              AS Email,
                activo             AS Activo,
                fecha_creacion     AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM usuarios
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre_completo ASC";

        return await _connection.QueryAsync<Usuario>(sql, new { EmpresaId = empresaId });
    }
}
```

**Reglas Dapper:**
- Usar siempre alias `AS NombrePascalCase` para mapear a las propiedades de la Entity
- Filtrar siempre `AND activo = true` (salvo excepciones justificadas)
- Filtrar siempre `AND empresa_id = @EmpresaId` en tablas de negocio
- Usar `@Param` para parámetros (nunca interpolación de strings)
- Usar `QueryFirstOrDefaultAsync<T>` para GetById, `QueryAsync<T>` para GetAll

### Patrón Service (BLL)

```csharp
// Freiroute.BLL/Services/[Modulo]Service.cs — patrón canónico
public class EmpresaService : IEmpresaService
{
    // Inyectar repositorios, validators, auditoria, email, logger
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IValidator<EmpresaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<EmpresaService> _logger;

    public async Task<EmpresaResponseDto> CreateAsync(EmpresaRequestDto request)
    {
        // 1. Validar con FluentValidation
        await _validator.ValidateAndThrowAsync(request);

        // 2. Validar reglas de negocio
        var existe = await _empresaRepository.GetByEmailAdminAsync(request.EmailAdmin);
        if (existe is not null)
            throw new ConflictException("El email ya está registrado en otra empresa.");

        // 3. Crear la entidad
        var empresa = new Empresa { Nombre = request.Nombre, ... };

        // 4. Persistir
        await _empresaRepository.CreateAsync(empresa);

        // 5. Registrar auditoría (siempre)
        await _auditoria.RegistrarAsync("empresas", "CREATE", empresa.Id, ...);

        // 6. Retornar DTO (nunca Entity)
        return MapToResponseDto(empresa);
    }
}
```

### Patrón Controller (API)

```csharp
// Freiroute.API/Controllers/[Modulo]Controller.cs — patrón canónico
[ApiController]
[Route("api/[modulo]")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public EmpresasController(IEmpresaService empresaService)
        => _empresaService = empresaService;

    /// <summary>Descripción Swagger del endpoint (HU-001 CA-XX).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Empresas, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<EmpresaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EmpresaResponseDto>>> Create(EmpresaRequestDto request)
    {
        var resultado = await _empresaService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = resultado.Id },
            ApiResponse<EmpresaResponseDto>.Ok(resultado, "Empresa creada exitosamente"));
    }
}
```

### ApiResponse\<T\> — El Wrapper Universal

```csharp
// Siempre usar estos factory methods. NUNCA retornar tipos puros.
return Ok(ApiResponse<T>.Ok(data, "Mensaje de éxito"));         // 200
return Created(uri, ApiResponse<T>.Ok(data, "Creado"));         // 201
return Ok(ApiResponse<string>.Ok(string.Empty, "Desactivado")); // 200 en deactivate

// El GlobalExceptionMiddleware maneja los errores automáticamente:
// BusinessException       → 422 Unprocessable Entity
// NotFoundException       → 404 Not Found
// ConflictException       → 409 Conflict
// ForbiddenException      → 403 Forbidden
// ValidationException     → 400 Bad Request (FluentValidation)
// Requires2faException    → 202 Accepted (+ TempToken en body especial)
// Exception               → 500 Internal Server Error (solo mensaje genérico al cliente)
```

### Excepciones del Dominio (Freiroute.Utility.Exceptions)

```csharp
// Usar la excepción correcta según el caso:
throw new BusinessException("Mensaje de regla de negocio");           // → 422
throw new NotFoundException("Recurso no encontrado");                  // → 404
throw new ConflictException("Email ya existe en otra empresa");        // → 409
throw new ForbiddenException("Solo el Super Admin puede hacer esto");  // → 403
throw new Requires2faException(tempToken);                             // → 202

// NUNCA usar throw new Exception() directamente en BLL
// NUNCA retornar null cuando debería ser 404 — usar NotFoundException
```

### RequirePermission — Control de Acceso

```csharp
// En el controller — Módulos disponibles en ModuloPermiso.*:
[RequirePermission(ModuloPermiso.Ordenes, PermissionType.Read)]    // GET listados
[RequirePermission(ModuloPermiso.Embarques, PermissionType.Create)] // POST crear
[RequirePermission(ModuloPermiso.Carriers, PermissionType.Update)]  // PUT/PATCH editar

// SUPER_ADMIN tiene bypass automático — no necesita permisos explícitos.
// Para endpoints exclusivos de Super Admin, usar el guard del AdminController:
private void VerificarSuperAdmin()
{
    if (!User.IsSuperAdmin())
        throw new ForbiddenException("Solo el Super Admin puede acceder.");
}
```

### FluentValidation — Patrón de Validators

```csharp
// Freiroute.BLL/Validators/[Modulo]Validator.cs
public class EmpresaValidator : AbstractValidator<EmpresaRequestDto>
{
    public EmpresaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("Máximo 200 caracteres");

        RuleFor(x => x.EmailAdmin)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Formato de email inválido");
    }
}
// Mensajes de validación SIEMPRE en español.
// Registrar en IOC: services.AddScoped<IValidator<EmpresaRequestDto>, EmpresaValidator>();
// Invocar en Service: await _validator.ValidateAndThrowAsync(request);
```

---

## 🔐 Multi-Tenancy — Guía Operativa

### TenantMiddleware

El middleware se ejecuta DESPUÉS de `UseAuthentication()` en el pipeline:

```
GlobalExceptionMiddleware → UseHttpsRedirection → UseAuthentication → UseAuthorization
  → TenantMiddleware → OnboardingRedirectMiddleware → MapControllers
```

**Rutas excluidas del TenantMiddleware** (públicas):
- `/api/auth/*` (login, refresh, forgot-password, reset-password, oauth, 2fa/verify)
- `/swagger/*`
- `/health`
- `/`

**Super Admin**: no tiene `empresa_id` en su JWT. Puede pasar el header `X-Empresa-Id` para operar en un tenant específico.

### JWT Claims — Estructura Exacta

```json
{
  "user_id": "uuid-del-usuario",
  "empresa_id": "uuid-de-la-empresa",
  "perfil_id": "uuid-del-perfil",
  "tipo_usuario": "ADMIN",
  "nombre": "Juan Pérez",
  "permisos": ["ordenes:read", "embarques:create", "carriers:read"],
  "impersonado_por": "uuid-super-admin"  // solo si es impersonación
}
```

**Helpers de claims** (ClaimsPrincipalExtensions en `Freiroute.API/Extensions/`):
```csharp
User.GetUsuarioId()   // → Guid
User.GetEmpresaId()   // → Guid
User.IsSuperAdmin()   // → bool
```

### Login con 2FA — Flujo Especial

```
POST /api/auth/login (credenciales OK + 2FA activo)
  → 202 Accepted { requires2fa: true, tempToken: "..." }
  → POST /api/auth/2fa/verify { tempToken, codigo }
  → 200 OK { accessToken, refreshToken, usuario }
```

El `tempToken` expira en 10 minutos. Si el código 2FA falla 3 veces, la sesión se cierra.  
Los recovery codes se muestran **UNA sola vez** al activar 2FA. `GET /api/auth/2fa/recovery-codes` siempre retorna 422 (los hashes no son reversibles).

### Estados del Tenant (empresa.estado)

```
TRIAL       → período de prueba 30 días (estado al crear)
ACTIVE      → suscripción activa y pagada
PAST_DUE    → vencido pero con 7 días de gracia (job corre a medianoche UTC)
SUSPENDED   → sin acceso a módulos operativos (solo login + aviso)
CANCELLED   → cancelado permanentemente
```

El `VencimientoSuscripcionJob` (BackgroundService) procesa: ACTIVE con venc. pasado → PAST_DUE. PAST_DUE con +7 días → SUSPENDED.

---

## 🧪 Patrones de Testing

### Tests Unitarios BLL (xUnit + Moq + FluentAssertions)

```csharp
// tests/Freiroute.BLL.Tests/Services/[Modulo]ServiceTests.cs
public class EmpresaServiceTests
{
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<IValidator<EmpresaRequestDto>> _validator = new();

    private EmpresaService CrearServicio() => new(
        _empresaRepo.Object, /* otros mocks */ _auditoria.Object, _validator.Object);

    [Fact]
    public async Task CreateAsync_EmailDuplicado_LanzaConflictException()
    {
        // Arrange
        _empresaRepo.Setup(r => r.GetByEmailAdminAsync("test@test.com"))
                    .ReturnsAsync(new Empresa { Id = Guid.NewGuid() });
        var sut = CrearServicio();

        // Act + Assert
        await sut.Invoking(s => s.CreateAsync(new EmpresaRequestDto { EmailAdmin = "test@test.com" }))
                 .Should().ThrowAsync<ConflictException>();
    }
}
```

### Tests de Integración API (WebApplicationFactory + Moq)

```csharp
// tests/Freiroute.API.Tests/Controllers/[Modulo]ControllerTests.cs
// Usar TestWebApplicationFactory que mockea todos los BLL Services
public class EmpresasControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IEmpresaService> _empresaService;

    public EmpresasControllerTests(TestWebApplicationFactory factory)
    {
        _empresaService = factory.EmpresaServiceMock;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ConTokenSuperAdmin_Retorna200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerarTokenSuperAdmin());

        var response = await _client.GetAsync("/api/empresas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Tokens para Tests

```csharp
// tests/Freiroute.API.Tests/JwtTestHelper.cs
JwtTestHelper.GenerarTokenSuperAdmin()     // tipo_usuario: SUPER_ADMIN
JwtTestHelper.GenerarTokenAdmin(empresaId) // tipo_usuario: ADMIN con empresa_id
JwtTestHelper.GenerarTokenOperador(empresaId, permisos) // tipo_usuario: OPERADOR
```

### Umbrales Obligatorios (DoD)

- **BLL:** ≥ 80% cobertura (`dotnet test --collect:"XPlat Code Coverage"`)
- **API:** ≥ 60% cobertura
- `dotnet build Freiroute.sln` → **0 warnings / 0 errores**
- `dotnet test Freiroute.sln` → **0 fallos**

---

## 🎨 Design System Freiroute — Implementación Real

### Variables CSS (freiroute.css — 37KB en wwwroot/css/)

```css
:root {
  /* Identidad */
  --fr-navy-primary:    #0B2545;   /* sidebar, navbar, marca */
  --fr-navy-mid:        #1B4F8A;   /* hover sidebar */
  --fr-action-blue:     #1A73E8;   /* botones CTA, links */
  --fr-cyan-accent:     #00D4FF;   /* logo, item activo sidebar */
  --fr-blue-tint:       #E3F0FF;   /* fondos de tarjetas informativas */

  /* Semántica operacional */
  --fr-success:         #2E7D32;
  --fr-success-light:   #E6F4EA;
  --fr-warning:         #F57F17;
  --fr-warning-light:   #FFF8E1;
  --fr-danger:          #E53935;
  --fr-danger-light:    #FFEBEE;

  /* Neutrales */
  --fr-surface-bg:      #F8FAFC;
  --fr-surface-card:    #FFFFFF;
  --fr-text-primary:    #1E293B;
  --fr-text-secondary:  #64748B;
  --fr-border:          #E2E8F0;
}
```

### Tipografía

| Rol | Fuente | Pesos | Uso |
|---|---|---|---|
| UI Principal | Inter (Variable) | 400, 500, 600, 700 | Todo el sistema |
| Display/Marketing | DM Sans | 400, 500, 700 | Portal cliente, onboarding |
| Datos/Códigos | JetBrains Mono | 400, 500 | IDs de embarque, números |

### Badges de Estado (HTML real del proyecto)

```html
<span class="badge-fr badge-fr-success">Entregado</span>
<span class="badge-fr badge-fr-info">En tránsito</span>
<span class="badge-fr badge-fr-warning">SLA en riesgo</span>
<span class="badge-fr badge-fr-danger">Retrasado</span>
<span class="badge-fr badge-fr-neutral">Planificado</span>
```

### Layout

- **Sidebar expandido:** 240px · fondo `#0B2545`
- **Sidebar colapsado:** 64px · solo íconos
- **Topbar:** 56px · fondo blanco · `box-shadow: 0 1px 3px rgba(0,0,0,.08)`
- **Paginación:** 20 registros/página (estándar en toda la app)

### Areas MVC Existentes

```
Freiroute.Aplicacion/Areas/
├── Admin/       → Panel Super Admin (Dashboard, Planes, Suscripciones, Empresas)
├── Auth/        → Login, ForgotPassword, ResetPassword
├── Onboarding/  → Wizard 5 pasos (Paso1..5 + _LayoutOnboarding.cshtml)
└── Tenant/      → Usuarios, Configuracion  ← TenantController MVC PENDIENTE
```

---

## 🔧 Dependencia de Inyección — Registro IOC

Todos los registros están en `Freiroute.IOC/DependencyInjection.cs`.

**Lifetimes usados:**
- `Scoped` → Repositorios, Servicios BLL, Validators, IDbConnection
- `Singleton` → `IJwtService` (sin estado mutable, thread-safe)
- `HttpClient` → `IStorageService` (SupabaseStorageService) via `AddHttpClient<>`

**Al crear un módulo nuevo, agregar en DependencyInjection.cs:**
```csharp
// DAL
services.AddScoped<INuevoModuloRepository, NuevoModuloRepository>();
// BLL
services.AddScoped<INuevoModuloService, NuevoModuloService>();
// Validator
services.AddScoped<IValidator<NuevoModuloRequestDto>, NuevoModuloValidator>();
```

---

## ⚠️ Gotchas y Trampas Conocidas

### 1. Columnas de `empresas` — Migración post-fix

La tabla `empresas` tiene columnas añadidas en la migración `20260202000001`:
`industria`, `sitio_web`, `email_remitente`, `nombre_remitente`, `modos_transporte_activos`

El `EmpresaRepository.GetByIdAsync()` y `UpdateAsync()` YA incluyen estas columnas.  
Al hacer queries manuales sobre `empresas`, incluirlas.

### 2. TOTP_ENCRYPTION_KEY — Obligatoria al arrancar

`Program.cs` valida la key al inicio. Sin ella, el servidor no arranca:
```
CONFIGURACIÓN REQUERIDA: Security:TotpEncryptionKey no está definida.
```
En desarrollo: `appsettings.Development.json` (NO trackeado en git). En producción: variable de entorno `TOTP_ENCRYPTION_KEY`.

### 3. Supabase Storage — Bucket `logos-tenants`

El bucket debe existir en cada entorno antes de subir logos. En local: crear con Supabase Studio. La `ServiceRoleKey` debe ser la real del entorno (obtenida con `supabase status`), NO la demo key.

### 4. Onboarding — Verbos HTTP reales vs Spec

Los endpoints reales del wizard usan **POST** (no PUT como dice el spec original):
```
POST /api/onboarding/paso1..5  (no PUT)
POST /api/onboarding/completar
```
El redirect del `OnboardingRedirectMiddleware` apunta a `/onboarding?paso=1` (query param).

### 5. Deactivate/Reactivate — Verbo PATCH (no PUT)

```
PATCH /api/usuarios/{id}/deactivate   (no PUT)
PATCH /api/usuarios/{id}/reactivate   (no PUT)
```

### 6. Límite de Plan — Verificar en CREATE e INVITE

`PlanLimiteService.VerificarLimiteUsuariosAsync()` debe invocarse en:
- `UsuarioService.CreateAsync()`
- `UsuarioService.InvitarAsync()`
- `UsuarioService.ReactivarAsync()`
- `OnboardingService.GuardarPaso5Async()` (al enviar invitaciones)

### 7. AdminController — Restricción de Super Admin

`AdminController` NO usa `[RequirePermission]`. Usa `VerificarSuperAdmin()` al inicio de cada método. Los tests validan que cualquier rol no-SUPER_ADMIN recibe 403.

### 8. `GET /api/auth/2fa/recovery-codes` — Siempre retorna 422

Por diseño: los recovery codes en BD son hashes SHA-256 no reversibles. Solo se muestran en claro UNA vez al activar 2FA (en el response de `POST /api/auth/2fa/setup`).

### 9. Swagger CustomSchemaIds

`Program.cs` tiene `c.CustomSchemaIds(type => type.FullName)` para evitar colisiones entre DTOs con el mismo nombre en distintos namespaces (ej: `Freiroute.DTO.Empresa.EmpresaResponseDto` vs `Freiroute.DTO.Admin.EmpresaResponseDto`).

---

## 📋 Flujo de Trabajo por Historia de Usuario

```
1. AGENTS.md → leer completo
2. skill del rol asignado → leer
3. docs/framework/convenciones.md + requerimientos.md → revisar
4. docs/specs/[modulo]-spec.md → leer o crear
5. branch: feature/HU-XXX-nombre-hu desde develop
6. supabase start → verificar BD local disponible
7. dotnet build → 0 warnings antes de empezar
8. Implementar: Entity → DTO → DAL → BLL → Tests → API Controller → Vistas
9. dotnet test → 0 fallos
10. PR → aprobación → merge
```

---

## 🗣️ Convención de Idiomas

| Elemento | Idioma |
|---|---|
| Interfaz de usuario (labels, mensajes, menús) | **Español** |
| Tablas y columnas SQL | **Español** (snake_case) |
| Comentarios de BD | **Español** |
| Clases, métodos, interfaces C# | **Inglés** |
| Comentarios de código C# | **Inglés** |
| Documentación técnica (docs/) | **Español** |
| Mensajes de validación (FluentValidation / jQuery) | **Español** |
| Logs Serilog | **Inglés** |
| Nombres de migrations | **Inglés** o **Español** (consistente con los existentes) |

---

## 📚 Glosario del Dominio TMS

| Término | Descripción |
|---|---|
| `tenant` / `empresa` | Organización de transporte suscrita al SaaS |
| `empresa_id` | UUID discriminador universal de tenant en todas las tablas de negocio |
| `activo` | `boolean` de soft delete — `false` = registrado eliminado lógicamente |
| `RLS` | Row Level Security — aislamiento de datos a nivel PostgreSQL |
| `embarque` / `shipment` | Operación de transporte individual asignada a un carrier |
| `orden` | Solicitud de transporte del cliente (puede consolidarse en embarque) |
| `carrier` | Transportista (empresa propia o tercero) |
| `conductor` | Operador de vehículo registrado |
| `dispatcher` | Planificador/asignador de embarques |
| `OTD` | On-Time Delivery — % de entregas a tiempo |
| `POD` | Proof of Delivery — prueba de entrega digital |
| `FTL` | Full Truck Load — camión completo |
| `LTL` | Less Than Truck Load — carga parcial consolidada |
| `SLA` | Service Level Agreement — compromiso de nivel de servicio |
| `ETA` | Estimated Time of Arrival — hora estimada de llegada |
| `MRR` | Monthly Recurring Revenue — ingreso recurrente mensual |
| `ARR` | Annual Recurring Revenue — ingreso recurrente anual |
| `TRIAL` | Período de prueba 30 días al crear un nuevo tenant |
| `PAST_DUE` | Tenant vencido con 7 días de gracia antes de suspensión |
| `SUSPENDED` | Tenant bloqueado por falta de pago — solo ve aviso de suspensión |
| `ADR` | Architecture Decision Record — decisión arquitectónica documentada |
| `DoD` | Definition of Done — criterios para cerrar una HU |
| `HU` | Historia de Usuario |
| `BLL` | Business Logic Layer |
| `DAL` | Data Access Layer (Dapper) |
| `DTO` | Data Transfer Object |
| `IOC` | Inversión de Control / contenedor DI |

---

## 🔗 Rutas de Referencia Rápida

```
AGENTS.md                                → reglas del proyecto (leer primero)
docs/adr/                                → 13 ADRs (ADR-001 a ADR-013)
docs/specs/sprint-01-EP01-auth-multitenant.md  → spec Sprint 1
docs/specs/sprint-02-EP02-admin-saas.md        → spec Sprint 2
docs/specs/sprint-01-qa-report.md              → QA Report SP-01
docs/specs/sprint-02-qa-report.md              → QA Report SP-02 (438 tests, 0 fallos)
docs/specs/re-smoke-test-report.md             → bugs históricos y fixes aplicados
docs/framework/freiroute-product-backlog.md    → 156 HUs · 26 Sprints
docs/framework/lifecycle.md                   → ciclo de vida de ingeniería
docs/framework/freiroute-design-system.md     → Diseño del sistema Freiroute (referencia obligatoria para @FrontendDev antes de tocar cualquier vista)
supabase/migrations/                          → 18 migraciones versionadas
src/Freiroute.IOC/DependencyInjection.cs      → registro de TODAS las dependencias
src/Freiroute.API/Program.cs                  → pipeline de middleware y arranque
```

---

*CLAUDE.md — Freiroute TMS | Para Claude Code (Antigravity)*  
*Versión: 2.0 | Actualizado: 2026-09-05*  
*Estado del proyecto: SP-01 + SP-02 completados · 438 tests · BLL 85.3% · API 75.5%*  
*Próximo: SP-03 EP-03 — Gestión de Maestros (Catálogos)*
