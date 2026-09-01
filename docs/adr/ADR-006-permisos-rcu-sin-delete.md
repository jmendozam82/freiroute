# ADR-006: Permisos granulares READ / CREATE / UPDATE — sin DELETE

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
El sistema Freiroute TMS maneja datos críticos de operación logística donde el borrado físico de registros (cartas de porte, PODs digitales, facturas, historiales de embarques) puede comprometer cumplimiento legal SOX/GDPR y generar inconsistencias en cadenas de referencia FK entre tablas relacionadas. Se requiere un modelo de permisos que permita operaciones CRUD estándar SIN permitir eliminación explícita bajo ningún contexto dentro de la aplicación.

## Decisión
El sistema MANEJARÁ exactamente **tres tipos de permiso**: `READ`, `CREATE`, `UPDATE`. No existe `DELETE` en ninguna capa de la aplicación.

### Tabla de asignación de permisos por rol
| Rol | Empresas | Perfiles | Usuarios | Órdenes | Embarques | Carriers | Maestros |
|---|---|---|---|---|---|---|---|
| SUPER_ADMIN | R/C/U | R/C/U | R/C/U | R/C/U | R/C/U | R/C/U | R/C/U |
| ADMIN | C/U | R | R/C/U | R/C/U | R/C/U | R/C/U | R/C/U |
| DISPATCHER | — | — | — | R/C/U | R/C/U | R | — |
| OPERADOR | — | — | — | R/C | R/C/U | — | R |
| CONDUCTOR | — | — | — | — | R | — | — |
| CLIENTE | — | — | — | R | R | — | — |

### Implementación técnica
```csharp
// Modelo de permisos simplificado (3 valores únicos)
public enum PermissionType { READ, CREATE, UPDATE }

// Attribute en controllers
[RequirePermission("modulo", PermissionType.Read)]
[RequirePermission("modulo", PermissionType.Create)]
[RequirePermission("modulo", PermissionType.Update)]  // Incluye Deactivate

// En BD
CREATE TABLE permisos (
    perfil_id UUID NOT NULL REFERENCES perfiles(id),
    modulo VARCHAR(100) NOT NULL,
    tipo VARCHAR(20) NOT NULL CHECK (tipo IN ('READ', 'CREATE', 'UPDATE')),
    activo BOOLEAN NOT NULL DEFAULT true
);
```

Justificación principal: Eliminar DELETE como concepto operativo fuerza al sistema a usar soft delete (`activo = false`) implementado bajo `PermissionType.UPDATE`, lo que evita errores humanos de borrado accidenta y mantiene consistencia referencial absoluta.

## Alternativas Consideradas
1. **Eliminar DELETE completamente del modelo** — Demasiado restrictivo para administradores que necesitan limpiar datos basura o registros duplicados creados durante pruebas. Los admins SÍ pueden ejecutar `DELETE` directo en Postgres via psql cuando sea estrictamente necesario (fuera del scope de la app).
2. **DELETE solo para Super Admin** — Riesgo alto de eliminación accidental de datos operativos reales. Un error de `DROP TABLE` o `DELETE FROM` ejecutado desde SQL Management Studio podría destruir semanas de información de producción. Mejor prevenir.
3. **Soft delete + DELETE lógico con confirmación doble** — Mismo riesgo inherente al soft delete si alguien escribe `WHERE activo = false; UPDATE SET activo = true` incorrectamente. Es más seguro eliminar DELETE físicamente del stack.

## Consecuencias
**Positivas:**
- Eliminación completa de accidentes de borrado accidental de datos críticos
- Soft delete consistente aplicable universalmente a todos los módulos
- Flujo claro: cualquier acción destructiva pasa por `PermissionType.UPDATE`
- Cumplimiento GDPR derecho al olvido se cumple mediante masking/anonymización en lugar de borrado

**Negativas / Trade-offs:**
- Algunos escenarios administrativos requieren intervención manual vía psql (aceptable como fallback)
- Migraciones legacy deben migrar de DELETE a DEACTIVATE (costo único inicial)
- Testing debe verificar explícitamente que NO existen métodos DeleteAsync en interfaces DAL/BLL
- Reportes históricos requieren filtrar `WHERE activo = true` incluso cuando no hay DELETE en el código

## Módulos Afectados
Todos los módulos CRUD del MVP. Este ADR reemplaza el patrón DELETE estándar en todas las APIs REST, BLL Services y Vistas Razor. Ningún endpoint `DELETE /api/[modulo]/{id}` existirá jamás. Solo `POST /api/[modulo]/{id}/deactivate`.

---
