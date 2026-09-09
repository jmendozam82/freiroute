# AGENTS.md — La Constitución del Proyecto SaaS Freiroute TMS

> **ARCHIVO DE REGLAS A NIVEL DE PROYECTO — v3.0**
> Todo agente de IA (Claude, Cursor, Copilot, OpenCode) DEBE leer este archivo COMPLETO antes de escribir una sola línea de código.
> Contiene decisiones duraderas del equipo escritas como declaraciones EARS.
> **Hacer commit de este archivo ANTES del primer spec.**

---

## Historial de versiones

| Versión | Sprint | Cambios principales |
|---------|--------|---------------------|
| 1.0 | Sprint 1 | Versión inicial — stack, arquitectura, multi-tenant |
| 2.0 | Sprint 3 | Design System completo, glosario TMS, ADRs 001-018 |
| 3.0 | Sprint 4 | **Clases CSS corregidas** (`fr-badge-*`), FSM de órdenes (ADR-019/020), protocolo de 5 fases, excepciones BD, módulos de permiso reales, ADRs 019-020, lecciones de implementación |

---

## Identidad del Producto

**Nombre:** Freiroute TMS
**Tipo:** SaaS Multi-Tenant — Transportation Management System
**Nivel:** Mundial (referencia: Oracle TMS, SAP TM, MercuryGate, BluJay, Trimble TMS)
**Tagline:** *"Manage every route. Move every load."*

---

## Estado del Proyecto

```
Sprint 1 ✅  Auth & Multi-Tenant          — 98 pts
Sprint 2 ✅  Administración SaaS           — 86 pts
Sprint 3 ✅  Maestros & Catálogos         — 88 pts
Sprint 4 ✅  Order Management Core        — 60 pts
──────────────────────────────────────────
4/26 sprints completados · ~28% del backlog
943 tests · 148 endpoints · BD en Supabase Cloud sincronizada
```

---

## Identidad del Stack

El sistema USARÁ **ASP.NET Core MVC (.NET 8)** como framework de presentación.
El sistema USARÁ **ASP.NET Core Web API (.NET 8)** como backend REST.
El sistema USARÁ **Supabase (PostgreSQL 15)** como base de datos y BaaS.
El sistema USARÁ **Dapper** como micro-ORM para consultas SQL directas.
El sistema USARÁ **Supabase Auth + JWT** para autenticación y sesión.
El sistema USARÁ **Row Level Security (RLS)** de PostgreSQL para aislamiento multi-tenant.
El sistema USARÁ **FluentValidation** para validación del lado servidor.
El sistema USARÁ **jQuery Validate + Unobtrusive** para validación del lado cliente.
El sistema USARÁ **Bootstrap 5.3** como UI Kit base.
El sistema USARÁ el **Design System Freiroute** (ver sección UI/UX) para identidad visual.
El sistema USARÁ **Supabase CLI** para todas las migraciones de base de datos.
El sistema USARÁ **GitHub Actions** como pipeline de CI/CD.
El sistema USARÁ **Serilog** para logging estructurado en JSON.
El sistema USARÁ **xUnit + Moq + FluentAssertions** para testing.
El sistema USARÁ **BCrypt.Net** para hashing de contraseñas y API keys.
El sistema USARÁ **CsvHelper** para importación/exportación CSV.
El sistema USARÁ **Leaflet.js** (CDN) para mapas interactivos en el frontend.
El sistema USARÁ **ClosedXML** para generación de archivos Excel.

---

## Sistema de Agentes IA

| Agente | Skill | Responsabilidad Principal |
|---|---|---|
| **@PM** | .opencode/skills/skill-pm.md | Orquestación, sprint planning, coordinación de capas |
| **@Arquitecto** | .opencode/skills/skill-arquitecto.md | Entidades, DTOs, interfaces, ADRs, constantes Utility |
| **@IngenieroDatos** | .opencode/skills/skill-dal.md | Migraciones SQL, RLS, repositorios Dapper |
| **@BackendDev** | .opencode/skills/skill-bll.md | Validators, BLL Services, API Controllers, Background Jobs, Fixes de deuda técnica |
| **@QA** | .opencode/skills/skill-testing.md | Tests unitarios, integración, cobertura, QA Report |
| **@FrontendDev** | .opencode/skills/skill-view.md | Vistas Razor, Design System Freiroute, validación cliente |

### Protocolo de 5 Fases por Sprint

Cada sprint se implementa en fases secuenciales. **No iniciar la siguiente fase hasta recibir confirmación de cierre de la anterior.**

```
Fase 1 → @Arquitecto
         Constantes Utility + OrderStateMachine (si aplica)
         Entities + DTOs + Interfaces DAL + Interfaces BLL
         Reporta: "dotnet build: 0 errores ✔ — Listo para Fase 2"

Fase 2 → @IngenieroDatos
         Migraciones SQL (supabase migration new + supabase db push)
         Repositorios Dapper
         Registro en DI
         Reporta: "supabase db diff vacío · dotnet build 0 errores ✔ — Listo para Fase 3"

Fase 3 → @BackendDev
         Validators FluentValidation
         BLL Services + Background Jobs
         API Controllers
         Fixes de deuda técnica del sprint anterior
         Registro en DI
         Reporta: "dotnet build: 0 errores · dotnet test: N/N ✔ — Listo para Fase 4"

Fase 4 → @QA
         Tests unitarios BLL (≥80% cobertura)
         Tests integración API (≥60% cobertura)
         QA Report en docs/specs/sprint-NN-qa-report.md
         Reporta: "N tests · BLL X% · API Y% · 0 fallos ✔ — Listo para Fase 5"

Fase 5 → @FrontendDev
         Vistas Razor (Index, Create, Detail, Edit)
         MVC Controllers en Areas/
         Design System Freiroute
         Fix de deuda técnica de UI
         Reporta: "dotnet build: 0 errores · smoke tests ✔ — Listo para @PM"
```

### Regla de Sesión para Agentes

1. Leer AGENTS.md completo (este archivo)
2. Leer el doc de cierre del sprint anterior: `docs/framework/contexto-pm-sprintN-cierre.md`
3. Leer `docs/framework/deuda-tecnica.md` — gaps abiertos que corresponden a tu rol
4. Leer el spec del sprint actual: `docs/specs/sprint-NN-EPXX-nombre.md`
5. Leer los ADRs referenciados en el spec antes de escribir código
6. Ejecutar: spec → plan → tasks → implement → test

### Mensajes de cierre estándar

```
# Formato del mensaje de cierre de cada fase:
"@PM Fase N Sprint X completada — @NombreAgente.
 Entregable 1: ✔
 Entregable 2: ✔
 [desviaciones detectadas si aplica]
 dotnet build: 0 errores ✔
 [dotnet test: N/N ✔ si aplica]
 Listo para que @SiguienteAgente inicie la Fase N+1."
```

---

## Reglas de Arquitectura (EARS)

### Organización de Código

1. **El sistema ORGANIZARÁ** el código en exactamente 8 proyectos: `Aplicacion`, `API`, `BLL`, `DAL`, `Entity`, `DTO`, `IOC`, `Utility`.
2. **Ninguna capa PODRÁ** saltarse otra. El Controller MVC no llama al DAL. La Vista no llama al BLL.
3. **El flujo de datos SIEMPRE SERÁ**: Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL.
4. **Cuando** se genere un módulo nuevo, **el sistema REQUERIRÁ** crear en este orden: Entity → DTOs → DAL Interface → DAL Repository → BLL Interface → BLL Service → Validators → API Controller → Background Job (si aplica) → Controller MVC → Vistas Razor.

### Estructura de Proyectos

```
Freiroute.sln
├── src/
│   ├── Freiroute.Entity/          # Entidades de dominio puro
│   ├── Freiroute.DTO/             # DTOs (Request + Response por módulo)
│   ├── Freiroute.DAL/             # Interfaces + Repositorios (Dapper)
│   ├── Freiroute.BLL/             # Interfaces + Services + Validators + Jobs
│   ├── Freiroute.IOC/             # Inyección de dependencias
│   ├── Freiroute.Utility/         # Helpers, constantes, OrderStateMachine, enums
│   │   ├── Constants/             # OrdenEstado, OrdenPrioridad, ModoTransporte...
│   │   └── Orders/                # OrderStateMachine.cs (FSM pura, sin dependencias)
│   ├── Freiroute.API/             # Web API (JWT, Controllers, Middleware)
│   └── Freiroute.Aplicacion/      # MVC (Areas, Controllers, Views)
│       ├── Areas/
│       │   ├── Admin/             # Super Admin
│       │   ├── Tenant/            # Admin de tenant (vistas de negocio)
│       │   └── Portal/            # Portal del cliente
│       └── wwwroot/
│           ├── css/freiroute.css  # Design System Freiroute
│           ├── js/freiroute.js    # FrApi, FrToast, FrAuth, FrUtil helpers
│           └── assets/
├── tests/
│   ├── Freiroute.BLL.Tests/       # Tests unitarios BLL (≥80%)
│   └── Freiroute.API.Tests/       # Tests de integración (≥60%)
├── supabase/
│   └── migrations/                # Migraciones Supabase CLI en orden de timestamp
└── docs/
    ├── adr/                       # Architecture Decision Records
    ├── specs/                     # Spec por sprint (consolidado por sprint completo)
    └── framework/                 # Backlog, roadmap, cierre de sprint, deuda técnica
```

### Multi-Tenant

5. **Toda tabla de negocio DEBERÁ** contener el campo `empresa_id UUID NOT NULL` como discriminador de tenant.
6. **Toda consulta SQL DEBERÁ** filtrar por `empresa_id` — aunque RLS lo aplique también a nivel de BD.
7. **Row Level Security DEBERÁ** estar habilitado en cada tabla de negocio.
8. **El JWT DEBERÁ** contener: `user_id`, `empresa_id`, `perfil_id`, `permisos[]`, `tipo_usuario` (SUPER_ADMIN | ADMIN | OPERADOR | DISPATCHER | CONDUCTOR | CLIENTE).
9. **El middleware EXTRAERÁ** `empresa_id` del JWT y lo inyectará como `app.current_empresa_id` en el contexto de PostgreSQL para que RLS funcione.
10. **En los MVC Controllers del área Tenant**, `empresa_id` se extrae con `User.GetEmpresaId()`. **No existe `[RequireModulePermission]` como atributo** — usar gates `User.HasPermission("modulo", "READ")` en las vistas y `[RequirePermission]` solo en los API controllers.

### Base de Datos

11. **Toda migración PASARÁ** por Supabase CLI (`supabase migration new`). Prohibido ejecutar SQL ad-hoc en producción.
12. **Todos los IDs SERÁN** de tipo `UUID` generados con `gen_random_uuid()` en la BD — nunca en código C#.
13. **Toda tabla de negocio INCLUIRÁ**: `id UUID PK`, `empresa_id UUID NOT NULL`, `activo BOOLEAN NOT NULL DEFAULT true`, `fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now()`, `fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()`.
14. **Los registros NUNCA SE ELIMINARÁN** físicamente. Solo soft delete: `activo = false`. La única excepción es `ubicacion_zonas` (tabla de relación pura — DELETE físico autorizado, documentar con comentario).
15. **Los comentarios de BD SERÁN** en español (idioma del negocio del TMS). Obligatorios en COMMENT ON TABLE y COMMENT ON COLUMN para columnas no obvias.
16. **Los índices OBLIGATORIOS** son: `idx_[tabla]_empresa_id` e `idx_[tabla]_activo`.
17. **Toda tabla TENDRÁ** trigger `update_fecha_modificacion()` en UPDATE.

### Excepciones documentadas al patrón estándar de BD

Las siguientes tablas **no siguen el patrón completo** por diseño — cada excepción tiene un ADR o comentario que la autoriza:

| Tabla | Excepción | Razón |
|-------|-----------|-------|
| `contadores_orden` | Sin `id` propio, sin RLS, sin trigger | Tabla de infraestructura para contadores atómicos (ADR-020) |
| `ubicacion_zonas` | Sin `activo`, sin `fecha_modificacion`, DELETE físico | Tabla de relación pura append-only (ADR-018) |
| `historial_estados_orden` | INSERT-only en operación normal | Auditoría inmutable (ADR-019) — el trigger existe por consistencia |
| `historial_estados_reclamo` | INSERT-only en operación normal | Mismo patrón que historial de órdenes |

### Código C#

18. **Las clases C# USARÁN** PascalCase. Las variables camelCase. Las tablas SQL snake_case.
19. **Los métodos asíncronos TERMINARÁN** en `Async`: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`.
20. **Todas las respuestas de la API USARÁN** el wrapper `ApiResponse<T>`. Nunca retornar tipos puros.
21. **Los DTOs SERÁN SIEMPRE** diferentes de las Entities. Nunca exponer Entity directamente.
22. **Las interfaces COMENZARÁN** con la letra `I`: `IEmbarqueService`, `ICarrierRepository`.
23. **No existe `DeleteAsync`** en ninguna interfaz ni implementación del proyecto. Solo `DeactivateAsync`.
24. **Los Builders de test** en `tests/Freiroute.BLL.Tests/Builders/` son obligatorios para entidades con más de 5 campos requeridos (patrón establecido en Sprint 4).
25. **Los campos de auditoría `Detalles`** en los servicios SIEMPRE se pasan como **JSON serializado**: `JsonSerializer.Serialize(new { ... })`. Nunca como objeto anónimo directo — causa error 22P02 en el cast `::jsonb` del repositorio.
26. **BCrypt** se usa para hashing de API keys y contraseñas. En tests unitarios, **mockear el repositorio** que usa BCrypt — nunca asumir que un hash en string literal matcheará con `BCrypt.Verify`. Los tests de integración real son los que verifican el flujo completo.

### Permisos

27. **El sistema MANEJARÁ** exactamente 3 tipos de permiso: `READ`, `CREATE`, `UPDATE`. No existe `DELETE`.
28. **Todo endpoint de API DEBERÁ** verificar permisos con `[RequirePermission("modulo", PermissionType.X)]`.
29. **Los módulos de permiso reales son exactamente estos** (usar estos strings, no inventar otros):
    ```
    ordenes · embarques · carriers · rutas · track_trace · documentos
    flota · analytics · facturacion · clientes · usuarios · configuracion
    ```
30. **El SUPER_ADMIN** gestiona todas las empresas del SaaS (no solo la suya).
31. **El ADMIN** tiene acceso completo solo a los módulos de su empresa.
32. **El OPERADOR/DISPATCHER** tiene acceso según permisos configurados por el Admin.
33. **En la FSM de órdenes (ADR-019)**, el agente que valida el rol para cada transición es el BLL Service, no el Controller. El Controller solo extrae `tipoUsuario = User.GetTipoUsuario()` y lo pasa al servicio.

### Background Jobs

34. **Todo Background Job USARÁ** el patrón `BackgroundService` + `PeriodicTimer` (ADR-013).
35. **Los Jobs USARÁN** `IServiceScopeFactory` para crear scopes por ejecución — nunca inyectar servicios Scoped en un Singleton.
36. **Los Jobs NUNCA PROPAGARÁN** excepciones al exterior — siempre `try/catch` con `ILogger.LogError`. Un job que falla en una iteración continúa en la siguiente.
37. **Los Jobs cross-tenant** (como `RecurrenciaOrdenesJob`) consultan sin filtro de `empresa_id` porque procesan TODOS los tenants. Documentar explícitamente con comentario.

### FSM de Órdenes (ADR-019)

La máquina de estados de órdenes vive en `Freiroute.Utility/Orders/OrderStateMachine.cs`.
Es la **única fuente de verdad** para transiciones. Nunca duplicar la lógica en Controllers o Views.

```
Estados válidos:
DRAFT · CONFIRMED · ASSIGNED · PICKUP_SCHEDULED · IN_TRANSIT
DELIVERED · INVOICED · CLOSED · CANCELLED · ON_HOLD
FAILED_DELIVERY · PARTIALLY_SPLIT

Estados terminales (no admiten transición): CLOSED · CANCELLED

Mapeo estado → badge CSS (ver sección UI/UX):
DRAFT           → fr-badge-neutral
CONFIRMED       → fr-badge-info
ASSIGNED        → fr-badge-info
PICKUP_SCHEDULED→ fr-badge-info
IN_TRANSIT      → fr-badge-warning
DELIVERED       → fr-badge-success
INVOICED        → fr-badge-success
CLOSED          → fr-badge-neutral
CANCELLED       → fr-badge-neutral
ON_HOLD         → fr-badge-warning
FAILED_DELIVERY → fr-badge-danger
PARTIALLY_SPLIT → fr-badge-info
```

**`TransicionesDisponibles`** en `OrdenResponseDto` es un `List<string>` en JSON (serialización de `IReadOnlySet<string>`). El frontend **no recalcula la FSM** — solo renderiza los botones que llegan del API.

### Testing

38. **Todo módulo nuevo REQUERIRÁ** tests unitarios en `Freiroute.BLL.Tests` (≥ 80% cobertura).
39. **Todo endpoint crítico REQUERIRÁ** tests de integración en `Freiroute.API.Tests` (≥ 60% cobertura).
40. **El naming de tests SERÁ**: `[Método]_[Escenario]_[ResultadoEsperado]` (ej: `CreateAsync_CuandoDtoValido_RetornaOrdenEnDraft`).
41. **El pipeline CI BLOQUEARÁ** el merge si `dotnet test` falla o la cobertura baja del umbral mínimo.
42. **Los tests de integración que usan BCrypt** deben ir a `Freiroute.API.Tests` o ser marcados con `[Fact(Skip = "Requiere BD")]` si necesitan la BD real — nunca mockear `BCrypt.Verify` en un test que pretende validar el flujo de autenticación end-to-end.
43. **Los tests de concurrencia** (ej: numeración de órdenes) se marcan `[Fact(Skip = "Requiere supabase start")]` en local y se ejecutan en CI con `supabase start` antes del merge a `main`.
44. **El QA Report** se crea en `docs/specs/sprint-NN-qa-report.md` al cerrar cada sprint, siguiendo el formato de `sprint-03-qa-report.md`.

### Seguridad

45. **Los archivos de Supabase Storage ESTARÁN** en buckets privados. Las URLs usarán tokens temporales (signed URLs).
46. **Las claves secretas NUNCA SE COMMITEARÁN** al repositorio. Usar user secrets en desarrollo, GitHub Secrets en CI.
47. **HTTPS SERÁ** obligatorio en todos los entornos (desarrollo con certificado local dotnet dev-certs).
48. **Los logs NUNCA CONTENDRÁN** datos sensibles (contraseñas, tokens, datos personales del cliente).
49. **Las API Keys externas** se almacenan como hash bcrypt (`BCrypt.Net.BCrypt.HashPassword(rawKey, workFactor: 10)`). El `rawKey` es visible **una sola vez** al crear — el response lo incluye en `RawKey` solo en esa respuesta. Las lecturas posteriores retornan `RawKey = null`.
50. **La validación de API Keys** usa `BCrypt.Net.BCrypt.Verify(rawKey, hash)` — nunca comparación directa de strings.

### Documentación

51. **Cada sprint TENDRÁ** un spec consolidado `docs/specs/sprint-NN-EPXX-nombre.md` con TODAS las HUs, antes de que el agente escriba código.
52. **El flujo del agente SIEMPRE SERÁ**: spec → plan → tasks → implement → test. Nunca saltar directo a código.
53. **Las decisiones arquitectónicas relevantes TENDRÁN** un ADR en `docs/adr/ADR-NNN-descripcion.md`.
54. **El documento de cierre de sprint** `docs/framework/contexto-pm-sprintN-cierre.md` es el artefacto de traspaso entre sprints. Contiene: estado del proyecto, gaps, desviaciones aprobadas, instrucciones técnicas para el siguiente sprint y prompt de arranque.

---

## ADRs vigentes

| ADR | Título | Sprint origen |
|-----|--------|---------------|
| ADR-001 | Stack tecnológico | Sprint 1 |
| ADR-002 | Arquitectura N-tier | Sprint 1 |
| ADR-003 | Multi-tenant RLS | Sprint 1 |
| ADR-004 | Planes y suscripciones | Sprint 2 |
| ADR-005 | Soft delete universal | Sprint 1 |
| ADR-006 | Permisos RCU sin delete | Sprint 1 |
| ADR-007 | JWT claims tenant | Sprint 1 |
| ADR-008 | ApiResponse pattern | Sprint 1 |
| ADR-009 | Modelo de permisos flags | Sprint 1 |
| ADR-010 | Onboarding wizard multi-paso | Sprint 2 |
| ADR-011 | Cifrado TOTP AES-256 | Sprint 2 |
| ADR-012 | Signed URLs logos | Sprint 2 |
| ADR-013 | Background job con PeriodicTimer | Sprint 2 |
| ADR-014 | Geocodificación con Nominatim | Sprint 3 |
| ADR-015 | Versionado de tarifas de flete | Sprint 3 |
| ADR-016 | Dependencias entity-dto-utility | Sprint 3 |
| ADR-017 | Importación CSV fail-soft | Sprint 3 |
| ADR-018 | Point-in-polygon para zonas | Sprint 3 |
| ADR-019 | Máquina de estados de la orden (FSM) | Sprint 4 |
| ADR-020 | Numeración automática de órdenes | Sprint 4 |

---

## Design System Freiroute (UI/UX)

> Todo agente que toque el frontend DEBE respetar este sistema de diseño.
> Referencia completa en: `docs/framework/freiroute-design-system.md`

### ⚠️ Corrección importante (v3.0)

Las clases CSS del proyecto usan el prefijo **`fr-`**, no `badge-fr`. El AGENTS.md v2.0 tenía la convención invertida.

```html
<!-- ✅ CORRECTO — clases reales del CSS del proyecto -->
<span class="fr-badge fr-badge-success">Entregado</span>
<span class="fr-badge fr-badge-info">En tránsito</span>
<span class="fr-badge fr-badge-warning">SLA en riesgo</span>
<span class="fr-badge fr-badge-danger">Crítico</span>
<span class="fr-badge fr-badge-neutral">Borrador</span>

<button class="btn fr-btn-primary">Confirmar</button>
<button class="btn fr-btn-secondary">Cancelar</button>

<!-- ❌ INCORRECTO — NO usar estas clases -->
<span class="badge-fr badge-fr-success">...</span>
```

### Paleta de Colores

```css
:root {
  /* Identidad Freiroute */
  --fr-navy-primary:    #0B2545;   /* Sidebar, navbar, marca */
  --fr-navy-mid:        #1B4F8A;   /* Hover sidebar, gradiente */
  --fr-action-blue:     #1A73E8;   /* Botones CTA, links, acento primario */
  --fr-cyan-accent:     #00D4FF;   /* Logo mark, item activo sidebar, highlights */
  --fr-blue-tint:       #E3F0FF;   /* Fondos de tarjetas informativas */

  /* Semántica operacional */
  --fr-success:         #2E7D32;   /* Entregado, OTD positivo, documento OK */
  --fr-success-light:   #E6F4EA;   /* Fondo badge success */
  --fr-warning:         #F57F17;   /* En tránsito, SLA en riesgo, alerta */
  --fr-warning-light:   #FFF8E1;   /* Fondo badge warning */
  --fr-danger:          #E53935;   /* Crítico, error, documento vencido */
  --fr-danger-light:    #FFEBEE;   /* Fondo badge danger */

  /* Neutrales */
  --fr-surface-bg:      #F8FAFC;   /* Fondo página */
  --fr-surface-card:    #FFFFFF;   /* Tarjetas, modales, paneles */
  --fr-text-primary:    #1E293B;   /* Texto principal */
  --fr-text-secondary:  #64748B;   /* Labels, hints, texto secundario */
  --fr-border:          #E2E8F0;   /* Bordes de tarjetas y tablas */
}
```

### Tipografía

| Rol | Fuente | Weights | Uso |
|---|---|---|---|
| **UI Principal** | Inter (Variable) | 400, 500, 600, 700 | Sistema completo: sidebar, tablas, formularios, dashboards |
| **Display / Marketing** | DM Sans | 400, 500, 700 | Portal del cliente, landing page, onboarding wizard |
| **Datos / Códigos** | JetBrains Mono | 400, 500 | Números de orden/embarque, IDs, API keys, códigos |

```
Page Title:    Inter 700 · 28px · #1E293B
Module Title:  Inter 600 · 20px · #1E293B
Card Title:    Inter 600 · 15px · #1E293B
Body:          Inter 400 · 13px · #1E293B
Table Header:  Inter 600 · 11px · #64748B · UPPERCASE · letter-spacing .05em
Label/Hint:    Inter 500 · 11px · #64748B
Code/ID:       JetBrains Mono · 12px (usar clase fr-id-code)
```

### Layout

| Elemento | Especificación |
|---|---|
| **Sidebar expandido** | 240px · fondo `#0B2545` |
| **Sidebar colapsado** | 64px · solo íconos |
| **Topbar** | 56px · fondo blanco · sombra `0 1px 3px rgba(0,0,0,.08)` |
| **Contenido** | `calc(100vw - 240px)` · padding `24px` · fondo `#F8FAFC` |
| **Cards** | `border-radius: 10px` · `border: 1px solid #E2E8F0` · `background: #fff` |
| **Tablas** | Sin borde externo · filas separadas por `border-bottom: 1px solid #E2E8F0` |
| **Paginado** | 20 registros/página (RNF-01.4) — usar partial `_Paginacion` |

### Componentes Estándar

**Badges de estado operacional (usar SIEMPRE el prefijo fr-):**

```html
<!-- Estados de órdenes (ADR-019) -->
<span class="fr-badge fr-badge-neutral">Borrador</span>
<span class="fr-badge fr-badge-info">Confirmada</span>
<span class="fr-badge fr-badge-info">Asignada</span>
<span class="fr-badge fr-badge-warning">En tránsito</span>
<span class="fr-badge fr-badge-success">Entregada</span>
<span class="fr-badge fr-badge-success">Facturada</span>
<span class="fr-badge fr-badge-neutral">Cerrada</span>
<span class="fr-badge fr-badge-neutral">Cancelada</span>
<span class="fr-badge fr-badge-warning">En espera</span>
<span class="fr-badge fr-badge-danger">Entrega fallida</span>
<span class="fr-badge fr-badge-info">Dividida parcialmente</span>

<!-- Prioridades de órdenes -->
<span class="fr-badge fr-badge-danger">CRÍTICO</span>
<span class="fr-badge fr-badge-warning">ALTO</span>
<span class="fr-badge fr-badge-neutral">NORMAL</span>
<span class="fr-badge fr-badge-neutral">BAJO</span>
```

**Helpers de clase disponibles en `OrdenUiHelper.cs`:**

```csharp
OrdenUiHelper.GetEstadoClass(string estado)   // → "fr-badge-warning" etc.
OrdenUiHelper.GetEstadoLabel(string estado)   // → "En tránsito" etc.
OrdenUiHelper.GetAccionLabel(string estado)   // → "Iniciar tránsito" etc.
OrdenUiHelper.GetAccionIcon(string estado)    // → "ti-navigation" etc.
OrdenUiHelper.GetAccionClass(string estado)   // → "fr-btn-primary" etc.
OrdenUiHelper.GetPrioridadClass(string p)     // → "fr-badge-danger" etc.
```

**KPI Cards del dashboard:**

```html
<div class="kpi-card">
  <div class="kpi-label">Embarques hoy</div>
  <div class="kpi-value text-fr-blue">148</div>
  <div class="kpi-delta kpi-up">↑ 12% vs. ayer</div>
</div>
```

**Sidebar item activo:**

```html
<!-- Activo: background rgba(0,212,255,.12) · texto #00D4FF · border-right 2px #00D4FF -->
<a class="fr-sidebar-item active" href="/ordenes">
  <i class="ti ti-clipboard-list fr-icon"></i>
  <span>Órdenes</span>
</a>
```

**Código/ID monospace (números de orden, API keys, UUIDs truncados):**

```html
<code class="fr-id-code">ORD-2026-00001</code>
<code class="fr-id-code">frk_live_3f7a9...</code>
```

**Reglas de formularios:**
- Validaciones de cliente con alertas inline — **NUNCA `alert()`**
- Todos los mensajes de éxito/error vía **`FrToast`** (no `alert()`, no `confirm()`)
- Todas las llamadas AJAX vía **`FrApi`** (no `fetch` crudo)
- Vistas del área Tenant nombradas como `Detalle.cshtml` (no `Detail.cshtml`)

### Reglas de Color Semántico para TMS (tabla completa Sprint 4)

| Estado | Color | Badge CSS |
|---|---|---|
| DRAFT | Neutral gris | `fr-badge-neutral` |
| CONFIRMED | Azul informativo | `fr-badge-info` |
| ASSIGNED | Azul-cyan | `fr-badge-info` |
| PICKUP_SCHEDULED | Azul | `fr-badge-info` |
| IN_TRANSIT | Ámbar | `fr-badge-warning` |
| DELIVERED | Verde | `fr-badge-success` |
| INVOICED | Verde | `fr-badge-success` |
| CLOSED | Gris | `fr-badge-neutral` |
| CANCELLED | Gris oscuro | `fr-badge-neutral` |
| ON_HOLD | Naranja | `fr-badge-warning` |
| FAILED_DELIVERY | Rojo | `fr-badge-danger` |
| PARTIALLY_SPLIT | Azul-cyan | `fr-badge-info` |

---

## ModoTransporte — Dominios distintos

**⚠️ Punto de confusión frecuente — leer con atención:**

El sistema tiene dos listas de modos de transporte con valores distintos:

| Contexto | Valores válidos | Tabla |
|---------|----------------|-------|
| **Órdenes de transporte** (EP-04) | `TERRESTRE · AEREO · MARITIMO · FERROVIARIO · INTERMODAL` | `ordenes.modo_transporte` |
| **Tarifas de flete** (EP-03) | `FTL · LTL · AEREO · MARITIMO · FERROVIARIO · INTERMODAL` | `tarifas_base.modo_transporte` |

`FTL` y `LTL` son conceptos de carga (Full Truck Load / Less Than Truck Load) que aplican a la contratación de tarifas pero no son modos de transporte en el sentido de la orden. Los valores **no son intercambiables** entre tablas.

---

## Convención de Idiomas

| Elemento | Idioma |
|---|---|
| Interfaz de usuario (labels, mensajes, menús) | **Español** |
| Nombres de tablas y columnas SQL | **Español** (snake_case) |
| Comentarios de BD | **Español** |
| Clases, métodos, interfaces C# | **Inglés** |
| Comentarios de código C# | **Inglés** |
| Documentación técnica (docs/) | **Español** |
| Mensajes de validación | **Español** |
| Logs de aplicación (Serilog) | **Inglés** |
| Labels de estados en UI | **Español** (usar `OrdenEstado.GetLabel(estado)` o `OrdenUiHelper`) |

---

## Glosario del Dominio TMS Freiroute

| Término | Descripción |
|---|---|
| `tenant` / `empresa` | Organización de transporte que usa el SaaS Freiroute |
| `empresa_id` | Discriminador universal de tenant en todas las tablas |
| `activo` | Flag booleano que reemplaza el DELETE físico (soft delete) |
| `RLS` | Row Level Security — aislamiento de datos a nivel BD |
| `embarque` / `shipment` | Operación de transporte individual asignada a un carrier |
| `orden` | Solicitud de transporte del cliente (puede consolidarse en embarque) |
| `numero_orden` | Número legible de la orden (ej: `ORD-2026-00001`) — se genera al CONFIRMAR, no al crear |
| `FSM` | Finite State Machine — máquina de estados de la orden (ADR-019) |
| `DRAFT` | Estado inicial de la orden — sin número asignado, editable |
| `CONFIRMED` | Orden confirmada — tiene número asignado (ADR-020), editable |
| `PARTIALLY_SPLIT` | Orden dividida en sub-órdenes — cada sub-orden tiene su propio ciclo FSM |
| `split` | División de una orden en múltiples sub-órdenes de menor volumen |
| `consolidación` | Agrupación de múltiples órdenes CONFIRMED en un mismo shipment |
| `re-entrega` | Nueva orden creada tras un rechazo de entrega (FAILED_DELIVERY) |
| `reclamo` | Solicitud formal del cliente por daño, pérdida o retraso |
| `PO` | Purchase Order — número de orden de compra del cliente para trazabilidad |
| `SO` | Sales Order — número de orden de venta |
| `SLA` | Service Level Agreement — compromiso de nivel de servicio de entrega |
| `SLA status` | Estado calculado: OK · EN_RIESGO · CRITICO · VENCIDO |
| `API Key` | Clave de integración REST para clientes enterprise (formato `frk_live_...`) |
| `carrier` | Transportista o empresa de carga (propio o tercero) |
| `conductor` | Operador de vehículo registrado en el sistema |
| `dispatcher` | Planificador/asignador de embarques |
| `cliente` / `shipper` | Empresa que contrata el servicio de transporte |
| `ruta` | Secuencia de paradas optimizadas para un vehículo |
| `POD` | Proof of Delivery — prueba de entrega digital |
| `OTD` | On-Time Delivery — % de entregas a tiempo |
| `track & trace` | Rastreo GPS en tiempo real de vehículos y embarques |
| `FTL` | Full Truck Load — camión completo (contexto de tarifas) |
| `LTL` | Less Than Truck Load — carga parcial consolidada (contexto de tarifas) |
| `ETA` | Estimated Time of Arrival — hora estimada de llegada |
| `geofence` | Zona geográfica virtual para alertas de entrada/salida |
| `backhaul` | Carga de retorno para aprovechar viaje de regreso |
| `ADR` | Architecture Decision Record — decisión arquitectónica documentada |
| `HU` | Historia de Usuario del backlog |
| `BLL` | Business Logic Layer — lógica de negocio |
| `DAL` | Data Access Layer — acceso a datos con Dapper |
| `DTO` | Data Transfer Object — objeto entre capas |
| `IOC` | Inversión de Control — contenedor DI |
| `fail-soft` | Patrón de importación CSV donde las filas inválidas se reportan sin detener el proceso (ADR-017) |
| `ray casting` | Algoritmo de point-in-polygon para determinar si un punto cae dentro de una zona (ADR-018) |
| `RFQ` | Request for Quotation — solicitud de cotización a carriers (Sprint 6) |

---

## Checklist de Arranque de Sprint

Antes de iniciar cualquier Historia de Usuario, verificar:

- [ ] `AGENTS.md` leído completamente (este archivo)
- [ ] `docs/framework/contexto-pm-sprintN-cierre.md` leído — contiene el estado real del proyecto, gaps activos y desviaciones aprobadas
- [ ] `docs/framework/deuda-tecnica.md` revisado — gaps que corresponden a tu rol en este sprint
- [ ] `docs/specs/sprint-NN-EPXX-nombre.md` leído — spec completo del sprint actual
- [ ] ADRs referenciados en el spec leídos
- [ ] `supabase start` ejecutado y BD local disponible (o verificar conexión a BD cloud)
- [ ] Branch creada desde `develop`: `feature/sprint-NN-epXX-nombre`
- [ ] `dotnet build` sin warnings antes de empezar
- [ ] Tests actuales en verde: `dotnet test` sin fallos

---

## Lecciones de implementación (Sprints 1–4)

Errores detectados en sprints anteriores que los agentes NO deben repetir:

| Lección | Detalle | Sprint |
|---------|---------|--------|
| **Auditoría: pasar Detalles como JSON** | `JsonSerializer.Serialize(new {...})` — nunca objeto anónimo directo. El cast `::jsonb` del repo falla silenciosamente con texto plano. | Sprint 4 G-15 |
| **BCrypt: no comparar hash contra texto crudo** | `BCrypt.Verify(rawKey, storedHash)` — nunca `WHERE clave_hash = @rawKey`. | Sprint 4 G-14 |
| **Snapshot de plantillas: cargar la orden antes de serializar** | Llamar `GetByIdAsync` primero, luego mapear a DTO y serializar. No serializar `new OrdenRequestDto()` vacío. | Sprint 4 G-16 |
| **JOINs en el listado paginado** | El query de `GetAllAsync` (listado) necesita los mismos JOINs que `GetDetailAsync`. Los UUIDs crudos en respuesta son un error de SQL, no de mapeo. | Sprint 4 G-18 |
| **historial: asignar OrdenId después del INSERT** | El RETURNING id del `CreateAsync` debe asignarse al entity antes de crear el historial. | Sprint 4 G-17 |
| **FrApi.patch con body** | El helper `FrApi.patch` requiere que el body se serialice correctamente. Fix aplicado en Sprint 4 — no reimplementar el helper. | Sprint 4 G-10/G-11 |
| **Clases CSS con prefijo fr-** | `fr-badge-*` no `badge-fr-*`. Verificar en `wwwroot/css/freiroute.css` antes de usar. | Sprint 4 |
| **MVC: no existe [RequireModulePermission]** | En el área Tenant usar `User.HasPermission()` en vistas y `[Authorize]` en controllers. `[RequirePermission]` solo existe en API Controllers. | Sprint 4 |
| **ModoTransporte: dos dominios** | Órdenes usa TERRESTRE/AEREO/... Tarifas usa FTL/LTL/... No mezclar. | Sprint 4 |
| **supabase db push antes del smoke test** | Las migraciones del sprint deben estar aplicadas en la BD cloud antes del smoke test final. Verificar con `supabase db diff --linked`. | Sprint 4 |

---

*AGENTS.md — Constitución del proyecto Freiroute TMS*
*Versión: 3.0.0 | Actualizado: Sprint 4 · 2026*
*Este archivo se versiona en Git y es la fuente de verdad para todos los agentes de IA.*
*Próxima actualización: Sprint 5 al cerrar.*
