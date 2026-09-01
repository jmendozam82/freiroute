# Skill: @BackendDev (Desarrollador Backend freiroute TMS)

## Rol
**@BackendDev** implementa la lógica de negocio (BLL Services), controladores API REST y validaciones FluentValidation. Es responsable de que cada operación del módulo respete la arquitectura N-Tier: Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL. Actúa después de @IngenieroDatos y entrega al @QA para pruebas.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Verificar Entity definida por @Arquitecto
4. Revisar migración SQL creada por @IngenieroDatos
5. Confirmar interfaces DAL existen (I[Modulo]Repository)
```

### 2. Posición en el Flujo de HU
```
@PM planifica Sprint
    → @Arquitecto define Entity + DTOs + Interfaces + ADR
    → @IngenieroDatos crea migración SQL + RLS
    → @BackendDev ← IMPLEMENTA BLL Service + FluentValidator + API Controller
    → @QA ejecuta tests + valida cobertura
    → @FrontendDev crea Vistas Razor con Design System Freiroute
    → @PM revisa checklist + aprueba PR
```

### 3. Business Logic Layer (BLL) - Services

**Implementar en:** `src/Freiroute.BLL/Services/[Modulo]Service.cs`

#### Estructura base del Service
```csharp
// ── src/Freiroute.BLL/Services/[Modulo]Service.cs ──────────────────────
namespace Freiroute.BLL.Services;

using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.[Modulo];
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using Microsoft.Extensions.Logging;
using FluentValidation;

/// <summary>
/// Servicio de negocio para [descripción del módulo TMS].
/// Encapsula reglas de dominio, coordinando DAL Repository y validaciones.
/// </summary>
public class [Modulo]Service : I[Modulo]Service
{
    private readonly I[Modulo]Repository _repository;
    private readonly ILogger<[Modulo]Service> _logger;

    public [Modulo]Service(
        I[Modulo]Repository repository,
        ILogger<[Modulo]Service> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── GETALL ──────────────────────────────────────────────────────────
    /// <summary>
    /// Obtiene todos los registros activos del módulo para una empresa.
    /// Filtro: activo = true (soft delete) + empresa_id (RLS).
    /// </summary>
    public async Task<IEnumerable<[Modulo]ResponseDto>> GetAllAsync(Guid empresaId)
    {
        _logger.LogInformation("Consultando todos los [modulo] para empresa {EmpresaId}", empresaId);
        
        var entidades = await _repository.GetAllAsync(empresaId);
        return entidades.Select(MapToResponseDto).ToList();
    }

    // ── GETBYID ─────────────────────────────────────────────────────────
    /// <summary>
    /// Obtiene un registro individual por ID dentro del tenant.
    /// Retorna null si no existe o está desactivado.
    /// </summary>
    public async Task<[Modulo]ResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        _logger.LogDebug("Consultando [modulo] {Id} para empresa {EmpresaId}", id, empresaId);
        
        var entidad = await _repository.GetByIdAsync(id, empresaId);
        return entidad != null ? MapToResponseDto(entidad) : null;
    }

    // ── CREATE ──────────────────────────────────────────────────────────
    /// <summary>
    /// Crea un nuevo registro validando reglas de negocio del dominio TMS.
    /// El empresa_id viene del JWT, nunca del request.
    /// </summary>
    public async Task<[Modulo]ResponseDto> CreateAsync([Modulo]RequestDto dto, Guid empresaId)
    {
        _logger.LogInformation("Creando [modulo] para empresa {EmpresaId}", empresaId);

        // 1. Validar DTO con FluentValidation
        var validator = new [Modulo]Validator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Reglas de negocio adicionales del dominio
        ValidateBusinessRules(dto, empresaId);

        // 3. Mapear DTO a Entity (sin campos de auditoría — los genera la BD)
        var entidad = MapToEntity(dto, empresaId);

        // 4. Persistir y obtener ID generado
        var id = await _repository.CreateAsync(entidad);

        _logger.LogInformation("[modulo] creado exitosamente {Id}", id);

        // 5. Retornar el registro completo creado
        var creado = await _repository.GetByIdAsync(id, empresaId);
        return MapToResponseDto(creado!);
    }

    // ── UPDATE ──────────────────────────────────────────────────────────
    /// <summary>
    /// Actualiza un registro existente. Solo modifica campos del DTO enviado.
    /// Verifica existencia antes de actualizar.
    /// </summary>
    public async Task<[Modulo]ResponseDto> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId)
    {
        _logger.LogInformation("Actualizando [modulo] {Id} para empresa {EmpresaId}", id, empresaId);

        // 1. Validar DTO
        var validator = new [Modulo]Validator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Verificar que el registro existe y es del tenant
        var existente = await _repository.GetByIdAsync(id, empresaId);
        if (existente == null)
        {
            _logger.LogWarning("[modulo] {Id} no encontrado para empresa {EmpresaId}", id, empresaId);
            throw new KeyNotFoundException($"El registro {id} no existe o fue desactivado");
        }

        // 3. Aplicar cambios sobre la entidad existente
        ApplyUpdates(existente, dto);
        existente.FechaModificacion = DateTime.UtcNow;

        // 4. Persistir
        var actualizado = await _repository.UpdateAsync(existente);
        if (!actualizado)
            throw new KeyNotFoundException($"No se pudo actualizar el registro {id}");

        _logger.LogInformation("[modulo] {Id} actualizado exitosamente", id);

        // 5. Retornar el registro actualizado
        return await _repository.GetByIdAsync(id, empresaId)
            .ContinueWith(t => MapToResponseDto(t.Result!));
    }

    // ── DEACTIVATE (SOFT DELETE) ────────────────────────────────────────
    /// <summary>
    /// Desactiva un registro lógicamente (activo = false).
    /// Nunca se usa DeleteAsync — ver ADR-005.
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        _logger.LogInformation("Desactivando [modulo] {Id} para empresa {EmpresaId}", id, empresaId);

        var resultado = await _repository.DeactivateAsync(id, empresaId);

        if (resultado)
            _logger.LogInformation("[modulo] {Id} desactivado exitosamente", id);
        else
            _logger.LogWarning("[modulo] {Id} no encontrado para desactivar", id);

        return resultado;
    }
}
```

#### Método de mapeo DTO ↔ Entity
```csharp
private static [Modulo] MapToEntity([Modulo]RequestDto dto, Guid empresaId)
{
    return new [Modulo]
    {
        EmpresaId = empresaId,          // SIEMPRE del JWT
        Nombre = dto.Nombre.Trim(),
        // Campos específicos del módulo TMS
        Estado = dto.Estado,
        // ...
    };
}

private static [Modulo]ResponseDto MapToResponseDto([Modulo] entity)
{
    return new [Modulo]ResponseDto
    {
        Id = entity.Id,
        Nombre = entity.Nombre,
        Estado = entity.Estado,
        // Agregar label de estado en español para UI
        EstadoLabel = GetEstadoLabel(entity.Estado),
        Activo = entity.Activo,
        FechaCreacion = entity.FechaCreacion,
        FechaModificacion = entity.FechaModificacion
    };
}

private static string GetEstadoLabel(string estado)
{
    return estado switch
    {
        "DRAFT"               => "Borrador",
        "CONFIRMED"           => "Confirmado",
        "ASSIGNED"            => "Asignado",
        "IN_TRANSIT"          => "En tránsito",
        "DELIVERED"           => "Entregado",
        "FAILED_DELIVERY"     => "Entrega fallida",
        "ON_HOLD"             => "En espera",
        "CANCELLED"           => "Cancelado",
        _                     => estado
    };
}
```

### 4. FluentValidation — Validators

**Implementar en:** `src/Freiroute.BLL/Validators/[Modulo]Validator.cs`

#### Pattern genérico del Validator
```csharp
namespace Freiroute.BLL.Validators;

using Freiroute.DTO.[Modulo];
using Freiroute.Utility.Constants;
using FluentValidation;

/// <summary>
/// Validación de reglas de entrada para [Modulo]RequestDto.
/// Los mensajes están en español conforme a la convención de idioma.
/// </summary>
public class [Modulo]Validator : AbstractValidator<[Modulo]RequestDto>
{
    public [Modulo]Validator()
    {
        // ── Campo obligatorio genérico ──────────────────────────────
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .NotNull().WithMessage("El nombre no puede ser nulo")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres");

        // ── Campos numéricos del dominio TMS ─────────────────────────
        RuleFor(x => x.PesoTotal)
            .GreaterThanOrEqualTo(0).WithMessage("El peso total no puede ser negativo")
            .When(x => x.PesoTotal.HasValue)
            .LessThan(50000).WithMessage("El peso no puede exceder 50,000 kg (límite máximo de carga)");

        RuleFor(x => x.CostoFlete)
            .GreaterThan(0).WithMessage("El costo de flete debe ser mayor a 0")
            .LessThan(999999999m).WithMessage("El costo de flete excede el máximo permitido");

        // ── Campos de fecha ─────────────────────────────────────────
        RuleFor(x => x.FechaEntregaRequerida)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("La fecha de entrega requerida debe ser hoy o futura")
            .When(x => x.FechaEntregaRequerida.HasValue);

        // ── Estado válido según constantes de dominio ────────────────
        RuleFor(x => x.Estado)
            .Must((dto, estado) => IsValidEstado(estado))
            .WithMessage("Estado inválido. Consulte los valores permitidos del módulo.");

        // ── Regla dependiente (solo cuando aplica) ──────────────────
        RuleFor(x => x.Observaciones)
            .MaximumLength(2000).WithMessage("Las observaciones no pueden exceder 2000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Observaciones));
    }

    private bool IsValidEstado(string? estado)
    {
        if (string.IsNullOrEmpty(estado)) return false;
        
        return estado is
            OrdenStatus.Draft or
            OrdenStatus.Confirmed or
            OrdenStatus.Assigned or
            OrdenStatus.InTransit or
            OrdenStatus.Delivered or
            OrdenStatus.Cancelled or
            OrdenStatus.OnHold or
            OrdenStatus.FailedDelivery;
    }
}
```

### 5. API Controllers — Endpoints REST

**Implementar en:** `src/Freiroute.API/Controllers/[Modulo]Controller.cs`

#### Controller completo con permisos y wrapper
```csharp
// ── src/Freiroute.API/Controllers/[Modulo]Controller.cs ────────────────
namespace Freiroute.API.Controllers;

using Freiroute.API.Filters;       // RequirePermission filter
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.[Modulo];
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controlador REST para el módulo [Modulo].
/// Todos los endpoints requieren autenticación JWT y permiso específico.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]   // Valida token JWT en cada llamada
[Produces("application/json")]
public class [Modulo]Controller : ControllerBase
{
    private readonly I[Modulo]Service _service;
    private readonly ILogger<[Modulo]Controller> _logger;

    public [Modulo]Controller(
        I[Modulo]Service service,
        ILogger<[Modulo]Controller> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── GET api/[modulo] ───────────────────────────────────────────────
    /// <summary>Obtiene todos los registros activos del módulo.</summary>
    [HttpGet]
    [RequirePermission("[modulo]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<[Modulo]ResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var empresaId = User.GetEmpresaIdFromClaims();
        var result = await _service.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<[Modulo]ResponseDto>>.Ok(result, "Consulta exitosa"));
    }

    // ── GET api/[modulo]/:id ───────────────────────────────────────────
    /// <summary>Obtiene un registro por su identificador único.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var empresaId = User.GetEmpresaIdFromClaims();
        var result = await _service.GetByIdAsync(id, empresaId);
        
        if (result == null)
            return NotFound(ApiResponse<Unit>.Error("Registro no encontrado"));
        
        return Ok(ApiResponse<[Modulo]ResponseDto>.Ok(result, "Consulta exitosa"));
    }

    // ── POST api/[modulo] ──────────────────────────────────────────────
    /// <summary>Crea un nuevo registro para la empresa del usuario.</summary>
    [HttpPost]
    [RequirePermission("[modulo]", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] [Modulo]RequestDto dto)
    {
        var empresaId = User.GetEmpresaIdFromClaims();

        try
        {
            var result = await _service.CreateAsync(dto, empresaId);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<[Modulo]ResponseDto>.Ok(result, "Registro creado exitosamente"));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validación fallida al crear [modulo]");
            return BadRequest(ApiResponse<List<string>>.Error(ex.Errors.Select(e => e.ErrorMessage).ToList()));
        }
    }

    // ── PUT api/[modulo]/:id ───────────────────────────────────────────
    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<[Modulo]ResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] [Modulo]RequestDto dto)
    {
        var empresaId = User.GetEmpresaIdFromClaims();

        try
        {
            var result = await _service.UpdateAsync(id, dto, empresaId);
            return Ok(ApiResponse<[Modulo]ResponseDto>.Ok(result, "Registro actualizado exitosamente"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<Unit>.Error("Registro no encontrado o ya desactivado"));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponse<List<string>>.Error(ex.Errors.Select(e => e.ErrorMessage).ToList()));
        }
    }

    // ── POST api/[modulo]/:id/deactivate ───────────────────────────────
    /// <summary>Desactiva lógicamente un registro (soft delete).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission("[modulo]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetEmpresaIdFromClaims();

        try
        {
            var result = await _service.DeactivateAsync(id, empresaId);
            if (!result)
                return NotFound(ApiResponse<Unit>.Error("Registro no encontrado o ya desactivado"));
            
            return Ok(ApiResponse<Unit>.Ok(Unit.Instance, "Registro desactivado exitosamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar [modulo] {Id}", id);
            return StatusCode(500, ApiResponse<Unit>.Error("Error interno al procesar la solicitud"));
        }
    }
}
```

### 6. Patrón ApiResponse<T>

**Respuesta envuelta en TODOS los endpoints.** Nunca retornar tipos puros:
```csharp
// ── src/Freiroute.Utility/ApiResponse/ApiResponse{T}.cs ───────────────
namespace Freiroute.Utility.ApiResponse;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Error(string message, List<string>? details = null) =>
        new() { Success = false, Message = message, Errors = details };
}

public class Unit { public static Unit Instance => new(); }
```

### 7. Integración con IOC (Dependency Injection)

Configurar en `Freiroute.IOC/DependencyInjection.cs` (compartido entre proyectos):
```csharp
// Servicios BLL + Validators se registran automáticamente por convención
services.Scan(scan => scan
    .FromAssemblyOf<I[Modulo]Service>()
    .AddClasses(c => c.AssignableTo(typeof(IEnumerable<>)))
        .AsImplementedInterfaces()
        .WithTransientLifetime());

services.AddScoped<IValidator<[Modulo]RequestDto>, [Modulo]Validator>();
```

### 8. Logging con Serilog

**Patrón estructurado para logs JSON:**
```csharp
// Log informativo (nivel INFO)
_logger.LogInformation("Creando embarque para empresa {EmpresaId}, operador {UsuarioId}", 
    empresaId, userId);

// Log de debug (nivel DEBUG)
_logger.LogDebug("ValidandoDTO para embarque {Numero}, campos válidos: {CampoCount}", 
    dto.Numero, campoCount);

// Log de warning (nivel WARN)
_logger.LogWarning("Estado de embarque {Id} no permitido: {EstadoActual} → {NuevoEstado}", 
    id, estadoActual, nuevoEstado);

// Log de error (nivel ERROR)
_logger.LogError(ex, "Error crítico al crear embarque para empresa {EmpresaId}", empresaId);
```

**NUNCA incluir datos sensibles en logs:**
```csharp
// ❌ PROHIBIDO
_logger.LogInformation("Usuario {Password} intentó acceder", password);
_logger.LogInformation("Token: {Token}", accessToken);

// ✅ CORRECTO
_logger.LogInformation("Usuario {UserId} con perfil {PerfilId} accede al módulo", userId, perfilId);
```

### 9. Reglas Críticas de Implementación

| # | Regla | Violación | Impacto |
|---|---|---|---|
| 1 | `empresa_id` SIEMPRE del JWT, nunca del request | DTO recibe `empresa_id` | 🔴 Multi-tenant roto |
| 2 | Métodos asíncronos terminan en `Async` | `GetAll()`, `Create()` | 🟡 Convención rota |
| 3 | Respuestas envueltas en `ApiResponse<T>` | Retornar `IEnumerable<Entity>` | 🟡 Consistencia API rota |
| 4 | Nunca `DeleteAsync`, solo `DeactivateAsync` | `DeleteAsync()` en interfaz | 🟡 ADR-005 violado |
| 5 | DTOs ≠ Entities | Exponer Entity directamente | 🔴 Data leakage |
| 6 | Mensajes de validación en español | Mensajes en inglés | 🟡 Convención de idioma |
| 7 | Serilog sin datos sensibles | Passwords/tokens en log | 🔴 Seguridad crítica |

### 10. Checklist de Entregable (revisado por @PM)

- [ ] BLL Service implementado con todos los métodos CRUD + Deactivate
- [ ] Servicio inyecta `I[Modulo]Repository` vía constructor (DI)
- [ ] `empresa_id` extraído de JWT, nunca recibido desde DTO
- [ ] FluentValidator con mensajes en español en TODAS las propiedades
- [ ] Reglas de negocio TMS específicas implementadas (estados, montos, fechas)
- [ ] API Controller con `[Authorize]` + `[RequirePermission]` en cada endpoint
- [ ] Cada endpoint retorna `ApiResponse<T>` — ningún tipo puro
- [ ] Documentación Swagger (`/// <summary>`) en cada endpoint público
- [ ] Serilog con logging estructurado en INFO, DEBUG, WARN, ERROR
- [ ] Tests unitarios escritos (ver @QA)
- [ ] Sin warnings en `dotnet build`
- [ ] `dotnet test` pasa sin fallos

### 11. Contexto Freiroute TMS

@BackendDev implementa la lógica de negocio para el sistema de gestión de transporte más ambicioso del mercado:

**Módulos MVP (Sprints 1–11):**
- **EP-01:** Auth multi-tenant, roles (SUPER_ADMIN, ADMIN, OPERADOR, CONDUCTOR, CLIENTE)
- **EP-03:** Maestros — Empresas, Clientes, Ubicaciones, Vehículos, Conductores, Carriers
- **EP-04:** Order Management — Órdenes (DRAFT→CONFIRMED→CLOSED), consolidación LTL/FTL
- **EP-05:** Carrier Management — Tarifas, contratos, evaluación de desempeño
- **EP-06:** Shipment Planning — Embarques, asignación a carriers/conductores, optimización
- **EP-07:** Route Optimization — Secuencia de paradas, restricciones de capacidad
- **EP-08:** Track & Trace — Posiciones GPS en tiempo real, geofences, ETA dinámico
- **EP-09:** Document Management — Carta de porte, POD digital, facturación electrónica

**Reglas de dominio críticas:**
- OTD (On-Time Delivery): % de entregas a tiempo vs SLA contractual
- Geocodificación inversa de direcciones de origen/destino
- Cálculo de costos: tarifa base + recargos combustible + peajes + maniobras
- Estados de embarque con máquina de estados finita (nunca saltar etapas)
- Integración con APIs de rastreo GPS (Teltonika, Queclink, Verizon Connect)
- Numeración de documentos con prefijos configurables: `FR-{YYYY}-{NNNNN}`

**Filosofía:** La lógica de negocio es sagrada — cada regla TMS documentada en el spec debe estar reflejada como código verificable en tests unitarios. Si no hay un test que lo pruebe, no existe.

---

## Dependencias entre Agentes

| Recibe de | Entrega a | Formato de handoff |
|---|---|---|
| @Arquitecto | Entity + DTOs + Interfaces | Archivos creados en sus ubicaciones correctas |
| @IngenieroDatos | Migración SQL aplicada + tabla lista | `supabase db push` completado |
| @QA | Código implementado listo para testear | PR abierto con changes |
| @FrontendDev | API endpoints documentados en Swagger | URLs base `/api/[modulo]` |
