# AGENTS.md — La Constitución del Proyecto SaaS Freiroute TMS

> **ARCHIVO DE REGLAS A NIVEL DE PROYECTO — v2.0**
> Todo agente de IA (Claude, Cursor, Copilot, OpenCode) DEBE leer este archivo ANTES de escribir una sola línea de código.
> Contiene decisiones duraderas del equipo escritas como declaraciones EARS.
> **Hacer commit de este archivo ANTES del primer spec.**

---

## Identidad del Producto

**Nombre:** Freiroute TMS  
**Tipo:** SaaS Multi-Tenant — Transportation Management System  
**Nivel:** Mundial (referencia: Oracle TMS, SAP TM, MercuryGate, BluJay, Trimble TMS)  
**Tagline:** *"Manage every route. Move every load."*

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

---

## Sistema de Agentes IA

| Agente | Skill | Responsabilidad Principal |
|---|---|---|
| **@PM** | skill-pm.md | Orquestación, sprint planning, coordinación de capas |
| **@Arquitecto** | skill-arquitecto.md | Entidades, DTOs, interfaces, ADRs, estructura de módulo |
| **@IngenieroDatos** | skill-dal.md | Migraciones SQL, RLS, repositorios Dapper |
| **@BackendDev** | skill-bll.md | BLL Services, API Controllers, Tests |
| **@FrontendDev** | skill-view.md | Vistas Razor, UI Freiroute, validación cliente |
| **@QA** | skill-testing.md | Tests unitarios, integración, cobertura, criterios de aceptación |

### Flujo de Trabajo de Agentes

```
@PM planifica Sprint
    → @Arquitecto define Entity + DTOs + Interfaces + ADR
    → @IngenieroDatos crea migración SQL + RLS
    → @BackendDev implementa BLL Service + API Controller
    → @FrontendDev crea Vistas Razor con Design System Freiroute
    → @QA ejecuta tests + valida cobertura
    → @PM revisa checklist + aprueba PR
    → Deploy a staging
```

### Regla de Sesión para Agentes
1. Leer AGENTS.md completo
2. Leer la skill del rol asignado
3. Leer el spec.md del módulo (en `docs/specs/`)
4. Consultar `docs/framework/convenciones.md` para estándares de código y `docs/framework/requerimientos.md` para reglas de negocio.
5. Ejecutar: spec → plan → tasks → implement → test

---

## Reglas de Arquitectura (EARS)

### Organización de Código

1. **El sistema ORGANIZARÁ** el código en exactamente 8 proyectos: `Aplicacion`, `API`, `BLL`, `DAL`, `Entity`, `DTO`, `IOC`, `Utility`.
2. **Ninguna capa PODRÁ** saltarse otra. El Controller MVC no llama al DAL. La Vista no llama al BLL.
3. **El flujo de datos SIEMPRE SERÁ**: Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL.
4. **Cuando** se genere un módulo nuevo, **el sistema REQUERIRÁ** crear en este orden: Entity → DTOs → DAL Interface → DAL Repository → BLL Interface → BLL Service → API Controller → Controller MVC → Vistas Razor.

### Estructura de Proyectos

```
Freiroute.sln
├── src/
│   ├── Freiroute.Entity/          # Entidades de dominio
│   ├── Freiroute.DTO/             # DTOs (Request + Response)
│   ├── Freiroute.DAL/             # Interfaces + Repositorios (Dapper)
│   ├── Freiroute.BLL/             # Interfaces + Services + Validators
│   ├── Freiroute.IOC/             # Inyección de dependencias
│   ├── Freiroute.Utility/         # Helpers, extensiones, constantes
│   ├── Freiroute.API/             # Web API (JWT, Controllers, Middleware)
│   └── Freiroute.Aplicacion/      # MVC (Areas, Controllers, Views)
│       ├── Areas/
│       │   ├── Admin/             # Super Admin
│       │   ├── Tenant/            # Admin de tenant
│       │   └── Portal/            # Portal del cliente
│       └── wwwroot/
│           ├── css/freiroute.css  # Design System Freiroute
│           ├── js/
│           └── assets/
├── tests/
│   ├── Freiroute.BLL.Tests/       # Tests unitarios BLL (≥80%)
│   └── Freiroute.API.Tests/       # Tests de integración (≥60%)
├── supabase/
│   └── migrations/                # Migraciones Supabase CLI
└── docs/
    ├── adr/                       # Architecture Decision Records
    ├── specs/                     # Spec por módulo (antes de implementar)
    └── framework/                 # Backlog, roadmap, design system
```

### Multi-Tenant

5. **Toda tabla de negocio DEBERÁ** contener el campo `empresa_id UUID NOT NULL` como discriminador de tenant.
6. **Toda consulta SQL DEBERÁ** filtrar por `empresa_id` — aunque RLS lo aplique también a nivel de BD.
7. **Row Level Security DEBERÁ** estar habilitado en cada tabla de negocio.
8. **El JWT DEBERÁ** contener: `user_id`, `empresa_id`, `perfil_id`, `permisos[]`, `tipo_usuario` (SUPER_ADMIN | ADMIN | OPERADOR | CONDUCTOR | CLIENTE).
9. **El middleware EXTRAERÁ** `empresa_id` del JWT y lo inyectará como `app.current_empresa_id` en el contexto de PostgreSQL.

### Base de Datos

10. **Toda migración PASARÁ** por Supabase CLI (`supabase migration new`). Prohibido ejecutar SQL ad-hoc en producción.
11. **Todos los IDs SERÁN** de tipo `UUID` generados con `gen_random_uuid()` en la BD — nunca en código C#.
12. **Toda tabla de negocio INCLUIRÁ**: `id`, `empresa_id`, `activo`, `fecha_creacion`, `fecha_modificacion`.
13. **Los registros NUNCA SE ELIMINARÁN** físicamente. Solo soft delete: `activo = false`.
14. **Los comentarios de BD SERÁN** en español (idioma del negocio del TMS).
15. **Los índices OBLIGATORIOS** son: `idx_[tabla]_empresa_id` e `idx_[tabla]_activo`.
16. **Toda tabla TENDRÁ** trigger `update_fecha_modificacion()` en UPDATE.

### Código C#

17. **Las clases C# USARÁN** PascalCase. Las variables camelCase. Las tablas SQL snake_case.
18. **Los métodos asíncronos TERMINARÁN** en `Async`: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`.
19. **Todas las respuestas de la API USARÁN** el wrapper `ApiResponse<T>`. Nunca retornar tipos puros.
20. **Los DTOs SERÁN SIEMPRE** diferentes de las Entities. Nunca exponer Entity directamente.
21. **Las interfaces COMENZARÁN** con la letra `I`: `IEmbarqueService`, `ICarrierRepository`.
22. **No existe `DeleteAsync`** en ninguna interfaz ni implementación del proyecto.

### Permisos

23. **El sistema MANEJARÁ** exactamente 3 tipos de permiso: `READ`, `CREATE`, `UPDATE`. No existe `DELETE`.
24. **Todo endpoint de API DEBERÁ** verificar permisos con `[RequirePermission("modulo", PermissionType.X)]`.
25. **El SUPER_ADMIN** gestiona todas las empresas del SaaS (no solo la suya).
26. **El ADMIN** tiene acceso completo solo a los módulos de su empresa.
27. **El OPERADOR/DISPATCHER** tiene acceso según permisos configurados por el Admin.

### Testing

28. **Todo módulo nuevo REQUERIRÁ** tests unitarios en `Freiroute.BLL.Tests` (≥ 80% cobertura).
29. **Todo endpoint crítico REQUERIRÁ** tests de integración en `Freiroute.API.Tests` (≥ 60% cobertura).
30. **El flujo TDD SERÁ**: escribir test que falla → implementar → pasar el test → refactorizar.
31. **El pipeline CI BLOQUEARÁ** el merge si `dotnet test` falla o la cobertura baja del umbral mínimo.

### Seguridad

32. **Los archivos de Supabase Storage ESTARÁN** en buckets privados. Las URLs usarán tokens temporales (signed URLs).
33. **Las claves secretas NUNCA SE COMMITEARÁN** al repositorio. Usar variables de entorno o GitHub Secrets.
34. **HTTPS SERÁ** obligatorio en todos los entornos (desarrollo con certificado local dotnet dev-certs).
35. **Los logs NUNCA CONTENDRÁN** datos sensibles (contraseñas, tokens, datos personales del cliente).

### Documentación

36. **Cada módulo TENDRÁ** un archivo `spec.md` en `docs/specs/` ANTES de que el agente escriba código.
37. **El flujo del agente SIEMPRE SERÁ**: spec → plan → tasks → implement → test. Nunca saltar directo a código.
38. **Las decisiones arquitectónicas relevantes TENDRÁN** un ADR en `docs/adr/ADR-NNN-descripcion.md`.

---

## Design System Freiroute (UI/UX)

> Todo agente que toque el frontend DEBE respetar este sistema de diseño.  
> Referencia completa en: `docs/framework/freiroute-design-system.md`

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
| **Datos / Códigos** | JetBrains Mono | 400, 500 | Números de embarque, IDs, códigos, snippets |

**Escala tipográfica (razón 1.2 — Minor Third):**

```
Page Title:    Inter 700 · 28px · #1E293B
Module Title:  Inter 600 · 20px · #1E293B
Card Title:    Inter 600 · 15px · #1E293B
Body:          Inter 400 · 13px · #1E293B
Table Header:  Inter 600 · 11px · #64748B · UPPERCASE · letter-spacing .05em
Label/Hint:    Inter 500 · 11px · #64748B
Code/ID:       JetBrains Mono · 12px
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
| **Paginado** | 20 registros/página (RNF-01.4) |

### Componentes Estándar

**Badges de estado operacional:**

```html
<!-- Entregado -->
<span class="badge-fr badge-fr-success">Entregado</span>
<!-- En tránsito -->
<span class="badge-fr badge-fr-info">En tránsito</span>
<!-- SLA en riesgo -->
<span class="badge-fr badge-fr-warning">SLA en riesgo</span>
<!-- Retrasado / Crítico -->
<span class="badge-fr badge-fr-danger">Retrasado</span>
<!-- Planificado / Borrador -->
<span class="badge-fr badge-fr-neutral">Planificado</span>
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
<a class="sb-item active" href="/embarques">
  <i class="ti ti-truck"></i> Embarques
</a>
```

### Reglas de Color Semántico para TMS

| Estado de Embarque | Color | Hex |
|---|---|---|
| DRAFT | Neutral gris | `#64748B` |
| CONFIRMED | Azul informativo | `#1A73E8` |
| ASSIGNED | Azul-cyan | `#0891B2` |
| IN_TRANSIT | Ámbar | `#F57F17` |
| DELIVERED | Verde | `#2E7D32` |
| FAILED_DELIVERY | Rojo | `#E53935` |
| ON_HOLD | Naranja oscuro | `#C2410C` |
| CANCELLED | Gris oscuro | `#374151` |

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
| Mensajes de validación (FluentValidation / jQuery Validate) | **Español** |
| Logs de aplicación (Serilog) | **Inglés** |

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
| `carrier` | Transportista o empresa de carga (propio o tercero) |
| `conductor` | Operador de vehículo registrado en el sistema |
| `dispatcher` | Planificador/asignador de embarques |
| `cliente` / `shipper` | Empresa que contrata el servicio de transporte |
| `ruta` | Secuencia de paradas optimizadas para un vehículo |
| `POD` | Proof of Delivery — prueba de entrega digital |
| `OTD` | On-Time Delivery — % de entregas a tiempo |
| `track & trace` | Rastreo GPS en tiempo real de vehículos y embarques |
| `freight` | Carga o mercancía transportada |
| `FTL` | Full Truck Load — camión completo |
| `LTL` | Less Than Truck Load — carga parcial consolidada |
| `SLA` | Service Level Agreement — compromiso de nivel de servicio |
| `ETA` | Estimated Time of Arrival — hora estimada de llegada |
| `geofence` | Zona geográfica virtual para alertas de entrada/salida |
| `backhaul` | Carga de retorno para aprovechar viaje de regreso |
| `ADR` | Architecture Decision Record — decisión arquitectónica documentada |
| `HU` | Historia de Usuario del backlog |
| `BLL` | Business Logic Layer — lógica de negocio |
| `DAL` | Data Access Layer — acceso a datos con Dapper |
| `DTO` | Data Transfer Object — objeto entre capas |
| `IOC` | Inversión de Control — contenedor DI |

---

## Checklist de Arranque de Sprint

Antes de iniciar cualquier Historia de Usuario, verificar:

- [ ] AGENTS.md leído completamente
- [ ] Skill del rol asignado leída
- [ ] `docs/framework/convenciones.md` y `docs/framework/requerimientos.md` revisados
- [ ] `spec.md` del módulo existe en `docs/specs/`
- [ ] `supabase start` ejecutado y BD local disponible
- [ ] Branch creada desde `develop`: `feature/HU-XXX-nombre-hu`
- [ ] Ningún warning en `dotnet build` antes de empezar

---

*AGENTS.md — Constitución del proyecto Freiroute TMS*
*Versión: 2.0.0 | Actualizado: 2026*
*Este archivo se versiona en Git y es la fuente de verdad para todos los agentes de IA.*
