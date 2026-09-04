# ADR-009: Modelo de Permisos con Flags Booleanos por Módulo

## Estado
Aceptado

## Fecha
2026 — Sprint 1

## Contexto
Durante la Fase 1 del Sprint 1, @Arquitecto identificó una inconsistencia entre el ADR-006 original (que describía permisos con una columna `tipo` de tipo string: READ | CREATE | UPDATE) y el spec del Sprint 1 que definía la tabla `permisos` con tres columnas booleanas independientes: `puede_leer`, `puede_crear`, `puede_actualizar`.

Ambos modelos representan los mismos 3 niveles de permiso, pero con estructuras de BD y de código muy diferentes. Fue necesario decidir cuál adoptar como estándar definitivo.

## Decisión
El sistema usará **flags booleanos independientes** por módulo en la tabla `permisos`:

```sql
puede_leer        BOOLEAN NOT NULL DEFAULT false
puede_crear       BOOLEAN NOT NULL DEFAULT false
puede_actualizar  BOOLEAN NOT NULL DEFAULT false
```

En lugar de una columna `tipo VARCHAR(50)` con valores READ | CREATE | UPDATE.

## Comparación de Modelos

### Modelo A — Columna tipo (ADR-006 original, descartado)
```sql
-- Una fila por permiso por módulo
perfil_id | modulo    | tipo
uuid      | embarques | READ
uuid      | embarques | CREATE
uuid      | embarques | UPDATE
```

```csharp
// Requería 3 filas para un módulo completo
// Query compleja para verificar si tiene un permiso específico
```

### Modelo B — Flags booleanos (ADR-009, adoptado)
```sql
-- Una sola fila por módulo con los 3 niveles
perfil_id | modulo    | puede_leer | puede_crear | puede_actualizar
uuid      | embarques | true       | true        | false
```

```csharp
// Una sola fila por módulo
// Constraint UNIQUE (perfil_id, modulo) garantiza integridad
// Operación de reemplazo total atómica con ReemplazarPermisosAsync
```

## Alternativas Consideradas

1. **Columna tipo string (ADR-006)** — Descartada porque requiere múltiples filas por módulo, hace más compleja la verificación de permisos en el JWT y dificulta la UI de gestión de permisos (checkboxes independientes por nivel).

2. **Flags booleanos (ADR-009, elegido)** — Adoptado porque:
   - Una sola fila por módulo → constraint UNIQUE simple
   - UI de permisos natural: 3 checkboxes por módulo
   - Verificación de permiso directa: `puede_leer = true`
   - `ReemplazarPermisosAsync` puede hacer DELETE + INSERT en una transacción atómica
   - El claim JWT se serializa fácilmente: `"embarques:read"`, `"embarques:create"`

## Impacto en el Código

### IPermisoRepository — método adicional requerido
```csharp
// Reemplaza todos los permisos de un perfil en una transacción atómica
Task ReemplazarPermisosAsync(Guid perfilId, Guid empresaId,
    IEnumerable<Permiso> permisos);
```

### Serialización al JWT
```csharp
// Al generar el JWT, los permisos se serializan como:
// ["embarques:read", "embarques:create", "ordenes:read"]
// desde: WHERE perfil_id = @PerfilId AND puede_leer = true → "modulo:read"
//        WHERE perfil_id = @PerfilId AND puede_crear = true → "modulo:create"
//        WHERE perfil_id = @PerfilId AND puede_actualizar = true → "modulo:update"
```

### RequirePermissionAttribute
```csharp
// El atributo sigue funcionando igual:
[RequirePermission("embarques", PermissionType.Read)]
// Verifica claim: "embarques:read" en el JWT
```

## Migración

La migración `20260101000004_tabla_permisos.sql` debe usar la estructura de flags booleanos definida en el spec del Sprint 1. La migración vieja `20260831031417_initial_schema.sql` que usa el modelo de columna `tipo` debe ser reemplazada por @IngenieroDatos antes de `supabase db push`.

## Consecuencias

**Positivas:**
- Estructura de BD más limpia y con mejor integridad referencial
- UI de gestión de permisos trivial (tabla con checkboxes)
- Serialización/deserialización de claims JWT más simple
- `ReemplazarPermisosAsync` garantiza consistencia transaccional

**Negativas / Trade-offs:**
- Si en el futuro se agregan más tipos de permiso (ej: APPROVE, EXPORT), requiere ALTER TABLE para agregar columnas booleanas adicionales
- El ADR-006 queda reemplazado por este ADR-009 en lo relativo al modelo de datos de permisos

## Módulos Afectados
- `Freiroute.Entity/Permiso.cs`
- `Freiroute.DTO/Permiso/PermisoRequestDto.cs`
- `Freiroute.DAL/Interfaces/IPermisoRepository.cs`
- `supabase/migrations/[fecha]_tabla_permisos.sql`
- `Freiroute.BLL/Services/AuthService.cs` (serialización JWT)
- `Freiroute.API/Attributes/RequirePermissionAttribute.cs`
