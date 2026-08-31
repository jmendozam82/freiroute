# ADR-007: Permisos Granulares (READ, CREATE, UPDATE) sin permiso de DELETE

| Campo | Valor |
|---|---|
| **ID** | ADR-007 |
| **Título** | Modelo de permisos basado únicamente en `READ`, `CREATE`, `UPDATE` (excluyendo `DELETE`) |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-22 |
| **Decidido por** | Product Owner + Arquitecto de software |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El sistema requiere un control de acceso basado en roles (RBAC) donde los administradores puedan asignar permisos específicos a los perfiles de usuario. Tradicionalmente, los sistemas CRUD implementan cuatro permisos básicos: Create, Read, Update, Delete (CRUD).

Sin embargo, como se estableció en el **ADR-005 (Soft Delete Universal)**, los registros en este sistema no se eliminan físicamente. La acción que el usuario percibe como "Eliminar" es en realidad una actualización del estado del registro (`activo = false`).

La decisión arquitectónica es: ¿Debemos mantener un permiso conceptual de `DELETE` en la UI y en la base de datos de permisos, aunque técnicamente sea un `UPDATE`, o debemos alinear el modelo de permisos con la realidad técnica?

---

## Decisión

**El sistema gestionará exactamente tres tipos de permisos por módulo: `READ`, `CREATE`, y `UPDATE`. No existirá el permiso `DELETE`.**

La acción de desactivar un registro (soft delete) requerirá el permiso `UPDATE` sobre ese módulo.

---

## Alternativas Evaluadas

### Opción A: Modelo tradicional CRUD (READ, CREATE, UPDATE, DELETE) (RECHAZADA)

**Ventajas:**
- Familiar para los administradores que configuran el sistema
- Permite separar el permiso de "editar datos" del permiso de "desactivar el registro"

**Desventajas que motivaron su rechazo:**
- Crea una disonancia cognitiva: el sistema pide permiso de `DELETE` para ejecutar un método `DeactivateAsync` que hace un `UPDATE`.
- Añade complejidad innecesaria a la tabla de permisos (25% más de filas).
- En la práctica del dominio médico/SaaS evaluado, los usuarios que tienen autoridad para modificar un registro (ej. cambiar el estado de un paciente, modificar un diagnóstico) generalmente también tienen la autoridad para desactivarlo. La granularidad extra rara vez se utiliza.

### Opción B: Modelo simplificado RCU (READ, CREATE, UPDATE) (ELEGIDA) ✅

**Ventajas:**
- Alineación perfecta con el modelo de datos (ADR-005: no hay DELETEs físicos).
- Simplifica la UI de asignación de roles (menos checkboxes para el administrador).
- Simplifica la tabla de permisos en la base de datos.
- Código de autorización más limpio: el endpoint de desactivación simplemente requiere `PermissionType.Update`.

**Desventajas aceptadas:**
- No es posible dar permiso a un usuario para editar un registro, pero prohibirle desactivarlo. En este sistema, la capacidad de modificar implica la capacidad de cambiar su estado a inactivo.

---

## Consecuencias

### Positivas
- Interfaz de administración de roles más limpia y rápida de configurar.
- Menor tamaño de la tabla `permisos`.
- Coherencia técnica absoluta con el ADR-005.

### Negativas / Trade-offs aceptados
- Falta de granularidad para separar la edición de la desactivación. Si un cliente SaaS futuro requiere esta separación estricta, el modelo RCU será insuficiente.

### Implementación en Código

```csharp
// Enumerador de permisos
public enum PermissionType
{
    Read = 1,
    Create = 2,
    Update = 3
    // Sin Delete
}

// En el Controller
[HttpPut("{id}")]
[RequirePermission("pacientes", PermissionType.Update)]
public async Task<IActionResult> Update(Guid id, [FromBody] PacienteRequestDto dto) { ... }

[HttpDelete("{id}/deactivate")] // El verbo HTTP puede ser DELETE para semántica REST
[RequirePermission("pacientes", PermissionType.Update)] // Pero el permiso requerido es UPDATE
public async Task<IActionResult> Deactivate(Guid id) { ... }
```

---

## Referencias

- ADR-005 — Soft Delete Universal (la base de esta decisión)
