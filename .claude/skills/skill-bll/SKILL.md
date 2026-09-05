---
description: Patrón BLL Services y FluentValidation para Freiroute TMS. Úsalo para implementar la Business Logic Layer, crear API Controllers con autenticación JWT y permisos granulares, y desarrollar FluentValidators con mensajes en español.
---

# Skill: BLL — Business Logic Layer Freiroute TMS

## Patrón Service Estándar

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
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<[Modulo]ResponseDto>>> GetAllAsync(Guid empresaId)
    {
        try
        {
            var data = await _repository.GetAllAsync(empresaId);
            return ApiResponse<IEnumerable<[Modulo]ResponseDto>>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving [modulo] list for empresa {EmpresaId}", empresaId);
            return ApiResponse<IEnumerable<[Modulo]ResponseDto>>.Failure(
                "Error al obtener los registros. Intente de nuevo.");
        }
    }

    public async Task<ApiResponse<[Modulo]ResponseDto>> GetByIdAsync(Guid id, Guid empresaId)
    {
        try
        {
            var item = await _repository.GetByIdAsync(id, empresaId);
            return item is not null
                ? ApiResponse<[Modulo]ResponseDto>.Success(item)
                : ApiResponse<[Modulo]ResponseDto>.Failure("Registro no encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving [modulo] {Id} for empresa {EmpresaId}", id, empresaId);
            return ApiResponse<[Modulo]ResponseDto>.Failure("Error al obtener el registro.");
        }
    }

    public async Task<ApiResponse<Guid>> CreateAsync([Modulo]RequestDto dto, Guid empresaId)
    {
        // 1. Validar DTO
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponse<Guid>.ValidationFailure(
                validation.Errors.Select(e => e.ErrorMessage));
        }

        try
        {
            var id = await _repository.CreateAsync(dto, empresaId);
            _logger.LogInformation("[Modulo] {Id} created for empresa {EmpresaId}", id, empresaId);
            return ApiResponse<Guid>.Success(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating [modulo] for empresa {EmpresaId}", empresaId);
            return ApiResponse<Guid>.Failure("Error al crear el registro.");
        }
    }

    public async Task<ApiResponse<bool>> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponse<bool>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var result = await _repository.UpdateAsync(id, dto, empresaId);
            return result
                ? ApiResponse<bool>.Success(true)
                : ApiResponse<bool>.Failure("Registro no encontrado o sin cambios.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating [modulo] {Id}", id);
            return ApiResponse<bool>.Failure("Error al actualizar el registro.");
        }
    }

    // NUNCA DeleteAsync — siempre DeactivateAsync (soft delete)
    public async Task<ApiResponse<bool>> DeactivateAsync(Guid id, Guid empresaId)
    {
        try
        {
            var result = await _repository.DeactivateAsync(id, empresaId);
            return result
                ? ApiResponse<bool>.Success(true)
                : ApiResponse<bool>.Failure("Registro no encontrado o ya inactivo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating [modulo] {Id}", id);
            return ApiResponse<bool>.Failure("Error al desactivar el registro.");
        }
    }
}
```

## FluentValidator — Mensajes en Español

```csharp
// Freiroute.BLL/Validators/[Modulo]RequestDtoValidator.cs
namespace Freiroute.BLL.Validators;

public class [Modulo]RequestDtoValidator : AbstractValidator<[Modulo]RequestDto>
{
    public [Modulo]RequestDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.");

        // Para campos de monto/tarifa
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .When(x => x.Monto.HasValue);

        // Para fechas de negocio
        RuleFor(x => x.FechaETA)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("La fecha estimada de llegada no puede ser anterior a hoy.")
            .When(x => x.FechaETA.HasValue);
    }
}
```

## API Controller Estándar

```csharp
// Freiroute.API/Controllers/[Modulo]Controller.cs
namespace Freiroute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class [Modulo]Controller : ControllerBase
{
    private readonly I[Modulo]Service _service;
    private readonly ILogger<[Modulo]Controller> _logger;

    public [Modulo]Controller(I[Modulo]Service service, ILogger<[Modulo]Controller> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene la lista de [entidades] del tenant actual.</summary>
    [HttpGet]
    [RequirePermission("[modulo]", PermissionType.READ)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<[Modulo]ResponseDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll()
    {
        var empresaId = User.GetEmpresaId();
        var result = await _service.GetAllAsync(empresaId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Obtiene un [entidad] por ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.READ)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var result = await _service.GetByIdAsync(id, empresaId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>Crea un nuevo [entidad].</summary>
    [HttpPost]
    [RequirePermission("[modulo]", PermissionType.CREATE)]
    public async Task<IActionResult> Create([FromBody] [Modulo]RequestDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var result = await _service.CreateAsync(dto, empresaId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result)
            : BadRequest(result);
    }

    /// <summary>Actualiza un [entidad] existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("[modulo]", PermissionType.UPDATE)]
    public async Task<IActionResult> Update(Guid id, [FromBody] [Modulo]RequestDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var result = await _service.UpdateAsync(id, dto, empresaId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Desactiva un [entidad] (soft delete).</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission("[modulo]", PermissionType.UPDATE)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var result = await _service.DeactivateAsync(id, empresaId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
    // ❌ NO existe endpoint DELETE — solo Deactivate
}
```

## ApiResponse<T> — Wrapper Estándar

```csharp
// Freiroute.Utility/Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T data, string? message = null)
        => new() { IsSuccess = true, Data = data, Message = message };

    public static ApiResponse<T> Failure(string message)
        => new() { IsSuccess = false, Message = message };

    public static ApiResponse<T> ValidationFailure(IEnumerable<string> errors)
        => new() { IsSuccess = false, Errors = errors, Message = "Error de validación." };
}
```

## Registro en IOC

```csharp
// Freiroute.IOC/DependencyInjection.cs
// Repositorios DAL
services.AddScoped<I[Modulo]Repository, [Modulo]Repository>();

// Services BLL
services.AddScoped<I[Modulo]Service, [Modulo]Service>();

// Validators FluentValidation
services.AddScoped<IValidator<[Modulo]RequestDto>, [Modulo]RequestDtoValidator>();
```

## Tipos de Permisos

```csharp
// Solo existen 3 tipos de permiso en Freiroute TMS
public enum PermissionType { READ, CREATE, UPDATE }
// ❌ No existe DELETE
```
