# Ciclo de Vida de Ingeniería de Software — SaaS con Supabase bajo SCRUM

> **Documento de referencia metodológica**
> Base para el inicio de cualquier nuevo proyecto SaaS usando el stack: ASP.NET Core + Supabase + PostgreSQL.
> Independiente del dominio de negocio. Aplicable a cualquier vertical (médico, fintech, EdTech, logística, etc.).

---

## 1. Los Cuatro Pilares Convergentes en 2026

En 2026, la ingeniería de software de alto rendimiento converge en cuatro disciplinas que ya no son opcionales sino fundamentales para equipos que trabajan con agentes de IA:

```
┌─────────────────────────────────────────────────────┐
│         CUATRO PILARES CONVERGENTES 2026            │
│                                                     │
│   1. SDD    2. TDD     3. SCRUM+AI   4. IaC+BaaS   │
│  Spec-     Test-      Agentes en    Infra as        │
│  Driven    Driven     el Sprint     Code + BaaS     │
│  Dev       Dev        Board                         │
└─────────────────────────────────────────────────────┘
```

### Pilar 1 — SDD (Spec-Driven Development)

**Principio:** El código es la última etapa, no la primera.

```
Spec (WHAT) → Plan (HOW) → Tasks (WHO/WHEN) → Implement (CODE)
```

- Cada módulo tiene un `spec.md` que describe comportamiento esperado en lenguaje natural (EARS).
- El agente de IA actúa en "modo entrevista" para redactar el spec contigo.
- El spec se commitea ANTES de que el agente escriba código.
- Si no hay spec, no hay implementación. Sin excepciones.

### Pilar 2 — TDD (Test-Driven Development)

**Principio:** El test que falla ES la especificación ejecutable.

```
Escribe test que falla → Implementa lo mínimo → Pasa el test → Refactoriza
```

- El agente escribe el test primero, forzando que piense en los requerimientos antes que en la implementación.
- Los tests unitarios cubren la BLL (reglas de negocio).
- Los tests de integración cubren los endpoints de la API.
- **Meta de cobertura mínima:** 80% en BLL, 60% en API.

### Pilar 3 — SCRUM con Agentes de IA en el Sprint Board

**Principio:** Los agentes son miembros del equipo, no herramientas.

```
Product Owner (humano) → Sprint Planning → Agente asignado a HU →
Daily Review (humano valida) → Sprint Review → Retrospectiva
```

- Cada Historia de Usuario del backlog puede ser asignada a un agente especializado.
- El agente orquestador (PM) coordina a los agentes de capa (Arquitecto, IngenieroDatos, EspecialistaUI).
- La revisión del código generado es obligatoria por un humano antes del merge.
- **Velocidad estimada con agentes:** 3-5x vs. desarrollo manual.

### Pilar 4 — IaC + BaaS (Infrastructure as Code + Backend as a Service)

**Principio:** La infraestructura también se versiona y se reproduce.

```
Supabase CLI → Migraciones versionadas → GitHub Actions → Deploy automático
```

- Supabase como BaaS elimina la necesidad de gestionar servidores de BD, Auth y Storage.
- Las migraciones SQL son código — se revisan en PR, se testean en staging, se aplican en producción automáticamente.
- El entorno local de desarrollo es idéntico al de producción (Supabase local Docker).

---

## 2. Fases del Ciclo de Vida

### Fase 0 — Pilares del Proyecto (~2 horas, una sola vez)

> **La Constitución.** Se hace una vez y dura todo el proyecto.

```
┌──────────────────────────────────────────────────┐
│  FASE 0: PILARES (Hecho UNA VEZ al inicio)       │
│                                                  │
│  1. Crear AGENTS.md → commit → push              │
│  2. Definir stack tecnológico                    │
│  3. Crear estructura de carpetas del repo        │
│  4. Documentar decisiones base (ADRs)            │
│  5. Configurar Supabase (local + cloud)          │
│  6. Configurar GitHub Actions básico             │
│  7. Configurar IDE + extensiones                 │
└──────────────────────────────────────────────────┘
```

**Entregables de Fase 0:**
- [ ] `AGENTS.md` en la raíz del repo (commiteado)
- [ ] `README.md` con descripción del proyecto
- [ ] Solución .NET creada con los 8 proyectos
- [ ] Supabase project creado (local y cloud)
- [ ] Documentos ADR base creados en `docs/adr/` (Decisiones fundacionales)
- [ ] Primera migración: `initial_schema.sql`
- [ ] GitHub Actions básico configurado

### Fase 1 — Setup del Loop por Módulo (~30 min, antes de cada módulo)

> **El Loop de Desarrollo.** Se ejecuta antes de cada nuevo módulo o Historia de Usuario.

```
┌──────────────────────────────────────────────────┐
│  FASE 1: SETUP DEL LOOP (Antes de cada HU)       │
│                                                  │
│  1. Leer AGENTS.md                               │
│  2. Escribir spec.md del módulo                  │
│  3. ¿Hay impacto arquitectónico? → Crear ADR     │
│  4. Revisar spec (y ADRs) con el equipo          │
│  5. Crear plan.md (cómo implementar el spec)     │
│  6. Crear tasks.md (checklist de tareas)         │
└──────────────────────────────────────────────────┘
```

**Entregables de Fase 1:**
- [ ] `docs/specs/[modulo]-spec.md`
- [ ] Nuevos ADRs en `docs/adr/` (solo si la HU altera/crea arquitectura)
- [ ] Aprobación del Product Owner
- [ ] Backlog actualizado en el Sprint Board

### Fase 2 — El Agente Trabaja (La implementación)

> **El agente implementa siguiendo el spec.** El humano revisa.

```
┌──────────────────────────────────────────────────┐
│  FASE 2: IMPLEMENTACIÓN (El agente trabaja)      │
│                                                  │
│  1. @IngenieroDatos → Migración SQL + RLS        │
│  2. @Arquitecto     → Entity + DTO + Interfaces  │
│  3. @IngenieroDatos → Repository (DAL)           │
│  4. @BackendDev     → Service (BLL) + Tests      │
│  5. @BackendDev     → API Controller + Tests     │
│  6. @FrontendDev    → Vistas Razor MVC           │
│  7. @QA             → Ejecución de tests         │
│  8. @PM             → Revisión + aprobación      │
└──────────────────────────────────────────────────┘
```

### Fase 3 — Revisión y Cierre del Sprint

```
┌──────────────────────────────────────────────────┐
│  FASE 3: CIERRE DE SPRINT                        │
│                                                  │
│  1. Demo al Product Owner                        │
│  2. Tests de aceptación (usuario)                │
│  3. Merge a main con PR aprobado                 │
│  4. Deploy automático vía GitHub Actions         │
│  5. Retrospectiva del equipo                     │
│  6. Actualizar backlog para siguiente Sprint     │
└──────────────────────────────────────────────────┘
```

---

## 3. SCRUM Adaptado a Equipos con Agentes de IA

### Roles del Equipo

| Rol | Tipo | Responsabilidad |
|---|---|---|
| Product Owner | Humano | Define y prioriza el backlog, acepta las HUs |
| Scrum Master | Humano | Facilita el proceso, elimina impedimentos |
| @PM (Orquestador) | Agente IA | Coordina los agentes de capa, revisa la integración |
| @Arquitecto | Agente IA | Define estructura, Entity, DTOs, interfaces y redacta ADRs |
| @IngenieroDatos | Agente IA | Migraciones SQL, DAL repositories, RLS |
| @BackendDev | Agente IA | BLL Services, API Controllers, Tests |
| @FrontendDev | Agente IA | Vistas Razor, validación cliente |
| @QA | Agente IA | Ejecuta tests, reporta fallos |
| Desarrollador Senior | Humano | Revisión de código, toma decisiones de arquitectura y aprueba ADRs |

### Estructura del Sprint

```
Sprint = 2 semanas (10 días hábiles)

Día 1:    Sprint Planning
          → Product Owner presenta HUs priorizadas
          → Equipo estima (Story Points)
          → Agente @PM genera specs de las HUs del sprint

Días 2-9: Sprint Execution
          → Daily Standup (15 min): Qué hizo ayer / Qué hace hoy / Bloqueos
          → Agentes trabajan en paralelo por capa
          → Human reviews cada PR antes del merge

Día 10:   Sprint Review + Retrospectiva
          → Demo al Product Owner
          → Retrospectiva del equipo
          → Actualización del backlog
```

### Velocidad con Agentes de IA

| Tipo de HU | Sin Agentes | Con Agentes | Mejora |
|---|---|---|---|
| CRUD básico (1 módulo) | 3-4 días | 4-8 horas | ~5x |
| Módulo con reglas de negocio | 5-8 días | 1-2 días | ~4x |
| Módulo tiempo real | 8-12 días | 2-4 días | ~3x |
| Módulo con reportes | 5-7 días | 1-3 días | ~3x |

---

## 4. Métricas de Calidad del Proyecto

### Definition of Done (DoD)

Una Historia de Usuario se considera **DONE** cuando:

- [ ] Spec aprobado por Product Owner
- [ ] Migración SQL con `clinica_id` y RLS habilitado
- [ ] Entity, DTOs, Repository e Interface creados
- [ ] BLL Service con reglas de negocio implementadas
- [ ] Tests unitarios de BLL (cobertura ≥ 80%)
- [ ] API Controller con Swagger documentado
- [ ] Tests de integración del endpoint (cobertura ≥ 60%)
- [ ] Vistas Razor funcionales y responsivas
- [ ] Validación FluentValidation (server) + jQuery Validate (client)
- [ ] Permisos `READ`/`CREATE`/`UPDATE` verificados
- [ ] Filtro por `clinica_id` en todas las consultas
- [ ] Soft delete implementado (`activo = false`)
- [ ] PR aprobado por al menos 1 developer humano
- [ ] Deploy exitoso en staging
- [ ] Demo aceptada por Product Owner

### KPIs del Proyecto

| Métrica | Objetivo |
|---|---|
| Cobertura de tests BLL | ≥ 80% |
| Cobertura de tests API | ≥ 60% |
| Tiempo de respuesta API (p95) | < 500ms |
| Uptime del sistema | ≥ 99.5% |
| Bugs críticos en producción | 0 por Sprint |
| Deuda técnica por Sprint | ≤ 10% del tiempo del sprint |
| Velocidad promedio del equipo | Establecida en Sprint 2 |

---

## 5. Estructura de Carpetas Estándar del Repositorio

```
[Proyecto]/
├── AGENTS.md                    ← La Constitución (commit PRIMERO)
├── README.md                    ← Documentación general
├── .gitignore
├── .github/
│   └── workflows/
│       ├── ci.yml               ← Tests en cada PR
│       └── deploy.yml           ← Deploy automático a staging/prod
├── docs/
│   ├── framework/               ← Esta carpeta — metodología base
│   ├── adr/                     ← Architecture Decision Records
│   │   └── ADR-NNN-[nombre].md
│   ├── specs/                   ← Specs por módulo
│   │   └── [modulo]-spec.md
│   ├── arquitectura.md          ← Diagramas de arquitectura
│   ├── backlog.md               ← Product backlog completo
│   └── api-docs.md              ← Documentación de endpoints
├── skills/                      ← Instrucciones para agentes por capa
│   ├── skill-bll.md
│   ├── skill-dal.md
│   ├── skill-controller.md
│   ├── skill-view.md
│   └── skill-supabase.md
├── supabase/
│   ├── config.toml
│   └── migrations/              ← Migraciones versionadas
│       └── YYYYMMDDHHMMSS_[nombre].sql
├── src/
│   ├── [Proyecto].Aplicacion/   ← Frontend MVC
│   ├── [Proyecto].API/          ← Backend Web API
│   ├── [Proyecto].BLL/          ← Business Logic Layer
│   ├── [Proyecto].DAL/          ← Data Access Layer
│   ├── [Proyecto].Entity/       ← Modelos de dominio
│   ├── [Proyecto].DTO/          ← Data Transfer Objects
│   ├── [Proyecto].IOC/          ← Inyección de dependencias
│   └── [Proyecto].Utility/      ← Helpers y extensiones
└── tests/
    ├── [Proyecto].BLL.Tests/    ← Unit tests
    └── [Proyecto].API.Tests/    ← Integration tests
```

---

## 6. Comandos de Inicio de Proyecto (Bootstrap)

```bash
# 1. Crear la solución
dotnet new sln -n [Proyecto]

# 2. Crear los 8 proyectos
dotnet new mvc      -n [Proyecto].Aplicacion -o src/[Proyecto].Aplicacion
dotnet new webapi   -n [Proyecto].API        -o src/[Proyecto].API
dotnet new classlib -n [Proyecto].BLL        -o src/[Proyecto].BLL
dotnet new classlib -n [Proyecto].DAL        -o src/[Proyecto].DAL
dotnet new classlib -n [Proyecto].Entity     -o src/[Proyecto].Entity
dotnet new classlib -n [Proyecto].DTO        -o src/[Proyecto].DTO
dotnet new classlib -n [Proyecto].IOC        -o src/[Proyecto].IOC
dotnet new classlib -n [Proyecto].Utility    -o src/[Proyecto].Utility

# 3. Agregar todos a la solución
dotnet sln add src/**/*.csproj

# 4. Inicializar Supabase
supabase init
supabase start

# 5. Primera migración
supabase migration new initial_schema

# 6. Inicializar Git
git init
git add AGENTS.md   # ← AGENTS.md se commitea PRIMERO
git commit -m "feat: agregar AGENTS.md (La Constitución)"
git add .
git commit -m "chore: inicializar estructura del proyecto"
```

---

*lifecycle.md — Ciclo de vida de ingeniería de software para SaaS*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*
