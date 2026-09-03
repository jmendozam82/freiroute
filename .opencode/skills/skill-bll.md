# Skill: @BackendDev (Desarrollador Backend Freiroute TMS)

## Rol
**@BackendDev** implementa la lógica de negocio en la BLL, los controladores API y los tests unitarios e integración. Trabaja con los contratos que @Arquitecto definió y sobre las tablas que @IngenieroDatos creó. Es responsable de que el flujo de datos sea correcto, seguro y con cobertura de tests suficiente.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Leer Entity, DTOs e Interfaces creados por @Arquitecto
4. Confirmar que la migración de @IngenieroDatos está aplicada (supabase db diff vacío)
```

### 2. FluentValidator

```csharp
// Freiroute.BLL/Validators/[Modulo]Validator.cs
namespace Freiroute.BLL.Validators;

public class [Modulo]Validator : AbstractValidator<[Modulo]RequestDto>
{
    public [Modulo]Validator()
    {
        // ── Campos de texto ──────────────────────────────────────────
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        // ── Campos numéricos (ejemplo para tarifas TMS) ──────────────
        RuleFor(x => x.PesoKg)
            .GreaterThan(0).WithMessage("El peso debe ser mayor a 0")
            .LessThanOrEqualTo(50000).WithMessage("El peso no puede exceder 50,000 kg");

        RuleFor(x => x.VolumenM3)
            .GreaterThan(0).WithMessage("El volumen debe ser mayor a 0");

        // ── Fechas (ejemplo para embarques TMS) ──────────────────────
        RuleFor(x => x.FechaEntregaRequerida)
            .GreaterThan(x => x.FechaPickupPlanificada)
            .WithMessage("La fecha de entrega debe ser posterior al pickup");

        RuleFor(x => x.FechaPickupPlanificada)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("La fecha de pickup no puede ser en el pasado");

        // ── Enums / estados válidos del dominio TMS ───────────────────
        RuleFor(x => x.ModoTransporte)
            .Must(m => new[] { "FTL", "LTL", "AEREO", "MARITIMO", "FERROVIARIO", "INTERMODAL" }.Contains(m))
            .WithMessage("Modo de transporte inválido. Valores permitidos: FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL");

        // ── GUIDs requeridos ─────────────────────────────────────────
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("El cliente es obligatorio");

        RuleFor(x => x.OrigenId)
            .NotEmpty().WithMessage("La ubicación de origen es obligatoria");

        RuleFor(x => x.DestinoId)
            .NotEmpty().WithMessage("La ubicación de destino es obligatoria")
            .NotEqual(x => x.OrigenId).WithMessage("El destino no puede ser igual al origen");
    }
}
```

### 3. BLL Service

```csharp
// Freiroute.BLL/Services/[Modulo]Service.cs
namespace Freiroute.BLL.Services;

public class [Modulo]Service : I[Modulo]Service
{
    private readonly I[Modulo]Repository _repository;
    private readonly IValidator<[Modulo]RequestDto> _validator;
    private readonly ILogger<[Modulo]Service> _logger;

    public [Modulo]Service(
        I[Modulo]Repository repository,
        IValidator<[Modulo]RequestDto> validator,
        ILogger<[Modulo]Service> logger)
    {
        _repository = repository;
        _validator  = validator;
        _logger     = logger;
    }

    // ── GET ALL ──────────────────────────────────────────────────────
    public async Task<IEnumerable<[Modulo]ResponseDto>> GetAllAsync(Guid empresaId)
    {
        _logger.LogInformation("Getting all {Modulo} for empresa {EmpresaId}", nameof([Modulo]), empresaId);
        var entidades = await _repository.GetAllAsync(empresaId);
        return entidades.Select(MapToResponseDto);
    }

    // ── GET BY ID ────────────────────────────────────────────────────
    public async Task<[Modulo]ResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var entidad = await _repository.GetByIdAsync(id, empresaId);
        return entidad != null ? MapToResponseDto(entidad) : null;
    }

    // ── CREATE ───────────────────────────────────────────────────────
    public async Task<[Modulo]ResponseDto> CreateAsync([Modulo]RequestDto dto, Guid empresaId)
    {
        // 1. Validar
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 2. Mapear DTO → Entity
        var entidad = new [Modulo]
        {
            EmpresaId = empresaId,
            Nombre    = dto.Nombre.Trim(),
            // ... mapear demás propiedades
        };

        // 3. Persistir
        var id = await _repository.CreateAsync(entidad);
        _logger.LogInformation("Created {Modulo} {Id} for empresa {EmpresaId}", nameof([Modulo]), id, empresaId);

        // 4. Retornar registro creado
        var creado = await _repository.GetByIdAsync(id, empresaId);
        return MapToResponseDto(creado!);
    }

    // ── UPDATE ───────────────────────────────────────────────────────
    public async Task<[Modulo]ResponseDto> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId)
    {
        // 1. Verificar existencia
        var existente = await _repository.GetByIdAsync(id, empresaId)
            ?? throw new KeyNotFoundException($"[Modulo] {id} no encontrado");

        // 2. Validar
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 3. Actualizar entity
        existente.Nombre = dto.Nombre.Trim();
        // ... mapear demás propiedades

        await _repository.UpdateAsync(existente);
        _logger.LogInformation("Updated {Modulo} {Id}", nameof([Modulo]), id);

        // 4. Retornar actualizado
        var actualizado = await _repository.GetByIdAsync(id, empresaId);
        return MapToResponseDto(actualizado!);
    }

    // ── DEACTIVATE ───────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var resultado = await _repository.DeactivateAsync(id, empresaId);
        if (resultado)
            _logger.LogInformation("Deactivated {Modulo} {Id}", nameof([Modulo]), id);
        return resultado;
    }

    // ── MAPEO PRIVADO ────────────────────────────────────────────────
    private static [Modulo]ResponseDto MapToResponseDto([Modulo] e) => new()
    {
        Id                = e.Id,
        Nombre            = e.Nombre,
        Activo            = e.Activo,
        FechaCreacion     = e.FechaCreacion,
        FechaModificacion = e.FechaModificacion
    };
}
```

**Mapeo de estados TMS con label en español:**
```csharp
private static string GetEstadoLabel(string estado) => estado switch
{
    "DRAFT"            => "Borrador",
    "CONFIRMED"        => "Confirmado",
    "ASSIGNED"         => "Asignado",
    "PICKUP_SCHEDULED" => "Pickup programado",
    "IN_TRANSIT"       => "En tránsito",
    "DELIVERED"        => "Entregado",
    "INVOICED"         => "Facturado",
    "CLOSED"           => "Cerrado",
    "CANCELLED"        => "Cancelado",
    "ON_HOLD"          => "En espera",
    "FAILED_DELIVERY"  => "Entrega fallida",
    _                  => estado
};
```

### 4. Cambio de Estado (State Machine TMS)

```csharp
// Freiroute.BLL/Services/EmbarqueService.cs — método adicional
public async Task<EmbarqueResponseDto> CambiarEstadoAsync(
    Guid id, string nuevoEstado, Guid empresaId)
{
    var embarque = await _repository.GetByIdAsync(id, empresaId)
        ?? throw new KeyNotFoundException($"Embarque {id} no encontrado");

    // Validar transición permitida
    if (!EsTransicionValida(embarque.Estado, nuevoEstado))
        throw new BusinessException(
            $"No se puede cambiar de {embarque.Estado} a {nuevoEstado}");

    embarque.Estado = nuevoEstado;
    await _repository.UpdateAsync(embarque);

    _logger.LogInformation(
        "Embarque {Id} changed state from {From} to {To}",
        id, embarque.Estado, nuevoEstado);

    return MapToResponseDto(embarque);
}

private static bool EsTransicionValida(string estadoActual, string nuevoEstado)
{
    var transicionesPermitidas = new Dictionary<string, string[]>
    {
        ["DRAFT"]            = ["CONFIRMED", "CANCELLED"],
        ["CONFIRMED"]        = ["ASSIGNED", "CANCELLED", "ON_HOLD"],
        ["ASSIGNED"]         = ["PICKUP_SCHEDULED", "CANCELLED", "ON_HOLD"],
        ["PICKUP_SCHEDULED"] = ["IN_TRANSIT", "CANCELLED"],
        ["IN_TRANSIT"]       = ["DELIVERED", "FAILED_DELIVERY", "ON_HOLD"],
        ["DELIVERED"]        = ["INVOICED"],
        ["INVOICED"]         = ["CLOSED"],
        ["ON_HOLD"]          = ["CONFIRMED", "CANCELLED"],
        ["FAILED_DELIVERY"]  = ["IN_TRANSIT", "CANCELLED"]
    };

    return transicionesPermitidas.TryGetValue(estadoActual, out var permitidos)
           && permitidos.Contains(nuevoEstado);
}
```

### 5. ApiResponse<T> Wrapper

```csharp
// Freiroute.Utility/ApiResponse.cs
namespace Freiroute.Utility;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "OK") => new()
    {
        Success = true,
        Message = message,
        Data    = data
    };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors  = errors ?? []
    };
}
```

### 6. API Controller

```csharp
// Freiroute.API/Controllers/[Modulo]Controller.cs
namespace Freiroute.API.Controllers;

/// <summary>
/// Controlador REST para la gestión de [Modulo] del TMS Freiroute.
/// Requiere autenticación JWT y permisos granulares por operación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class [Modulo]Controller : ControllerBase
{
    private readonly I[Modulo]Service _service;
    private readonly ILogger<[Modulo]Controller> _logger;

    public [Modulo]Controller(I[Modulo]Service service, ILogger<[Modulo]Controller> logger)
    {
        _service = service;
        _logger  = logger;
    }

    private Guid EmpresaId => Guid.Parse(User.FindFirstValue("empresa_id")!);

    /// <summary>Obtiene todos los [modulo] activos de la empresa del usuario autenticado.</summary>
    [HttpGet]
    [RequirePermission("[modulo]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<[Modulo]ResponseDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync(EmpresaId);
        return Ok(ApiResponse<IEnumerable<[Modulo]ResponseDto>>.Ok(result));
    }

    /// <summary>Obtiene un [modulo] por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id, EmpresaId);
        if (result is null)
            return NotFound(ApiResponse<[Modulo]ResponseDto>.Fail($"[Modulo] {id} no encontrado"));
        return Ok(ApiResponse<[Modulo]ResponseDto>.Ok(result));
    }

    /// <summary>Crea un nuevo [modulo].</summary>
    [HttpPost]
    [RequirePermission("[modulo]", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create([FromBody] [Modulo]RequestDto dto)
    {
        var result = await _service.CreateAsync(dto, EmpresaId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<[Modulo]ResponseDto>.Ok(result, "[Modulo] creado exitosamente"));
    }

    /// <summary>Actualiza un [modulo] existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] [Modulo]RequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, EmpresaId);
        return Ok(ApiResponse<[Modulo]ResponseDto>.Ok(result, "[Modulo] actualizado exitosamente"));
    }

    /// <summary>Desactiva (eliminación lógica) un [modulo]. No elimina físicamente.</summary>
    [HttpDelete("{id:guid}/deactivate")]
    [RequirePermission("[modulo]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _service.DeactivateAsync(id, EmpresaId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail($"[Modulo] {id} no encontrado"));
        return Ok(ApiResponse<bool>.Ok(true, "[Modulo] desactivado exitosamente"));
    }
}
```

### 7. Global Exception Handler

```csharp
// Freiroute.API/Middleware/GlobalExceptionMiddleware.cs
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            var errores = ex.Errors.Select(e => e.ErrorMessage).ToList();
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("Error de validación", errores));
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (BusinessException ex)
        {
            context.Response.StatusCode = 422;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("Error interno del servidor"));
        }
    }
}

// Freiroute.Utility/Exceptions/BusinessException.cs
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
```

### 8. Checklist de Entregable Backend

- [ ] **FluentValidator** con mensajes en español para todos los campos del RequestDto
- [ ] **BLL Service** con GetAll, GetById, Create, Update, Deactivate + métodos de negocio TMS
- [ ] **Mapeo privado** `MapToResponseDto` en el Service (no en el Controller)
- [ ] **State machine** si el módulo tiene estados (Órdenes, Embarques, etc.)
- [ ] **API Controller** con `[Authorize]`, `[RequirePermission]`, `ApiResponse<T>` en todos los endpoints
- [ ] **Swagger** documentado con `/// <summary>` en cada endpoint y `[ProducesResponseType]`
- [ ] **Logging** con Serilog en operaciones de Create, Update, Deactivate y cambios de estado
- [ ] **GlobalExceptionMiddleware** registrado en Program.cs
- [ ] Tests unitarios BLL con cobertura ≥ 80% (ver skill-testing.md)
- [ ] Tests de integración API con cobertura ≥ 60% (ver skill-testing.md)
- [ ] `dotnet build` sin warnings
- [ ] `dotnet test` sin fallos

---

## Contexto Freiroute TMS

@BackendDev implementa la lógica de transporte asegurando que cada operación filtre por `empresa_id`, los estados de embarque sigan transiciones válidas, los cálculos de flete consideren tarifas y recargos, y los reportes de OTD, utilización de flota y costos sean precisos. Los módulos críticos con reglas de negocio complejas son: Órdenes, Embarques, Carriers, Rutas, Track & Trace y Freight Audit.
