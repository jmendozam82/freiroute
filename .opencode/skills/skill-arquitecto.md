# Skill: @Arquitecto (Arquitecto de Solución Freiroute TMS)

## Rol
**@Arquitecto** define la estructura técnica del proyecto, crea las entidades, DTOs, interfaces y Architecture Decision Records (ADRs). Es el primero en actuar sobre cada HU: ningún agente escribe código de negocio hasta que @Arquitecto entregue los contratos de capa.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer el spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Verificar el ADR base relevante (docs/adr/)
4. Revisar entidades existentes para evitar duplicidades
```

### 2. Orden de Creación por Módulo

Para cada nuevo módulo (ej: `embarques`, `carriers`, `ordenes`), crear en este orden:

```
1. Entity         → Freiroute.Entity/[Modulo].cs
2. RequestDto     → Freiroute.DTO/[Modulo]/[Modulo]RequestDto.cs
3. ResponseDto    → Freiroute.DTO/[Modulo]/[Modulo]ResponseDto.cs
4. DAL Interface  → Freiroute.DAL/Interfaces/I[Modulo]Repository.cs
5. BLL Interface  → Freiroute.BLL/Interfaces/I[Modulo]Service.cs
6. ADR (si aplica)→ docs/adr/ADR-NNN-descripcion.md
```

### 3. Entidades (Entity)

**Estructura base obligatoria:**
```csharp
// Freiroute.Entity/[Modulo].cs
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

**Tipos de datos por dominio TMS:**

| Campo | Tipo C# | Tipo SQL | Notas |
|---|---|---|---|
| IDs | `Guid` | `UUID` | `gen_random_uuid()` |
| Nombres | `string` | `VARCHAR(200)` | NOT NULL |
| Montos | `decimal` | `NUMERIC(18,4)` | Para tarifas y costos |
| Coordenadas | `double` | `DOUBLE PRECISION` | Para GPS lat/lng |
| Estados | `string` | `VARCHAR(50)` | Usar constantes de dominio |
| Pesos | `decimal` | `NUMERIC(10,3)` | En kg |
| Volúmenes | `decimal` | `NUMERIC(10,3)` | En m³ |
| Fechas de negocio | `DateTime` | `TIMESTAMPTZ` | Siempre con zona horaria |
| Documentos/blobs | N/A | N/A | Solo referencia URL de Supabase Storage |

**Constantes de estados por módulo:**
```csharp
// Freiroute.Utility/Constants/[Modulo]Status.cs
public static class OrdenStatus
{
    public const string Draft = "DRAFT";
    public const string Confirmed = "CONFIRMED";
    public const string Assigned = "ASSIGNED";
    public const string PickupScheduled = "PICKUP_SCHEDULED";
    public const string InTransit = "IN_TRANSIT";
    public const string Delivered = "DELIVERED";
    public const string Invoiced = "INVOICED";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";
    public const string OnHold = "ON_HOLD";
    public const string FailedDelivery = "FAILED_DELIVERY";
}
```

### 4. DTOs

**RequestDto — Entrada (validación en BLL):**
```csharp
// Freiroute.DTO/[Modulo]/[Modulo]RequestDto.cs
namespace Freiroute.DTO.[Modulo];

/// <summary>
/// DTO de entrada para crear o actualizar un [Modulo].
/// La validación de reglas de negocio se aplica en [Modulo]Validator (BLL).
/// </summary>
public class [Modulo]RequestDto
{
    public string Nombre { get; set; } = string.Empty;
    // Solo campos que el cliente puede enviar
    // NUNCA incluir: Id, EmpresaId, Activo, FechaCreacion, FechaModificacion
}
```

**ResponseDto — Salida (lo que el cliente recibe):**
```csharp
// Freiroute.DTO/[Modulo]/[Modulo]ResponseDto.cs
namespace Freiroute.DTO.[Modulo];

/// <summary>
/// DTO de salida para consultas de [Modulo]. Nunca expone campos internos ni sensibles.
/// </summary>
public class [Modulo]ResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    // Solo campos seguros para el cliente
    // NUNCA incluir: EmpresaId directamente (ya filtrado por RLS)
}
```

**DTOs especializados del dominio TMS:**
```csharp
// Para módulos de embarque
public class EmbarqueResponseDto
{
    public Guid Id { get; set; }
    public string NumeroEmbarque { get; set; } = string.Empty;  // FR-2026-00847
    public string Estado { get; set; } = string.Empty;          // IN_TRANSIT
    public string EstadoLabel { get; set; } = string.Empty;     // "En tránsito"
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public string CarrierNombre { get; set; } = string.Empty;
    public string ConductorNombre { get; set; } = string.Empty;
    public DateTime FechaPickupPlanificada { get; set; }
    public DateTime FechaEntregaRequerida { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public DateTime? Eta { get; set; }
    public decimal PesoTotal { get; set; }
    public decimal VolumenTotal { get; set; }
    public decimal CostoFlete { get; set; }
    public bool OtdCumplido { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
```

### 5. Interfaz del Repositorio DAL

```csharp
// Freiroute.DAL/Interfaces/I[Modulo]Repository.cs
namespace Freiroute.DAL.Interfaces;

public interface I[Modulo]Repository
{
    Task<IEnumerable<[Modulo]>> GetAllAsync(Guid empresaId);
    Task<[Modulo]?> GetByIdAsync(Guid id, Guid empresaId);
    Task<Guid> CreateAsync([Modulo] entidad);
    Task<bool> UpdateAsync([Modulo] entidad);
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
    // Métodos adicionales según dominio TMS:
    // Task<IEnumerable<[Modulo]>> GetByEstadoAsync(string estado, Guid empresaId);
    // Task<IEnumerable<[Modulo]>> GetByCarrierAsync(Guid carrierId, Guid empresaId);
}
```

> ⚠️ **Prohibido** declarar `DeleteAsync`. Solo `DeactivateAsync`.

### 6. Interfaz del Servicio BLL

```csharp
// Freiroute.BLL/Interfaces/I[Modulo]Service.cs
namespace Freiroute.BLL.Interfaces;

public interface I[Modulo]Service
{
    Task<IEnumerable<[Modulo]ResponseDto>> GetAllAsync(Guid empresaId);
    Task<[Modulo]ResponseDto?> GetByIdAsync(Guid id, Guid empresaId);
    Task<[Modulo]ResponseDto> CreateAsync([Modulo]RequestDto dto, Guid empresaId);
    Task<[Modulo]ResponseDto> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId);
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);
    // Métodos de negocio TMS adicionales:
    // Task<[Modulo]ResponseDto> CambiarEstadoAsync(Guid id, string nuevoEstado, Guid empresaId);
}
```

### 7. Architecture Decision Records (ADR)

Crear cuando la decisión impacta estructuralmente el sistema. Formato:

```markdown
# ADR-NNN: [Título de la Decisión]

## Estado
[Propuesto | Aceptado | Reemplazado por ADR-XXX]

## Fecha
[YYYY-MM-DD]

## Contexto
[Por qué surgió esta decisión. El problema que resuelve.]

## Decisión
[La decisión tomada, en una oración.]

## Alternativas Consideradas
1. [Alternativa A] — Descartada porque [razón]
2. [Alternativa B] — Descartada porque [razón]

## Consecuencias
**Positivas:**
- [Beneficio 1]

**Negativas / Trade-offs:**
- [Trade-off 1]

## Módulos Afectados
- [Lista de módulos del TMS que aplica esta decisión]
```

**ADRs base ya definidos para Freiroute:**

| ADR | Decisión |
|---|---|
| ADR-001 | Stack: ASP.NET Core (.NET 8) + Supabase + Dapper |
| ADR-002 | Arquitectura N-Tier con 8 proyectos |
| ADR-003 | Multi-tenant: empresa_id discriminador + RLS PostgreSQL |
| ADR-004 | Design System Freiroute (ver AGENTS.md sección UI/UX) |
| ADR-005 | Soft delete universal: activo = false, nunca DELETE físico |
| ADR-006 | Permisos: solo READ, CREATE, UPDATE — sin DELETE en la aplicación |
| ADR-007 | Numeración de documentos: prefijos configurables + consecutivo |
| ADR-008 | Estados de embarque: máquina de estados con transiciones controladas |

### 8. Convenciones Adicionales

**Namespace por capa:**
```
Freiroute.Entity          → entidades de dominio
Freiroute.DTO.[Modulo]    → DTOs del módulo
Freiroute.DAL.Interfaces  → contratos DAL
Freiroute.DAL.Repositories → implementaciones Dapper
Freiroute.BLL.Interfaces  → contratos BLL
Freiroute.BLL.Services    → implementaciones de servicio
Freiroute.BLL.Validators  → FluentValidation validators
Freiroute.API.Controllers → REST controllers
Freiroute.Aplicacion.[Area].Controllers → MVC controllers
```

**Patrón de numeración de documentos TMS:**
```csharp
// Freiroute.Utility/DocumentNumberGenerator.cs
// Formato: {PREFIX}-{YYYY}-{NNNNN}
// Ejemplos: FR-2026-00847 (embarque), ORD-2026-01234 (orden), CAR-2026-00012 (carta de porte)
```

### 9. Checklist de Entregable por Módulo

- [ ] **Entity**: Campos base obligatorios + campos de negocio + constantes de estado si aplica
- [ ] **RequestDto**: Solo campos editables, sin IDs internos ni campos de auditoría
- [ ] **ResponseDto**: Campos seguros para cliente, labels de estado en español
- [ ] **DAL Interface**: GetAll, GetById, Create, Update, Deactivate (nunca Delete)
- [ ] **BLL Interface**: GetAll, GetById, Create, Update, Deactivate + métodos de negocio TMS
- [ ] **ADR**: Creado si la decisión impacta la arquitectura del sistema
- [ ] **Namespace**: Correcto según convención del proyecto
- [ ] **Entregado a @IngenieroDatos**: Con los nombres exactos de tabla y columnas SQL

---

## Contexto Freiroute TMS

@Arquitecto asegura que el TMS de transporte multi-tenant cumpla con la arquitectura establecida en AGENTS.md. Cada empresa (`empresa_id`) tiene datos completamente aislados mediante RLS. Los módulos críticos del dominio (Órdenes, Embarques, Carriers, Rutas, Track & Trace) deben modelarse con los estados correctos del negocio de transporte y nunca exponer datos entre tenants.
