# ADR-005: Soft Delete Universal (activo = false) en lugar de Delete Físico

| Campo | Valor |
|---|---|
| **ID** | ADR-005 |
| **Título** | Soft Delete universal con campo `activo` en lugar de eliminación física de registros |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-20 |
| **Decidido por** | Tech Lead + Product Owner |
| **Revisado en** | Vittal Sprint 0 — Regla de negocio global |

---

## Contexto

El sistema gestiona datos médicos (pacientes, expedientes, citas, diagnósticos) y datos de negocio (usuarios, clínicas, catálogos). La pregunta es: **¿qué ocurre cuando un usuario elimina un registro?**

En un contexto médico, la eliminación de datos tiene implicaciones legales:
- Los expedientes médicos deben ser conservados por ley (en muchos países, mínimo 5-10 años)
- Un diagnóstico o tratamiento no puede "desaparecer" del historial
- Las auditorías regulatorias requieren trazabilidad completa

En un contexto de negocio SaaS:
- Un usuario puede ser desactivado sin perder su historial de actividad
- Un medicamento descontinuado no debería borrar las recetas que lo incluyeron

---

## Decisión

**Ningún registro se eliminará físicamente de la base de datos.** Todo registro tiene un campo `activo BOOLEAN NOT NULL DEFAULT true`. La "eliminación" de un registro consiste en:

```sql
UPDATE [tabla] SET activo = false, fecha_modificacion = NOW()
WHERE id = @Id AND tenant_id = @TenantId;
```

**Consecuencia en el código:** No existe el método `DeleteAsync` en ningún Repository. Solo existe `DeactivateAsync`.

```csharp
// ❌ PROHIBIDO — nunca existirá
Task DeleteAsync(Guid id, Guid tenantId);

// ✅ CORRECTO — la única forma de "eliminar"
Task<bool> DeactivateAsync(Guid id, Guid tenantId);
```

---

## Alternativas Evaluadas

### Opción A: DELETE físico con backup periódico (RECHAZADA)

**Ventajas:**
- La base de datos no crece indefinidamente
- Los listados son más simples (no necesitan filtrar por `activo`)
- El modelo de datos es más limpio visualmente

**Desventajas que motivaron su rechazo:**
- En sistemas médicos, eliminar datos es ilegal en muchas jurisdicciones
- Un usuario borrado accidentalmente pierde todos sus datos permanentemente
- Es imposible auditar "quién borró qué y cuándo"
- Las referencias por foreign keys pueden romperse (ON DELETE CASCADE elimina en cascada)
- Un `DELETE` accidental en producción es irrecuperable sin restaurar un backup completo

### Opción B: Tabla de auditoría separada + DELETE físico (RECHAZADA)

```sql
-- Copiar el registro a una tabla de auditoría antes de borrarlo
INSERT INTO pacientes_eliminados SELECT * FROM pacientes WHERE id = @Id;
DELETE FROM pacientes WHERE id = @Id;
```

**Ventajas:**
- La tabla principal queda "limpia"
- Se mantiene el historial en tablas separadas

**Desventajas que motivaron su rechazo:**
- Requiere tablas de "papelera" por cada entidad — duplicación del schema
- Las consultas de recuperación son complejas
- Si hay JOINs entre tablas (expediente → cita → diagnóstico), la recuperación parcial es inconsistente
- El costo de mantenimiento duplica el número de tablas del sistema

### Opción C: Soft Delete con campo `activo` (ELEGIDA) ✅

**Ventajas:**
- Historia completa siempre disponible para auditoría
- Recuperación trivial: `UPDATE activo = true`
- Las foreign keys nunca se rompen (el registro sigue existiendo)
- Seguro en sistemas regulados (médico, financiero, legal)
- No requiere tablas adicionales
- Compatible con RLS — los registros inactivos siguen siendo del tenant correcto

**Desventajas aceptadas:**
- Todos los listados deben filtrar por `activo = true` (sin excepción)
- La base de datos crece con el tiempo (incluye registros inactivos)
- Índices deben incluir `activo` para evitar table scans
- La lógica de "no se puede eliminar si tiene registros dependientes activos" debe implementarse en BLL

---

## Consecuencias

### Positivas
- Cumplimiento regulatorio en datos médicos y financieros
- Recuperación trivial de registros "eliminados" por error
- Historial completo de auditoría sin tablas adicionales
- Las relaciones de la BD nunca se rompen por eliminaciones
- El perfil `ADMIN` puede reactivar registros desactivados por error

### Negativas / Trade-offs aceptados
- Todo `SELECT` de listado incluye `AND activo = true` — si se omite, el bug devuelve registros inactivos
- Los índices de PostgreSQL deben ser parciales o incluir `activo`: `WHERE activo = true`
- La BD crece indefinidamente — pero en el contexto SaaS médico, esto es un requerimiento legal, no una desventaja
- La regla "no se puede desactivar si tiene dependientes activos" requiere lógica de validación en BLL

### Regla de implementación en DAL

```csharp
// Todos los GetAll filtran por activo = true
const string sql = @"
    SELECT * FROM pacientes
    WHERE tenant_id = @TenantId
    AND activo = true  -- OBLIGATORIO, nunca omitir
    ORDER BY primer_apellido";

// Todos los GetById también filtran por activo = true
const string sql = @"
    SELECT * FROM pacientes
    WHERE id = @Id AND tenant_id = @TenantId AND activo = true";
```

### Índice recomendado en PostgreSQL

```sql
-- Índice parcial — solo indexa registros activos (más eficiente)
CREATE INDEX idx_[tabla]_activo_tenant
ON [tabla](tenant_id, activo)
WHERE activo = true;
```

---

## Referencias

- [Soft Delete Pattern — Martin Fowler](https://www.martinfowler.com/eaaDev/SoftDelete.html)
- [PostgreSQL Partial Indexes](https://www.postgresql.org/docs/current/indexes-partial.html)
- ADR-002 — Arquitectura N-Tier (DAL no tiene DeleteAsync por esta decisión)
- ADR-004 — RLS (los registros inactivos siguen siendo del tenant correcto)
