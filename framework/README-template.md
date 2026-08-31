# README — Framework SaaS con Supabase y ASP.NET Core

> **Plantilla base para nuevos proyectos**
> Copiar y adaptar este archivo como el README principal del nuevo proyecto.

---

# [NombreProyecto]

> Breve descripción del sistema en 1-2 oraciones.

**Tipo de sistema:** SaaS (Software as a Service) + BaaS (Backend as a Service)
**Stack:** ASP.NET Core 8 + Supabase + PostgreSQL 15
**Modelo:** Multi-tenant con Row Level Security

---

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Frontend | ASP.NET Core MVC (.NET 8) — Razor Pages |
| Backend | ASP.NET Core Web API (.NET 8) |
| Base de datos | PostgreSQL 15 via Supabase |
| ORM | Dapper (SQL directo) |
| Autenticación | Supabase Auth + JWT |
| Aislamiento | Row Level Security (RLS) de PostgreSQL |
| Validación (server) | FluentValidation |
| Validación (client) | jQuery Validate |
| UI Kit | Bootstrap 5.3 |
| Tiempo real | Supabase Realtime + SignalR |
| Storage | Supabase Storage |
| CI/CD | GitHub Actions |
| IA | Claude Code CLI + AGENTS.md |

---

## Arquitectura

```
[NombreProyecto].Aplicacion/   ← Frontend MVC (Razor Pages)
[NombreProyecto].API/          ← REST API (JWT + Swagger)
[NombreProyecto].BLL/          ← Reglas de negocio + FluentValidation
[NombreProyecto].DAL/          ← Repositorios + Dapper
[NombreProyecto].Entity/       ← Modelos de dominio
[NombreProyecto].DTO/          ← DTOs Request/Response
[NombreProyecto].IOC/          ← Inyección de dependencias
[NombreProyecto].Utility/      ← Helpers + ApiResponse<T>
```

**Flujo de datos:** Vista → Controller MVC → API Controller → BLL → DAL → Supabase/PostgreSQL

---

## Inicio Rápido

### Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Supabase CLI](https://supabase.com/docs/guides/cli)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para Supabase local)
- [VS Code](https://code.visualstudio.com/) + C# Dev Kit

### Instalación

```bash
# 1. Clonar el repositorio
git clone https://github.com/[org]/[nombre-proyecto].git
cd [nombre-proyecto]

# 2. Iniciar Supabase local
supabase start

# 3. Aplicar migraciones
supabase db push

# 4. Configurar variables de entorno
cp .env.example .env.local
# Editar .env.local con los valores de tu entorno

# 5. Restaurar dependencias
dotnet restore

# 6. Ejecutar el proyecto
dotnet run --project src/[NombreProyecto].API       # Backend (puerto 5001)
dotnet run --project src/[NombreProyecto].Aplicacion # Frontend (puerto 5000)
```

### Verificar

- **API Docs:** https://localhost:5001/swagger
- **Frontend:** https://localhost:5000
- **Supabase Studio:** http://localhost:54323

---

## Comandos Frecuentes

```bash
# Base de datos
supabase migration new [nombre]    # Nueva migración
supabase db push                   # Aplicar migraciones
supabase migration list            # Estado de migraciones

# Tests
dotnet test                        # Todos los tests
dotnet test tests/[P].BLL.Tests   # Solo unit tests
dotnet test tests/[P].API.Tests   # Solo integration tests

# Build
dotnet build                       # Compilar
dotnet run --project src/[P].API  # Ejecutar API
```

---

## Documentación

| Documento | Descripción |
|---|---|
| [AGENTS.md](./AGENTS.md) | La Constitución — reglas para agentes de IA |
| [docs/framework/lifecycle.md](./docs/framework/lifecycle.md) | Ciclo de vida de ingeniería |
| [docs/framework/arquitectura.md](./docs/framework/arquitectura.md) | Arquitectura del sistema |
| [docs/framework/requerimientos.md](./docs/framework/requerimientos.md) | RF, RNF y reglas de negocio |
| [docs/framework/backlog.md](./docs/framework/backlog.md) | Product backlog con HUs por Sprint |
| [docs/framework/testing.md](./docs/framework/testing.md) | Guía de testing y TDD |
| [docs/framework/convenciones.md](./docs/framework/convenciones.md) | Convenciones de código |
| [docs/framework/quickstart.md](./docs/framework/quickstart.md) | Guía de inicio de proyecto |

---

## Metodología

Este proyecto sigue los **4 pilares convergentes de 2026**:

1. **SDD** (Spec-Driven Development): spec → plan → tasks → implement
2. **TDD** (Test-Driven Development): el test primero, la implementación después
3. **SCRUM + IA**: agentes de IA como miembros del equipo de desarrollo
4. **IaC + BaaS**: infraestructura como código, backend gestionado por Supabase

---

*Basado en el Framework SaaS con Supabase v1.0.0 (Vittal, 2026)*
