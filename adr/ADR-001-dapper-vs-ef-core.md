# ADR-001: Dapper como ORM en lugar de Entity Framework Core

| Campo | Valor |
|---|---|
| **ID** | ADR-001 |
| **Título** | Dapper como ORM en lugar de Entity Framework Core |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-15 |
| **Decidido por** | Arquitecto de software + Tech Lead |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El equipo necesitaba elegir la estrategia de acceso a datos para un sistema SaaS médico multi-tenant sobre PostgreSQL vía Supabase. El criterio principal era: **control total sobre las consultas SQL**, especialmente para:

- Políticas de RLS (Row Level Security) que requieren `current_setting()` de PostgreSQL
- Consultas con múltiples JOINs complejos (expedientes, citas, diagnósticos)
- Filtros por `clinica_id` en TODAS las consultas sin posibilidad de omitirlos por error

Las dos opciones principales evaluadas fueron:

1. **Entity Framework Core** — ORM maduro de Microsoft, Code-First o Database-First
2. **Dapper** — Micro-ORM que mapea resultados SQL a objetos C#

---

## Decisión

**Usaremos Dapper** como la única capa de acceso a datos en este proyecto y en todos los proyectos derivados del framework.

---

## Alternativas Evaluadas

### Opción A: Entity Framework Core (RECHAZADA)

**Ventajas:**
- Menos código boilerplate — LINQ genera SQL automáticamente
- Migraciones automáticas con `dotnet ef migrations add`
- Soporte nativo para relaciones, lazy loading, change tracking

**Desventajas que motivaron su rechazo:**
- Las políticas RLS de PostgreSQL (`current_setting`) no son manejables limpiamente desde el query pipeline de EF Core
- El filtro global de `clinica_id` puede omitirse accidentalmente con `IgnoreQueryFilters()`
- Las consultas generadas por EF Core en JOINs complejos son subóptimas y difíciles de depurar
- El `change tracker` introduce overhead en escenarios de solo lectura (dashboards, reportes)
- La curva de aprendizaje para RLS + PostgreSQL-specific features es mayor

### Opción B: Dapper (ELEGIDA) ✅

**Ventajas:**
- SQL explícito: cada consulta es auditable, optimizable y predecible
- Compatibilidad nativa con funciones PostgreSQL (`gen_random_uuid()`, `current_setting()`, `NOW()`)
- El filtro por `clinica_id` es parte del SQL — imposible olvidarlo
- Rendimiento superior en consultas de lectura (sin change tracking)
- Fácil de testear con `Mock<IDbConnection>`
- Curva de aprendizaje mínima: si sabes SQL, sabes Dapper

**Desventajas aceptadas:**
- Más código manual para INSERTs y UPDATEs
- Sin migraciones automáticas (se resuelve con Supabase CLI)
- Sin lazy loading (se resuelve con queries explícitas y DTOs)

### Opción C: Repositorio raw con NpgsqlCommand (RECHAZADA)

Demasiado verboso, sin ventaja sobre Dapper.

---

## Consecuencias

### Positivas
- Todas las consultas son SQL estándar PostgreSQL — cualquier DBA puede auditarlas
- El aislamiento multi-tenant es visible en el código (no oculto en filtros globales)
- El rendimiento de lectura es máximo (sin overhead de EF Core)
- Las migraciones de BD son scripts SQL versionados (mejor para revisiones en PR)

### Negativas / Trade-offs aceptados
- Cada operación CRUD requiere SQL manual en el Repository
- Sin generación automática de schema desde el modelo C# — se usa Supabase CLI
- El desarrollador debe conocer SQL para contribuir al DAL

### Impacto en el proyecto
- `[Proyecto].DAL` contiene SQL explícito en cada Repository
- Las migraciones se gestionan 100% con Supabase CLI (`supabase migration new`)
- Los tests del DAL mockan `IDbConnection` directamente

---

## Referencias

- [Dapper GitHub](https://github.com/DapperLib/Dapper)
- [Dapper vs EF Core — Benchmark 2024](https://github.com/DapperLib/Dapper)
- ADR-003 — Supabase como BaaS (relacionado: migraciones)
- ADR-004 — RLS para multi-tenant (relacionado: SQL explícito requerido)
