# Framework de Ingeniería de Software SaaS con Supabase
## Índice General — v1.0.0

> **Documento de navegación del framework**
> Este framework es la destilación de las mejores prácticas del proyecto Vittal (2026).
> Úsalo como punto de inicio para cualquier nuevo proyecto SaaS en el mismo stack.

---

## ¿Qué es este Framework?

Este framework documenta la metodología, arquitectura y convenciones para construir software SaaS de alta calidad usando:

- **ASP.NET Core 8** (MVC + Web API)
- **Supabase** (PostgreSQL, Auth, Realtime, Storage)
- **SCRUM** con agentes de IA integrados al proceso
- **Los 4 pilares convergentes de 2026**

---

## Mapa de Documentos

```
docs/framework/
├── INDEX.md              ← Este archivo (navegación del framework)
├── AGENTS.md             ← La Constitución (COPIAR A LA RAÍZ del nuevo proyecto)
├── README-template.md    ← Plantilla README (COPIAR Y ADAPTAR)
│
├── lifecycle.md          ← Ciclo de vida + Los 4 pilares de 2026
├── arquitectura.md       ← Arquitectura N-Tier + diagramas
├── requerimientos.md     ← RF, RNF y Reglas de Negocio (plantilla)
├── backlog.md            ← Product Backlog por Sprints (plantilla)
├── testing.md            ← TDD, Unit Tests, Integration Tests
├── convenciones.md       ← Nomenclatura, plantillas de código, checklist
└── quickstart.md         ← Bootstrap de proyecto desde cero
```

---

## Ruta de Lectura Recomendada

### Para iniciar un NUEVO PROYECTO (Fase 0):

```
1. lifecycle.md        → Entender la metodología y los 4 pilares
2. AGENTS.md           → La Constitución — copiar a la raíz del nuevo repo
3. quickstart.md       → Secuencia de comandos para inicializar
4. arquitectura.md     → Entender la arquitectura antes de codificar
5. convenciones.md     → Plantillas de código para cada capa
```

### Para trabajar en un MÓDULO NUEVO (Fase 1):

```
1. backlog.md          → Revisar la HU del sprint actual
2. requerimientos.md   → Identificar RF, RNF y reglas de negocio
3. convenciones.md     → Usar las plantillas de código por capa
4. testing.md          → Escribir los tests primero (TDD)
```

### Para DEBUGGING o REVISIÓN de código:

```
1. AGENTS.md           → Verificar que el código respeta las reglas
2. arquitectura.md     → Verificar el flujo de capas
3. testing.md          → Verificar cobertura de tests
4. convenciones.md     → Verificar nomenclatura y checklist
```

---

## Los 4 Pilares Convergentes de 2026

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│  1. SDD — Spec-Driven Development                          │
│     spec → plan → tasks → implement                        │
│     Cada módulo tiene un spec.md ANTES del código          │
│                                                            │
│  2. TDD — Test-Driven Development                          │
│     El test que falla ES la especificación ejecutable      │
│     xUnit + Moq + FluentAssertions                         │
│                                                            │
│  3. SCRUM con Agentes de IA                                │
│     Agentes de IA como miembros del Sprint Board           │
│     Human reviews obligatorios antes del merge             │
│                                                            │
│  4. IaC + BaaS                                             │
│     Supabase CLI para migraciones versionadas en Git       │
│     GitHub Actions para CI/CD automático                   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## Resumen del Stack

| Componente | Tecnología | Propósito |
|---|---|---|
| **Frontend** | ASP.NET Core MVC + Razor | Interfaz de usuario |
| **Backend** | ASP.NET Core Web API | REST API + JWT |
| **Base de datos** | PostgreSQL 15 via Supabase | Almacenamiento de datos |
| **ORM** | Dapper | Consultas SQL directas |
| **Auth** | Supabase Auth | Autenticación + JWT |
| **Multi-tenant** | Row Level Security | Aislamiento de datos |
| **Tiempo real** | Supabase Realtime + SignalR | Notificaciones push |
| **Storage** | Supabase Storage | Archivos y documentos |
| **Validación server** | FluentValidation | Reglas de negocio |
| **Validación client** | jQuery Validate | UX de formularios |
| **UI Kit** | Bootstrap 5.3 | Diseño responsivo |
| **CI/CD** | GitHub Actions | Integración continua |
| **IA** | Claude Code + AGENTS.md | Desarrollo asistido |

---

## Arquitectura de Capas (Resumen)

```
Aplicacion (MVC)    ← Vistas Razor, Controllers MVC
     ↓
API (Web API)       ← REST Endpoints, JWT, Swagger
     ↓
BLL                 ← Servicios, Reglas de Negocio, FluentValidation
     ↓
DAL                 ← Repositorios, Dapper, SQL
     ↓
Supabase            ← PostgreSQL + RLS + Auth + Storage + Realtime
```

**Regla de oro:** Ninguna capa puede saltarse otra. Sin excepciones.

---

## Reglas de Negocio Universales (Resumen)

| # | Regla | Detalle |
|---|---|---|
| 1 | Soft Delete | Nunca `DELETE` — siempre `activo = false` |
| 2 | IDs Auto | UUID generado por PostgreSQL (`gen_random_uuid()`) |
| 3 | Multi-Tenant | Todo filtra por `tenant_id` + RLS |
| 4 | Permisos | Solo `READ`, `CREATE`, `UPDATE` |
| 5 | DTOs | Nunca exponer Entity directamente |
| 6 | Auditoría | `fecha_creacion` y `fecha_modificacion` en toda tabla |
| 7 | Validación doble | Server (FluentValidation) + Client (jQuery Validate) |
| 8 | Migraciones | Solo via Supabase CLI — nunca SQL ad-hoc en producción |
| 9 | Archivos | Supabase Storage privado con URLs temporales |
| 10 | Tests | BLL ≥ 80% cobertura, API ≥ 60% cobertura |

---

## Sprint 0 — Checklist de Inicio (Resumen)

```bash
# 1. AGENTS.md commiteado PRIMERO
git add AGENTS.md && git commit -m "feat: La Constitución"

# 2. Crear solución con 8 proyectos
dotnet new sln + 8x dotnet new [classlib|mvc|webapi]

# 3. Inicializar Supabase
supabase init && supabase start

# 4. Primera migración: tenants, perfiles, usuarios, permisos
supabase migration new initial_schema && supabase db push

# 5. Variables de entorno (.env.local — NO en git)
# 6. GitHub Actions CI configurado
# 7. dotnet build ✅ | dotnet test ✅
```

---

## Origen y Mantenimiento

Este framework fue destilado del proyecto **Vittal** (2026), un sistema médico SaaS multi-tenant que demostró en producción las mejores prácticas documentadas aquí.

**Principios de actualización:**
- Actualizar cuando se aprenda algo nuevo en producción
- Versionear cada cambio significativo
- Cualquier decisión arquitectónica que rompa con este framework debe documentarse y justificarse en el AGENTS.md del proyecto específico

---

*INDEX.md — Framework SaaS con Supabase v1.0.0*
*Origen: Proyecto Vittal (2026) | Actualizado: 2026-08-30*
*Aplica a: Cualquier proyecto SaaS con ASP.NET Core 8 + Supabase + SCRUM + Agentes de IA*
