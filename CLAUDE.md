# CLAUDE.md — Freiroute TMS · Configuración para Claude Code (Antigravity)

> **Archivo de contexto persistente para Claude Code**
> Este archivo se carga automáticamente en CADA sesión de Claude Code.
> No modifica ni reemplaza los archivos de opencode en `.opencode/`.

---

## Identidad del Producto

**Nombre:** Freiroute TMS
**Tipo:** SaaS Multi-Tenant — Transportation Management System
**Nivel:** Mundial (referencia: Oracle TMS, SAP TM, MercuryGate, BluJay, Trimble TMS)
**Tagline:** *"Manage every route. Move every load."*

---

## Stack Tecnológico

- **Presentación:** ASP.NET Core MVC (.NET 8) — proyecto `Freiroute.Aplicacion`
- **API REST:** ASP.NET Core Web API (.NET 8) — proyecto `Freiroute.API`
- **Base de datos:** Supabase (PostgreSQL 15) — migraciones en `supabase/migrations/`
- **ORM:** Dapper (micro-ORM, consultas SQL directas)
- **Auth:** Supabase Auth + JWT con RLS
- **Validación servidor:** FluentValidation
- **Validación cliente:** jQuery Validate + Unobtrusive
- **UI Kit:** Bootstrap 5.3 + Design System Freiroute
- **Testing:** xUnit + Moq + FluentAssertions
- **Logging:** Serilog (JSON estructurado)
- **CI/CD:** GitHub Actions

---

## Arquitectura N-Tier — 8 Proyectos

```
Flujo obligatorio:
Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL
```

| Proyecto | Responsabilidad |
|---|---|
| `Freiroute.Entity` | Entidades de dominio |
| `Freiroute.DTO` | DTOs Request + Response |
| `Freiroute.DAL` | Interfaces + Repositorios Dapper |
| `Freiroute.BLL` | Interfaces + Services + Validators |
| `Freiroute.IOC` | Inyección de dependencias |
| `Freiroute.Utility` | Helpers, extensiones, constantes |
| `Freiroute.API` | Web API (JWT, Controllers, Middleware) |
| `Freiroute.Aplicacion` | MVC (Areas, Controllers, Views) |

**REGLA CRÍTICA:** Ninguna capa puede saltarse otra. El Controller MVC **nunca** llama al DAL.

---

## Reglas de Código C#

- Clases y métodos: **PascalCase** | Variables: **camelCase** | SQL: **snake_case**
- Métodos async SIEMPRE terminan en `Async`: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`
- Todas las respuestas API usan el wrapper `ApiResponse<T>` — **nunca** retornar tipos puros
- DTOs SIEMPRE son diferentes a Entities — **nunca** exponer Entity directamente
- Interfaces SIEMPRE empiezan con `I`: `IEmbarqueService`, `ICarrierRepository`
- **No existe `DeleteAsync`** — solo `DeactivateAsync` (soft delete: `activo = false`)

---

## Reglas de Base de Datos

- IDs: `UUID` con `gen_random_uuid()` — **nunca** generados en C#
- Toda tabla de negocio incluye: `id`, `empresa_id`, `activo`, `fecha_creacion`, `fecha_modificacion`
- **Soft delete obligatorio:** `activo = false` — **nunca** DELETE físico
- **Índices obligatorios** por tabla: `idx_[tabla]_empresa_id` e `idx_[tabla]_activo`
- Toda tabla tiene trigger `update_fecha_modificacion()` en UPDATE
- RLS habilitado en CADA tabla de negocio
- Migraciones SOLO con Supabase CLI: `supabase migration new [nombre]`

---

## Multi-Tenant

- **Toda** consulta SQL filtra por `empresa_id` (aunque RLS ya lo aplique)
- JWT contiene: `user_id`, `empresa_id`, `perfil_id`, `permisos[]`, `tipo_usuario`
- Tipos de usuario: `SUPER_ADMIN | ADMIN | OPERADOR | CONDUCTOR | CLIENTE`
- Permisos granulares: **solo** `READ`, `CREATE`, `UPDATE` — NO existe DELETE

---

## Design System Freiroute — Colores

```css
--fr-navy-primary:    #0B2545;   /* Sidebar, navbar */
--fr-navy-mid:        #1B4F8A;   /* Hover sidebar */
--fr-action-blue:     #1A73E8;   /* Botones CTA */
--fr-cyan-accent:     #00D4FF;   /* Logo, item activo sidebar */
--fr-success:         #2E7D32;   /* Entregado */
--fr-warning:         #F57F17;   /* En tránsito */
--fr-danger:          #E53935;   /* Crítico */
--fr-surface-bg:      #F8FAFC;   /* Fondo página */
--fr-text-primary:    #1E293B;   /* Texto principal */
```

**Tipografía:** Inter (Variable) — UI principal | DM Sans — Marketing | JetBrains Mono — Código/IDs

---

## Convención de Idiomas

| Elemento | Idioma |
|---|---|
| Interfaz de usuario (labels, mensajes) | **Español** |
| Tablas y columnas SQL | **Español** (snake_case) |
| Clases, métodos C# | **Inglés** |
| Comentarios C# | **Inglés** |
| Documentación técnica | **Español** |
| Mensajes de validación | **Español** |
| Logs Serilog | **Inglés** |

---

## Flujo de Trabajo Obligatorio

**SIEMPRE:** spec → plan → tasks → implement → test  
**NUNCA** saltar directo a código.

Antes de implementar cualquier módulo:
1. ✅ `docs/specs/[modulo]-spec.md` debe existir
2. ✅ Branch creada desde `develop`: `feature/HU-XXX-nombre`
3. ✅ `supabase start` ejecutado y BD local disponible
4. ✅ `dotnet build` sin warnings

---

## Agentes disponibles en Claude Code

Invoca estos subagentes con `@nombre` o deja que Claude los seleccione automáticamente:

- **@pm** — Orquestador: sprint planning, coordinación, PRs
- **@arquitecto** — Entity, DTOs, interfaces, ADRs
- **@ingenierodatos** — Migraciones SQL, RLS, repositorios Dapper
- **@backenddev** — BLL Services, API Controllers, Tests
- **@frontenddev** — Vistas Razor, Design System, validación cliente
- **@qa** — Unit tests, integration tests, cobertura

**Skills disponibles** (invoca con `/nombre`):
- `/skill-pm` — Guía de sprint planning y coordinación
- `/skill-arquitecto` — Convenciones de Entity/DTO/Interfaces
- `/skill-dal` — Patrón repositorio Dapper + RLS
- `/skill-bll` — Patrón de Services + FluentValidation
- `/skill-view` — Design System Freiroute + Razor Views
- `/skill-testing` — TDD con xUnit, Moq, FluentAssertions

---

## Glosario Clave

| Término | Descripción |
|---|---|
| `tenant` / `empresa` | Organización que usa el SaaS |
| `empresa_id` | Discriminador universal de tenant |
| `activo` | Flag booleano de soft delete |
| `embarque` | Operación de transporte individual |
| `carrier` | Transportista (propio o tercero) |
| `conductor` | Operador de vehículo registrado |
| `OTD` | On-Time Delivery — % entregas a tiempo |
| `POD` | Proof of Delivery — prueba de entrega digital |
| `FTL` | Full Truck Load | `LTL` | Less Than Truck Load |

---

*CLAUDE.md — Freiroute TMS | Para Claude Code (Antigravity)*
*Espejo funcional de AGENTS.md — no modifica archivos de opencode*
