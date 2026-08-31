# ADR-003: Supabase como BaaS en lugar de Infraestructura Propia

| Campo | Valor |
|---|---|
| **ID** | ADR-003 |
| **Título** | Supabase como BaaS (Backend as a Service) en lugar de infraestructura propia o competidores |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-15 |
| **Decidido por** | CTO + Arquitecto de software |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El proyecto requería una solución de base de datos y servicios backend que cubriera:

1. **Base de datos PostgreSQL** gestionada
2. **Autenticación** (registro, login, JWT, recuperación de contraseña)
3. **Almacenamiento de archivos** (expedientes médicos: PDFs, imágenes)
4. **Tiempo real** (cola de espera, alertas)
5. **API REST auto-generada** (PostgREST para integraciones BaaS)
6. **Migraciones versionadas** (Supabase CLI)

La decisión afecta directamente la arquitectura, el costo operativo y el vendor lock-in a largo plazo.

---

## Decisión

**Usaremos Supabase** como BaaS completo para todos los proyectos del framework.

Los componentes utilizados:
- `Supabase Auth` → Autenticación + JWT
- `PostgreSQL 15` → Base de datos principal (accedida vía Dapper/Npgsql)
- `Supabase Realtime` → Suscripciones en tiempo real
- `Supabase Storage` → Almacenamiento de archivos
- `Row Level Security` → Aislamiento multi-tenant
- `Supabase CLI` → Migraciones versionadas

---

## Alternativas Evaluadas

### Opción A: Infraestructura propia en Azure / AWS (RECHAZADA)

**Componentes equivalentes que se necesitarían:**
- Azure SQL / AWS RDS (PostgreSQL) → Reemplaza Supabase DB
- Azure AD B2C / AWS Cognito → Reemplaza Supabase Auth
- Azure Blob Storage / AWS S3 → Reemplaza Supabase Storage
- Azure SignalR Service / AWS AppSync → Reemplaza Supabase Realtime
- Azure API Management → Reemplaza PostgREST

**Ventajas de infra propia:**
- Control total sobre la infraestructura
- Sin vendor lock-in de Supabase
- SLAs corporativos de Azure/AWS

**Desventajas que motivaron su rechazo:**
- 4-5 servicios separados que integrar vs. 1 plataforma cohesiva
- Costo de DevOps: configuración, monitoreo y mantenimiento de cada servicio
- Tiempo de setup de Fase 0: ~2 semanas vs. ~2 horas con Supabase
- La velocidad de desarrollo del MVP se reduce significativamente
- El equipo de desarrollo toma tiempo de la funcionalidad para gestionar infraestructura

### Opción B: Firebase (RECHAZADA)

**Ventajas de Firebase:**
- BaaS completo (similar a Supabase)
- Gran ecosistema de Google

**Desventajas que motivaron su rechazo:**
- NoSQL (Firestore) — incompatible con el modelo relacional multi-tenant y RLS
- Sin SQL nativo — las consultas complejas de expedientes médicos serían muy difíciles
- Sin RLS nativo de PostgreSQL — el aislamiento multi-tenant requeriría implementación manual en código
- La migración desde Firebase es costosa si el negocio crece

### Opción C: PocketBase (RECHAZADA)

BaaS open-source interesante pero sin ecosistema maduro para producción empresarial.

### Opción D: Supabase (ELEGIDA) ✅

**Ventajas:**
- PostgreSQL real — todo el poder de SQL relacional, funciones, extensiones
- RLS nativo — aislamiento multi-tenant a nivel de base de datos (no solo en código)
- Auth integrado con JWT configurables (claims custom: `clinica_id`, `perfil_id`)
- Storage S3-compatible con políticas de acceso por bucket
- Realtime sobre `LISTEN/NOTIFY` de PostgreSQL — no requiere infraestructura adicional
- CLI con migraciones versionadas — el schema es código que se versiona en Git
- Dashboard visual para el equipo no-técnico
- Entorno local idéntico a producción (Docker) — elimina "funciona en mi máquina"
- Open source — posibilidad de self-hosting si el proyecto lo requiere a futuro

**Desventajas aceptadas (vendor lock-in explícito):**
- Dependencia del roadmap de Supabase
- Los JWT de Supabase Auth tienen una estructura propia
- El SDK de Supabase tiene su propia API para Storage y Realtime
- Si se migra de Supabase, hay trabajo de refactoring en Auth y Storage

### Vendor Lock-in: Evaluación explícita

| Componente Supabase | Nivel de lock-in | Costo de migración |
|---|---|---|
| PostgreSQL | **Bajo** — SQL estándar, Dapper | 2-4 días (cambiar connection string + posibles diferencias SQL) |
| Supabase Auth | **Medio** — JWT configurable | 1-2 semanas (migrar a Identity Server u otro proveedor) |
| Supabase Storage | **Bajo** — S3-compatible | 1-3 días (cambiar bucket endpoint) |
| Supabase Realtime | **Medio** — protocolo propio | 1-2 semanas (migrar a SignalR puro o Ably) |
| RLS | **Bajo** — SQL estándar de PostgreSQL | 0 días (funciona en cualquier PostgreSQL) |

**Conclusión del vendor lock-in:** El lock-in es aceptable. La mayor parte del código (BLL, DAL, Entities, DTOs) es completamente agnóstico de Supabase.

---

## Consecuencias

### Positivas
- Fase 0 de un proyecto nuevo: ~2 horas en lugar de ~2 semanas
- El equipo de desarrollo se enfoca 100% en la funcionalidad de negocio
- El entorno local (`supabase start`) es idéntico al de producción
- Las migraciones son scripts SQL en Git — auditables en PR
- RLS garantiza el aislamiento multi-tenant a nivel de BD (dos capas de seguridad)

### Negativas / Trade-offs aceptados
- Dependencia de la disponibilidad y roadmap de Supabase
- El plan gratuito tiene límites (storage, bandwidth, funciones) — proyectos en producción necesitan plan Pro
- El SDK de Supabase JS se usa en el frontend para Realtime (dependencia adicional)

### Criterio de revisión
Si el proyecto supera los 100,000 usuarios activos o requiere SLAs corporativos que Supabase no puede garantizar, considerar self-hosting de Supabase (open source) o migración de Auth a Azure AD B2C y Storage a S3.

---

## Referencias

- [Supabase Documentation](https://supabase.com/docs)
- [Supabase vs. Firebase Comparison](https://supabase.com/alternatives/supabase-vs-firebase)
- [Self-hosting Supabase](https://supabase.com/docs/guides/self-hosting)
- ADR-001 — Dapper como ORM (accede a PostgreSQL de Supabase directamente)
- ADR-004 — RLS para multi-tenant (característica core de Supabase/PostgreSQL)
