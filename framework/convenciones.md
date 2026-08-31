# Convenciones de Código — SaaS con ASP.NET Core + Supabase

> **Estándar de codificación para todos los proyectos del stack**
> Estas convenciones se aplican automáticamente via AGENTS.md.
> Garantizan consistencia, legibilidad y mantenibilidad a largo plazo.

---

## 1. Nomenclatura General

### Reglas por tipo de elemento

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases C# | PascalCase | `ProductoService`, `CitaRepository` |
| Interfaces C# | IPascalCase | `IProductoService`, `ICitaRepository` |
| Métodos C# | PascalCase | `GetAllAsync`, `CreateProductoAsync` |
| Variables C# | camelCase | `productoId`, `tenantId`, `isValid` |
| Parámetros C# | camelCase | `dto`, `tenantId`, `cancellationToken` |
| Constantes C# | UPPER_SNAKE_CASE | `MAX_PAGE_SIZE`, `DEFAULT_TIMEOUT` |
| Propiedades C# | PascalCase | `PrimerNombre`, `FechaCreacion` |
| Tablas SQL | snake_case (plural) | `productos`, `tipos_categorias` |
| Columnas SQL | snake_case | `tenant_id`, `fecha_nacimiento` |
| Archivos .cshtml | PascalCase | `Index.cshtml`, `CreateProducto.cshtml` |
| Archivos .cs | PascalCase | `ProductoService.cs`, `IProductoService.cs` |
| Rutas URL | kebab-case | `/api/tipos-categoria`, `/catalogos/productos` |
| Variables JavaScript | camelCase | `productoId`, `tenantData` |
| Constantes JavaScript | UPPER_SNAKE_CASE | `MAX_RETRIES`, `API_BASE_URL` |

### Reglas de idioma

```
Variables, métodos, clases C# → INGLÉS
Tablas, columnas, comentarios BD → ESPAÑOL
Mensajes de error al usuario → ESPAÑOL
Documentación técnica → ESPAÑOL
Comentarios de código → INGLÉS
```

---

## 2. Estructura Obligatoria por Tipo de Archivo

### Entity (Modelo de Dominio)

```csharp
// Vittal.Entity/Producto.cs
namespace [Proyecto].Entity;

/// <summary>
/// Represents a product in the system. Belongs to a tenant.
/// </summary>
public class Producto
{
    // ─── Identity ────────────────────────────────────────────
    public Guid Id { get; set; }                         // Autogenerado por PostgreSQL
    public Guid TenantId { get; set; }                   // OBLIGATORIO — discriminador tenant

    // ─── Business Fields ─────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string? FotoUrl { get; set; }                 // Supabase Storage URL

    // ─── Lifecycle ───────────────────────────────────────────
    public bool Activo { get; set; } = true;             // NUNCA eliminar — solo desactivar
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
```

### DTOs de Request y Response

```csharp
// [Proyecto].DTO/Producto/ProductoRequestDto.cs
namespace [Proyecto].DTO.Producto;

/// <summary>
/// Input data for creating or updating a Producto.
/// </summary>
public class ProductoRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
}

// [Proyecto].DTO/Producto/ProductoResponseDto.cs
namespace [Proyecto].DTO.Producto;

/// <summary>
/// Output data returned to the client. Never expose Entity directly.
/// </summary>
public class ProductoResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
```

### Interface del Repository (DAL)

```csharp
// [Proyecto].DAL/Interfaces/IProductoRepository.cs
namespace [Proyecto].DAL.Interfaces;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> GetAllAsync(Guid tenantId);
    Task<Producto?> GetByIdAsync(Guid id, Guid tenantId);
    Task<Guid> CreateAsync(Producto producto);
    Task<bool> UpdateAsync(Producto producto);
    Task<bool> DeactivateAsync(Guid id, Guid tenantId);   // No DeleteAsync — soft delete only
}
```

### Repository (DAL)

```csharp
// [Proyecto].DAL/Repositories/ProductoRepository.cs
namespace [Proyecto].DAL.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnection _db;

    public ProductoRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Producto>> GetAllAsync(Guid tenantId)
    {
        // SIEMPRE filtrar por tenant_id — RLS lo refuerza a nivel BD
        const string sql = @"
            SELECT * FROM productos
            WHERE tenant_id = @TenantId AND activo = true
            ORDER BY nombre ASC";

        return await _db.QueryAsync<Producto>(sql, new { TenantId = tenantId });
    }

    public async Task<Producto?> GetByIdAsync(Guid id, Guid tenantId)
    {
        const string sql = @"
            SELECT * FROM productos
            WHERE id = @Id AND tenant_id = @TenantId AND activo = true";

        return await _db.QueryFirstOrDefaultAsync<Producto>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(Producto producto)
    {
        const string sql = @"
            INSERT INTO productos (tenant_id, nombre, descripcion, precio, activo, fecha_creacion)
            VALUES (@TenantId, @Nombre, @Descripcion, @Precio, true, NOW())
            RETURNING id";

        return await _db.ExecuteScalarAsync<Guid>(sql, producto);
    }

    public async Task<bool> UpdateAsync(Producto producto)
    {
        const string sql = @"
            UPDATE productos
            SET nombre = @Nombre,
                descripcion = @Descripcion,
                precio = @Precio,
                fecha_modificacion = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";

        var rows = await _db.ExecuteAsync(sql, producto);
        return rows > 0;
    }

    // REGLA: No existe DeleteAsync — solo DeactivateAsync
    public async Task<bool> DeactivateAsync(Guid id, Guid tenantId)
    {
        const string sql = @"
            UPDATE productos
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";

        var rows = await _db.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
        return rows > 0;
    }
}
```

### Interface del Service (BLL)

```csharp
// [Proyecto].BLL/Interfaces/IProductoService.cs
namespace [Proyecto].BLL.Interfaces;

public interface IProductoService
{
    Task<IEnumerable<ProductoResponseDto>> GetAllAsync(Guid tenantId);
    Task<ProductoResponseDto?> GetByIdAsync(Guid id, Guid tenantId);
    Task<ProductoResponseDto> CreateAsync(ProductoRequestDto dto, Guid tenantId);
    Task<ProductoResponseDto> UpdateAsync(Guid id, ProductoRequestDto dto, Guid tenantId);
    Task<bool> DeactivateAsync(Guid id, Guid tenantId);
}
```

### Service (BLL)

```csharp
// [Proyecto].BLL/Services/ProductoService.cs
namespace [Proyecto].BLL.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly IValidator<ProductoRequestDto> _validator;
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(
        IProductoRepository repository,
        IValidator<ProductoRequestDto> validator,
        ILogger<ProductoService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductoResponseDto>> GetAllAsync(Guid tenantId)
    {
        var productos = await _repository.GetAllAsync(tenantId);
        return productos.Select(MapToResponseDto);
    }

    public async Task<ProductoResponseDto> CreateAsync(ProductoRequestDto dto, Guid tenantId)
    {
        // 1. Validar el DTO
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Mapear DTO a Entity
        var producto = new Producto
        {
            TenantId = tenantId,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio
        };

        // 3. Persistir
        var id = await _repository.CreateAsync(producto);

        // 4. Retornar el registro creado
        var created = await _repository.GetByIdAsync(id, tenantId);
        return MapToResponseDto(created!);
    }

    private static ProductoResponseDto MapToResponseDto(Producto p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Precio = p.Precio,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        FechaModificacion = p.FechaModificacion
    };
}
```

### API Controller

```csharp
// [Proyecto].API/Controllers/ProductosController.cs
namespace [Proyecto].API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _service;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(IProductoService service, ILogger<ProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los productos activos del tenant</summary>
    [HttpGet]
    [RequirePermission("productos", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = User.GetTenantId();
        var result = await _service.GetAllAsync(tenantId);
        return Ok(ApiResponse<IEnumerable<ProductoResponseDto>>.Ok(result));
    }

    /// <summary>Crea un nuevo producto</summary>
    [HttpPost]
    [RequirePermission("productos", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ProductoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
    {
        var tenantId = User.GetTenantId();
        var result = await _service.CreateAsync(dto, tenantId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ProductoResponseDto>.Ok(result, "Producto creado exitosamente"));
    }

    /// <summary>Desactiva un producto (soft delete)</summary>
    [HttpDelete("{id:guid}/deactivate")]
    [RequirePermission("productos", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var tenantId = User.GetTenantId();
        var result = await _service.DeactivateAsync(id, tenantId);
        if (!result) return NotFound();
        return Ok(ApiResponse<bool>.Ok(true, "Producto desactivado exitosamente"));
    }
}
```

### FluentValidator

```csharp
// [Proyecto].BLL/Validators/ProductoValidator.cs
namespace [Proyecto].BLL.Validators;

public class ProductoValidator : AbstractValidator<ProductoRequestDto>
{
    public ProductoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del producto es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a cero");

        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
            .When(x => x.Descripcion != null);
    }
}
```

### ApiResponse<T> Wrapper

```csharp
// [Proyecto].Utility/ApiResponse.cs
namespace [Proyecto].Utility;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Factory methods
    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}
```

---

## 3. Migraciones SQL — Plantilla

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_create_[tabla].sql

-- ============================================================
-- TABLA: [nombre_tabla]
-- Descripción: [qué almacena esta tabla]
-- HU relacionada: HU-XX
-- ============================================================

CREATE TABLE IF NOT EXISTS [nombre_tabla] (
    -- Identity
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,

    -- Business fields
    [campo1]        [TIPO] NOT NULL,
    [campo2]        [TIPO],

    -- Lifecycle (OBLIGATORIO en toda tabla de negocio)
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Índices obligatorios
CREATE INDEX idx_[tabla]_tenant_id ON [nombre_tabla](tenant_id);
CREATE INDEX idx_[tabla]_activo ON [nombre_tabla](activo);
-- Agregar índices adicionales según los campos de búsqueda frecuente

-- RLS obligatorio (aislamiento multi-tenant)
ALTER TABLE [nombre_tabla] ENABLE ROW LEVEL SECURITY;

CREATE POLICY "tenant_isolation" ON [nombre_tabla]
    FOR ALL
    USING (tenant_id = (current_setting('app.current_tenant_id', true))::UUID);

-- Comentarios en español (obligatorio)
COMMENT ON TABLE [nombre_tabla] IS '[Descripción de la tabla en español]';
COMMENT ON COLUMN [nombre_tabla].tenant_id IS 'Identificador del tenant al que pertenece el registro';
COMMENT ON COLUMN [nombre_tabla].activo IS 'Los registros no se eliminan, solo se desactivan';
```

---

## 4. Estructura de Carpetas por Módulo

```
Módulo: [NombreModulo]
├── [Proyecto].Entity/
│   └── [NombreModulo].cs
├── [Proyecto].DTO/
│   └── [NombreModulo]/
│       ├── [NombreModulo]RequestDto.cs
│       └── [NombreModulo]ResponseDto.cs
├── [Proyecto].DAL/
│   ├── Interfaces/I[NombreModulo]Repository.cs
│   └── Repositories/[NombreModulo]Repository.cs
├── [Proyecto].BLL/
│   ├── Interfaces/I[NombreModulo]Service.cs
│   ├── Services/[NombreModulo]Service.cs
│   └── Validators/[NombreModulo]Validator.cs
├── [Proyecto].API/
│   └── Controllers/[NombreModulo]Controller.cs
├── [Proyecto].Aplicacion/
│   └── Areas/[Area]/
│       ├── Controllers/[NombreModulo]Controller.cs
│       └── Views/[NombreModulo]/
│           ├── Index.cshtml
│           ├── Create.cshtml
│           └── Edit.cshtml
├── [Proyecto].IOC/
│   └── DependencyInjection.cs (agregar registro)
├── tests/
│   ├── [Proyecto].BLL.Tests/
│   │   ├── [NombreModulo]ServiceTests.cs
│   │   └── [NombreModulo]ValidatorTests.cs
│   └── [Proyecto].API.Tests/
│       └── [NombreModulo]ControllerTests.cs
└── supabase/migrations/
    └── YYYYMMDDHHMMSS_create_[nombre_tabla].sql
```

---

## 5. Checklist de Entregable por Módulo

Antes de marcar una HU como DONE:

```
✅ MIGRACIÓN
  [ ] Archivo .sql en supabase/migrations/
  [ ] Incluye tenant_id NOT NULL
  [ ] RLS habilitado
  [ ] Índices creados
  [ ] Comentarios en español
  [ ] supabase db push exitoso

✅ ENTITY Y DTOs
  [ ] Entity con todos los campos del schema
  [ ] RequestDto (campos editables por el usuario)
  [ ] ResponseDto (datos de salida, sin campos sensibles)

✅ DAL
  [ ] Interface IXxxRepository creada
  [ ] Repository implementado con Dapper
  [ ] GetAll filtra por tenant_id Y activo = true
  [ ] No existe DeleteAsync — solo DeactivateAsync
  [ ] Registrado en DependencyInjection.cs

✅ BLL
  [ ] Interface IXxxService creada
  [ ] Service implementado
  [ ] FluentValidator implementado
  [ ] Mapeo Entity → ResponseDto
  [ ] Tests unitarios ≥ 80% de cobertura
  [ ] Registrado en DependencyInjection.cs

✅ API
  [ ] Controller con [Authorize] y [Produces]
  [ ] Todos los endpoints con [RequirePermission]
  [ ] Swagger documentado (/// <summary>)
  [ ] Retorna ApiResponse<T>
  [ ] Tests de integración ≥ 60% de cobertura

✅ FRONTEND (MVC)
  [ ] Controller MVC en el Área correcta
  [ ] Vista Index.cshtml con listado paginado
  [ ] Vista Create.cshtml con formulario
  [ ] Vista Edit.cshtml con formulario prellenado
  [ ] jQuery Validate configurado en formularios
  [ ] Mensajes de éxito/error mostrados al usuario

✅ CALIDAD
  [ ] dotnet build sin warnings
  [ ] dotnet test sin fallos
  [ ] PR revisado por developer humano
  [ ] Demo aceptada por Product Owner
```

---

*convenciones.md — Estándar de código para proyectos SaaS con ASP.NET Core*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*
