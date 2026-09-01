# Guía de Inicio de Proyecto — Bootstrap desde Cero

> **La secuencia completa y precisa para inicializar un nuevo proyecto SaaS**
> Contiene los comandos exactos, el orden correcto y las mejores prácticas de 2026.
> Tiempo estimado: ~2 horas para completar Fase 0 (una sola vez por proyecto).

---

## Secuencia de Tres Niveles

```
┌─────────────────────────────────────────────────────────────┐
│  NIVEL 1: La Constitución                                   │
│  AGENTS.md → Define las reglas para SIEMPRE                 │
│  ↓                                                          │
│  NIVEL 2: El Loop de Desarrollo                             │
│  spec → plan → tasks → implement (por cada módulo)          │
│  ↓                                                          │
│  NIVEL 3: La Ejecución                                      │
│  TDD + Agentes de IA + SCRUM                                │
└─────────────────────────────────────────────────────────────┘
```

---

## Fase 0 — La Constitución (~2 horas, HECHA UNA SOLA VEZ)

### Paso 1: Crear el Repositorio

```bash
# En GitHub: crear el repo [nombre-proyecto]
# Luego clonar localmente:
git clone https://github.com/[org]/[nombre-proyecto].git
cd [nombre-proyecto]
```

### Paso 2: Crear AGENTS.md — EL PRIMER COMMIT

> **⚡ REGLA CRÍTICA:** `AGENTS.md` se commitea ANTES que cualquier código.
> Esta es la práctica convergente en 2026 para proyectos con agentes de IA.

```bash
# Copiar la plantilla AGENTS.md de este framework
cp docs/framework/AGENTS.md ./AGENTS.md

# Editar AGENTS.md con los datos del proyecto específico
# (nombre, stack si difiere, términos del dominio, etc.)

# Commitear AGENTS.md PRIMERO — esto es la Constitución
git add AGENTS.md
git commit -m "feat: La Constitución del proyecto — AGENTS.md"
git push origin main
```

### Paso 3: Crear la Solución .NET

```powershell
# Crear la solución principal
dotnet new sln -n [NombreProyecto]

# Crear los 8 proyectos de la arquitectura N-Tier
dotnet new mvc      -n [NombreProyecto].Aplicacion -o src/[NombreProyecto].Aplicacion
dotnet new webapi   -n [NombreProyecto].API        -o src/[NombreProyecto].API
dotnet new classlib -n [NombreProyecto].BLL        -o src/[NombreProyecto].BLL
dotnet new classlib -n [NombreProyecto].DAL        -o src/[NombreProyecto].DAL
dotnet new classlib -n [NombreProyecto].Entity     -o src/[NombreProyecto].Entity
dotnet new classlib -n [NombreProyecto].DTO        -o src/[NombreProyecto].DTO
dotnet new classlib -n [NombreProyecto].IOC        -o src/[NombreProyecto].IOC
dotnet new classlib -n [NombreProyecto].Utility    -o src/[NombreProyecto].Utility

# Crear proyectos de tests
dotnet new xunit -n [NombreProyecto].BLL.Tests -o tests/[NombreProyecto].BLL.Tests
dotnet new xunit -n [NombreProyecto].API.Tests -o tests/[NombreProyecto].API.Tests

# Agregar TODOS los proyectos a la solución
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj
```

### Paso 4: Configurar Referencias entre Proyectos

```powershell
# Entity no depende de nadie
# DTO depende de Entity
dotnet add src/[NombreProyecto].DTO reference src/[NombreProyecto].Entity

# DAL depende de Entity y DTO
dotnet add src/[NombreProyecto].DAL reference src/[NombreProyecto].Entity
dotnet add src/[NombreProyecto].DAL reference src/[NombreProyecto].DTO

# BLL depende de DAL, Entity y DTO
dotnet add src/[NombreProyecto].BLL reference src/[NombreProyecto].DAL
dotnet add src/[NombreProyecto].BLL reference src/[NombreProyecto].Entity
dotnet add src/[NombreProyecto].BLL reference src/[NombreProyecto].DTO

# IOC depende de BLL y DAL
dotnet add src/[NombreProyecto].IOC reference src/[NombreProyecto].BLL
dotnet add src/[NombreProyecto].IOC reference src/[NombreProyecto].DAL

# API depende de BLL, DTO e IOC
dotnet add src/[NombreProyecto].API reference src/[NombreProyecto].BLL
dotnet add src/[NombreProyecto].API reference src/[NombreProyecto].DTO
dotnet add src/[NombreProyecto].API reference src/[NombreProyecto].IOC

# Aplicacion depende de API, DTO e IOC
dotnet add src/[NombreProyecto].Aplicacion reference src/[NombreProyecto].API
dotnet add src/[NombreProyecto].Aplicacion reference src/[NombreProyecto].DTO
dotnet add src/[NombreProyecto].Aplicacion reference src/[NombreProyecto].IOC

# Tests dependen de la capa que testean
dotnet add tests/[NombreProyecto].BLL.Tests reference src/[NombreProyecto].BLL
dotnet add tests/[NombreProyecto].BLL.Tests reference src/[NombreProyecto].Entity
dotnet add tests/[NombreProyecto].BLL.Tests reference src/[NombreProyecto].DTO
dotnet add tests/[NombreProyecto].API.Tests reference src/[NombreProyecto].API
```

### Paso 5: Instalar Paquetes NuGet

```powershell
# En [Proyecto].DAL — acceso a datos
dotnet add src/[NombreProyecto].DAL package Dapper
dotnet add src/[NombreProyecto].DAL package Npgsql

# En [Proyecto].BLL — validación
dotnet add src/[NombreProyecto].BLL package FluentValidation
dotnet add src/[NombreProyecto].BLL package FluentValidation.DependencyInjectionExtensions

# En [Proyecto].API — autenticación y documentación
dotnet add src/[NombreProyecto].API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/[NombreProyecto].API package Swashbuckle.AspNetCore
dotnet add src/[NombreProyecto].API package Serilog.AspNetCore

# En [Proyecto].Aplicacion — cliente de Supabase
dotnet add src/[NombreProyecto].Aplicacion package Supabase

# En proyectos de tests
dotnet add tests/[NombreProyecto].BLL.Tests package Moq
dotnet add tests/[NombreProyecto].BLL.Tests package FluentAssertions
dotnet add tests/[NombreProyecto].API.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/[NombreProyecto].API.Tests package FluentAssertions
```

### Paso 6: Inicializar Supabase

```bash
# Instalar Supabase CLI (si no está instalado)
# Windows: winget install Supabase.CLI
# o: npm install -g supabase

# Inicializar Supabase en el proyecto
supabase init

# Iniciar Supabase local (requiere Docker)
supabase start

# Verificar que esté corriendo
supabase status
```

### Paso 7: Crear la Primera Migración (Schema Base)

```bash
# Crear la migración del schema inicial
supabase migration new initial_schema

# Editar el archivo generado en supabase/migrations/YYYYMMDDHHMMSS_initial_schema.sql
# Agregar las tablas base: tenants, perfiles, usuarios, permisos
```

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_initial_schema.sql

-- ============================================================
-- TABLA: tenants (organizaciones que usan el sistema)
-- ============================================================
CREATE TABLE IF NOT EXISTS tenants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre          VARCHAR(200) NOT NULL,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    plan            VARCHAR(50) NOT NULL DEFAULT 'starter',
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

COMMENT ON TABLE tenants IS 'Organizaciones que usan el sistema en modo SaaS';
COMMENT ON COLUMN tenants.slug IS 'Identificador único URL-friendly del tenant';

-- ============================================================
-- TABLA: perfiles (roles de usuario)
-- ============================================================
CREATE TABLE IF NOT EXISTS perfiles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    nombre          VARCHAR(100) NOT NULL,
    descripcion     TEXT,
    es_admin        BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

CREATE INDEX idx_perfiles_tenant_id ON perfiles(tenant_id);
ALTER TABLE perfiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation" ON perfiles
    FOR ALL USING (tenant_id = (current_setting('app.current_tenant_id', true))::UUID);

COMMENT ON TABLE perfiles IS 'Perfiles de usuario con sus permisos por módulo';

-- ============================================================
-- TABLA: usuarios
-- ============================================================
CREATE TABLE IF NOT EXISTS usuarios (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    perfil_id       UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    auth_user_id    UUID UNIQUE,              -- ID de Supabase Auth
    nombre          VARCHAR(100) NOT NULL,
    apellido        VARCHAR(100) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    telefono        VARCHAR(20),
    avatar_url      TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

CREATE INDEX idx_usuarios_tenant_id ON usuarios(tenant_id);
CREATE INDEX idx_usuarios_perfil_id ON usuarios(perfil_id);
CREATE INDEX idx_usuarios_email ON usuarios(email);
ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation" ON usuarios
    FOR ALL USING (tenant_id = (current_setting('app.current_tenant_id', true))::UUID);

COMMENT ON TABLE usuarios IS 'Usuarios del sistema con su perfil y tenant';

-- ============================================================
-- TABLA: permisos (READ/CREATE/UPDATE por módulo y perfil)
-- ============================================================
CREATE TABLE IF NOT EXISTS permisos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    perfil_id   UUID NOT NULL REFERENCES perfiles(id) ON DELETE CASCADE,
    modulo      VARCHAR(100) NOT NULL,
    tipo        VARCHAR(20) NOT NULL CHECK (tipo IN ('READ', 'CREATE', 'UPDATE')),
    activo      BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (perfil_id, modulo, tipo)
);

CREATE INDEX idx_permisos_perfil_id ON permisos(perfil_id);

COMMENT ON TABLE permisos IS 'Permisos granulares READ/CREATE/UPDATE por módulo y perfil';
COMMENT ON COLUMN permisos.tipo IS 'No existe DELETE — los registros solo se desactivan';
```

```bash
# Aplicar la migración localmente
supabase db push

# Verificar que las tablas existen
supabase db diff
```

### Paso 8: Configurar Variables de Entorno

```bash
# Crear .env.local (NO commitear al repo)
cat > .env.local << 'EOF'
SUPABASE_URL=http://localhost:54321
SUPABASE_ANON_KEY=[anon-key-de-supabase-start]
SUPABASE_SERVICE_ROLE_KEY=[service-role-key]
CONNECTION_STRING=Host=localhost;Port=54322;Database=postgres;Username=postgres;Password=postgres
JWT_SECRET=[generar-con-openssl-rand-hex-32]
JWT_ISSUER=[nombre-proyecto]-api
JWT_AUDIENCE=[nombre-proyecto]-client
EOF

# Agregar .env.local al .gitignore
echo ".env.local" >> .gitignore
echo "appsettings.*.json" >> .gitignore
```

### Paso 9: Configurar GitHub Actions

```bash
mkdir -p .github/workflows

cat > .github/workflows/ci.yml << 'EOF'
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
EOF
```

### Paso 10: Commit Inicial Completo

```bash
# Agregar el README.md del proyecto
cp docs/framework/README-template.md ./README.md
# Editar README.md con el nombre y descripción del proyecto

# Commit de la estructura base
git add .
git commit -m "chore: inicializar estructura del proyecto [NombreProyecto]

- Solución .NET con arquitectura N-Tier (8 proyectos)
- Proyectos de tests (BLL + API)
- Supabase inicializado con schema base
- GitHub Actions CI configurado
- .gitignore y variables de entorno

Based on: SaaS Framework v1.0.0 (Vittal 2026)"

git push origin main
```

---

## Fase 1 — Setup del Loop por Módulo (~30 min, ANTES de cada módulo)

### Plantilla de Spec por Módulo

Crear en `docs/specs/[modulo]-spec.md`:

```markdown
# Spec: [Nombre del Módulo]

**Historia de Usuario:** HU-XX
**Sprint:** [número]
**Fecha:** YYYY-MM-DD
**Aprobado por:** [Product Owner]

## Contexto

[¿Qué problema resuelve este módulo? ¿Quién lo usará?]

## Comportamiento Esperado

### [Escenario 1]

DADO QUE [condición inicial]
CUANDO [el usuario realiza la acción]
ENTONCES [el sistema debe...]

### [Escenario 2]

DADO QUE [condición]
CUANDO [acción]
ENTONCES [resultado]

## Reglas de Negocio Específicas

- [Regla 1]
- [Regla 2]

## Entidades de Datos

```
Tabla: [nombre_tabla]
- id: UUID (PK, autogenerado)
- tenant_id: UUID (FK, obligatorio)
- [campo1]: [tipo] — [descripción]
- [campo2]: [tipo] — [descripción]
- activo: BOOLEAN (soft delete)
- fecha_creacion: TIMESTAMPTZ
- fecha_modificacion: TIMESTAMPTZ
```

## API Endpoints

| Método | Ruta | Permiso | Descripción |
|---|---|---|---|
| GET | /api/[modulo] | READ | Obtener todos (paginado) |
| GET | /api/[modulo]/{id} | READ | Obtener por ID |
| POST | /api/[modulo] | CREATE | Crear nuevo |
| PUT | /api/[modulo]/{id} | UPDATE | Editar existente |
| DELETE | /api/[modulo]/{id}/deactivate | UPDATE | Desactivar (soft delete) |

## Criterios de Aceptación

- [ ] [Criterio 1]
- [ ] [Criterio 2]
- [ ] Tests unitarios BLL ≥ 80%
- [ ] Tests de integración API ≥ 60%
- [ ] PR revisado y aprobado

## Notas Técnicas

- [Nota 1 para el agente implementador]
- [Nota 2]
```

---

## Fase 2 — Inicio de Sesión de Agente

Al iniciar una nueva sesión de trabajo con el agente, enviar este prompt:

```
Lee el archivo AGENTS.md en la raíz del repositorio.
Lee el spec del módulo activo en docs/specs/[modulo]-spec.md.
Luego lee el tasks.md del sprint actual.

Resumen del contexto del proyecto:
- Proyecto: [NombreProyecto]
- Stack: ASP.NET Core 8 + Supabase + PostgreSQL
- Módulo actual: [nombre del módulo]
- Sprint: [número]
- Tu rol: [@Rol]

¿Qué tareas están pendientes para ti según el tasks.md?
```

---

## Comandos de Referencia Rápida

```bash
# Iniciar el entorno de desarrollo
supabase start                              # Inicia BD local
dotnet run --project src/[P].API           # Inicia el API
dotnet run --project src/[P].Aplicacion    # Inicia el Frontend

# Migraciones de BD
supabase migration new [nombre]            # Crear nueva migración
supabase db push                           # Aplicar migraciones pendientes
supabase migration list                    # Ver estado de migraciones

# Tests
dotnet test                                # Todos los tests
dotnet test tests/[P].BLL.Tests           # Solo tests BLL
dotnet test tests/[P].API.Tests           # Solo tests API

# Agentes de IA
export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1
claude --teammate-mode in-process

# Git workflow
git checkout -b feat/HU-XX-nombre-modulo  # Crear rama de feature
git add . && git commit -m "feat(modulo): descripción"
git push origin feat/HU-XX-nombre-modulo
# Crear PR → Review → Merge a develop
```

---

## Checklist de Inicio de Proyecto

```markdown
## ☑ Checklist Fase 0 — [NombreProyecto]

### Repositorio y Constitución
- [ ] Repositorio GitHub creado
- [ ] AGENTS.md commiteado PRIMERO
- [ ] README.md creado con descripción del proyecto

### Estructura del Proyecto
- [ ] Solución .NET con 8 proyectos creada
- [ ] Proyectos de tests creados
- [ ] Referencias entre proyectos configuradas
- [ ] Paquetes NuGet instalados

### Base de Datos
- [ ] Supabase inicializado (local + cloud)
- [ ] Supabase corriendo localmente (`supabase start`)
- [ ] Primera migración: schema base (tenants, perfiles, usuarios, permisos)
- [ ] RLS habilitado en todas las tablas base
- [ ] Migración aplicada exitosamente

### Configuración
- [ ] Variables de entorno en .env.local
- [ ] appsettings.json base configurado
- [ ] .gitignore actualizado (secrets excluidos)

### CI/CD
- [ ] GitHub Actions CI configurado
- [ ] Tests corren exitosamente en el pipeline

### Documentación
- [ ] Spec del Sprint 0 creado
- [ ] backlog.md inicial creado
- [ ] Carpeta docs/specs/ creada

### Verificación Final
- [ ] `dotnet build` sin errores
- [ ] `dotnet test` sin fallos
- [ ] API accesible en https://localhost:PORT/swagger
- [ ] Frontend accesible en https://localhost:PORT
```

---

*quickstart.md — Guía de inicio de proyecto para SaaS con Supabase*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*
