---
description: Convenciones de arquitectura N-Tier para Freiroute TMS. Úsalo para crear Entity, DTOs, interfaces de repositorio y servicio, y Architecture Decision Records (ADRs). Referencia para convenciones C# y SQL del proyecto.
---

# Skill: Arquitecto — Convenciones de Arquitectura Freiroute TMS

## Orden de Creación por Módulo

Para cada nuevo módulo (ej: `embarques`, `carriers`, `ordenes`):

```
1. Entity         → Freiroute.Entity/[Modulo].cs
2. RequestDto     → Freiroute.DTO/[Modulo]/[Modulo]RequestDto.cs
3. ResponseDto    → Freiroute.DTO/[Modulo]/[Modulo]ResponseDto.cs
4. DAL Interface  → Freiroute.DAL/Interfaces/I[Modulo]Repository.cs
5. BLL Interface  → Freiroute.BLL/Interfaces/I[Modulo]Service.cs
6. ADR (si aplica)→ docs/adr/ADR-NNN-descripcion.md
```

## Plantillas Estándar

### Entity (`Freiroute.Entity/`)

```csharp
namespace Freiroute.Entity;

/// <summary>
/// Entidad [Modulo] del módulo de [descripción del dominio TMS].
/// </summary>
public class [Modulo]
{
    // ── Campos base obligatorios (NO modificar) ─────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK tenant
    public bool Activo { get; set; } = true;            // Soft delete
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos de negocio del módulo ────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    // ... propiedades específicas del módulo TMS
}
```

### Request DTO (`Freiroute.DTO/[Modulo]/`)

```csharp
namespace Freiroute.DTO.[Modulo];

/// <summary>DTO de solicitud para crear/actualizar [Modulo].</summary>
public class [Modulo]RequestDto
{
    public string Nombre { get; set; } = string.Empty;
    // ... campos editables (NO incluir Id, EmpresaId, Activo, FechaCreacion)
}
```

### Response DTO (`Freiroute.DTO/[Modulo]/`)

```csharp
namespace Freiroute.DTO.[Modulo];

/// <summary>DTO de respuesta para lecturas de [Modulo].</summary>
public class [Modulo]ResponseDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    // ... campos de negocio para mostrar
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
```

### DAL Interface (`Freiroute.DAL/Interfaces/`)

```csharp
namespace Freiroute.DAL.Interfaces;

public interface I[Modulo]Repository
{
    Task<IEnumerable<[Modulo]ResponseDto>> GetAllAsync(Guid empresaId);
    Task<[Modulo]ResponseDto?> GetByIdAsync(Guid id, Guid empresaId);
    Task<Guid> CreateAsync([Modulo]RequestDto dto, Guid empresaId);
    Task<bool> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId);
    Task<bool> DeactivateAsync(Guid id, Guid empresaId); // NUNCA DeleteAsync
}
```

### BLL Interface (`Freiroute.BLL/Interfaces/`)

```csharp
namespace Freiroute.BLL.Interfaces;

public interface I[Modulo]Service
{
    Task<ApiResponse<IEnumerable<[Modulo]ResponseDto>>> GetAllAsync(Guid empresaId);
    Task<ApiResponse<[Modulo]ResponseDto>> GetByIdAsync(Guid id, Guid empresaId);
    Task<ApiResponse<Guid>> CreateAsync([Modulo]RequestDto dto, Guid empresaId);
    Task<ApiResponse<bool>> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId);
    Task<ApiResponse<bool>> DeactivateAsync(Guid id, Guid empresaId);
}
```

## Tipos de Datos por Dominio TMS

| Campo | Tipo C# | Tipo SQL | Notas |
|---|---|---|---|
| IDs | `Guid` | `UUID` | `gen_random_uuid()` en BD |
| Nombres | `string` | `VARCHAR(200)` | NOT NULL |
| Montos/Tarifas | `decimal` | `NUMERIC(18,4)` | Para tarifas y costos |
| Coordenadas GPS | `double` | `DOUBLE PRECISION` | Para lat/lng |
| Estados | `string` | `VARCHAR(50)` | Usar constantes estáticas |
| Pesos | `decimal` | `NUMERIC(10,3)` | En kg |
| Volúmenes | `decimal` | `NUMERIC(10,3)` | En m³ |
| Fechas de negocio | `DateTime` | `TIMESTAMPTZ` | Siempre con timezone |
| Documentos/blobs | N/A | N/A | Solo URL de Supabase Storage |

## Constantes de Estados de Embarque

```csharp
// Freiroute.Utility/Constants/EmbarqueStatus.cs
public static class EmbarqueStatus
{
    public const string Draft = "DRAFT";
    public const string Confirmed = "CONFIRMED";
    public const string Assigned = "ASSIGNED";
    public const string InTransit = "IN_TRANSIT";
    public const string Delivered = "DELIVERED";
    public const string FailedDelivery = "FAILED_DELIVERY";
    public const string OnHold = "ON_HOLD";
    public const string Cancelled = "CANCELLED";
}
```

## Template ADR (`docs/adr/ADR-NNN-descripcion.md`)

```markdown
# ADR-NNN: [Título de la Decisión]

**Fecha:** YYYY-MM-DD
**Estado:** [Propuesto | Aceptado | Deprecado | Reemplazado por ADR-XXX]

## Contexto
[Descripción del problema o situación que requiere una decisión]

## Decisión
[La decisión arquitectónica tomada, en términos claros]

## Consecuencias
**Positivas:**
- [Beneficio 1]

**Negativas / Trade-offs:**
- [Trade-off 1]

## Alternativas Consideradas
1. **[Alternativa A]**: [Por qué se descartó]
2. **[Alternativa B]**: [Por qué se descartó]
```

## Convenciones de Nomenclatura

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases C# | PascalCase | `EmbarqueService` |
| Métodos C# | PascalCase + Async | `GetAllAsync()` |
| Interfaces | `I` + PascalCase | `IEmbarqueService` |
| Variables C# | camelCase | `empresaId` |
| Tablas SQL | snake_case | `embarques` |
| Columnas SQL | snake_case | `empresa_id` |
| Índices SQL | `idx_[tabla]_[campo]` | `idx_embarques_empresa_id` |
| Triggers SQL | `trg_[tabla]_[accion]` | `trg_embarques_fecha_modificacion` |
