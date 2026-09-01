# ADR-005: Soft delete universal (activo = false)

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
El sistema Freiroute TMS maneja datos críticos de operación logística donde la eliminación física de registros compromete auditabilidad, historial de embarques, trazabilidad financiera y cumplimiento normativo. Un registro eliminado físicamente imposibilita reconstruir eventos pasados durante disputas contractuales, auditorías de seguridad o análisis forenses operativos. Se necesita un mecanismo que permita "desactivar" registros sin perder su rastro histórico completo.

## Decisión
Todo registro del sistema se manejará mediante **soft delete universal**:
- Toda tabla de negocio INCLUIRÁ la columna `BOOLEAN activo NOT NULL DEFAULT true`
- **NUNCA se ejecutará `DELETE FROM [tabla]` en producción** bajo ninguna circunstancia dentro de la aplicación
- Para "eliminar" un registro se establece `SET activo = false WHERE id = @Id AND empresa_id = @EmpresaId`
- Todas las queries SELECT DEBERÁN incluir `WHERE activo = true` por defecto
- Se implementará un trigger automático `update_fecha_modificacion()` en UPDATE para trazar cambios
- El endpoint de desactivación se llamará `POST /api/[modulo]/{id}/deactivate` (nunca `DELETE`)

La interfaz de usuario mostrará opciones condicionales:
```html
<!-- Solo visible si el usuario tiene permiso UPDATE -->
@if (User.HasPermission("modulo", "UPDATE")) {
    <button type="submit" class="btn btn-danger">Desactivar</button>
}
```

Justificación principal: Cumplimiento con estándares internacionales SOX, GDPR retention policies, y requerimientos legales de transporte que exigen conservar registros históricos mínimo 5 años (congelación documental).

## Alternativas Consideradas
1. **Eliminación física estándar** — Descartada porque viola compliance regulatorio (SOX, GDPR), destruye historial de embarques irreemplazable y genera inconsistencias en FK references entre tablas relacionadas.
2. **Soft delete con flag `deleted_at` timestamp** — Menos preferible que booleano `activo` porque complica queries (necesita `WHERE deleted_at IS NULL` vs `activo = true`). Booleano es más explícito semánticamente en español ("activo" vs "no activo") y más performante en índices B-tree.
3. **Tabla separada de históricos** (`[modulo]_deleted`) — Demasiado complejidad administrativa. No aporta beneficios comparado con soft delete inline; solo multiplica operaciones de INSERT/SELECT.

## Consecuencias
**Positivas:**
- Conservación completa del historial operativo y financiero
- Cumplimiento SOX/GDPR sin procesos manuales de exportación
- Auditoría simple: `SELECT * FROM embarques WHERE activo = false ORDER BY fecha_modificacion DESC`
- Sin foreign key violations cuando se "elimina" un master referenced by otros records
- Lógica visual intuitiva: activo=true → aparece en UI, activo=false → oculta

**Negativas / Trade-offs:**
- Cada query debe filtrar manualmente `AND activo = true` (mitigable con RLS policies)
- Espacio de almacenamiento creciente gradualmente (monitorizar volumen de datos inactivos)
- Índices adicionales recomendados: `idx_[tabla]_empresa_activo` compuesto para performance óptima
- Migraciones legacy deben adicionar columna `activo` con valor default `true`

## Módulos Afectados
Todos los módulos CRUD del MVP. Este ADR modifica el patrón de escritura estándar: en lugar de `DeleteAsync` existe únicamente `DeactivateAsync`. La BLL nunca delegará eliminación física al DAL.

---
