# AGENTS.md — La Constitución del Proyecto SaaS

> **ARCHIVO DE REGLAS A NIVEL DE PROYECTO**
> Todo agente de IA (Claude, Cursor, Copilot, OpenCode) debe leer este archivo ANTES de escribir una sola línea de código.
> Contiene decisiones duraderas del equipo escritas como declaraciones EARS.
> **Hacer commit de este archivo ANTES del primer spec.**

---

## Identidad del Stack

El sistema USARÁ **ASP.NET Core MVC (.NET 8)** como framework de presentación.
El sistema USARÁ **ASP.NET Core Web API (.NET 8)** como backend.
El sistema USARÁ **Supabase (PostgreSQL 15)** como base de datos y BaaS.
El sistema USARÁ **Dapper** como ORM para consultas SQL directas.
El sistema USARÁ **Supabase Auth + JWT** para autenticación.
El sistema USARÁ **Row Level Security (RLS)** de PostgreSQL para aislamiento multi-tenant.
El sistema USARÁ **FluentValidation** para validación del lado servidor.
El sistema USARÁ **jQuery Validate** para validación del lado cliente.
El sistema USARÁ **Bootstrap 5.3** como UI Kit.
El sistema USARÁ **Supabase CLI** para todas las migraciones de base de datos.
El sistema USARÁ **GitHub Actions** como pipeline de CI/CD.

---

## Reglas de Arquitectura (EARS)

### Capa N-Tier

1. **El sistema ORGANIZARÁ** el código en exactamente 8 proyectos: `Aplicacion`, `API`, `BLL`, `DAL`, `Entity`, `DTO`, `IOC`, `Utility`.
2. **Ninguna capa PODRÁ** saltarse otra. El Controller no llama al DAL. La Vista no llama al BLL.
3. **El flujo de datos SIEMPRE SERÁ**: Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL.
4. **Cuando** se genere un módulo nuevo, **el sistema REQUERIRÁ** crear: Entity → DTO → DAL Interface → DAL Repository → BLL Interface → BLL Service → API Controller → Vistas MVC.

### Multi-Tenant

5. **Toda tabla de negocio DEBERÁ** contener el campo `clinica_id UUID NOT NULL` como discriminador de tenant.
6. **Toda consulta SQL DEBERÁ** filtrar por `clinica_id`.
7. **Row Level Security DEBERÁ** estar habilitado en cada tabla de negocio.
8. **El JWT DEBERÁ** contener: `user_id`, `clinica_id`, `perfil_id`, `permisos[]`.

### Base de Datos

9. **Toda migración PASARÁ** por Supabase CLI (`supabase migration new`). Prohibido ejecutar SQL ad-hoc en producción.
10. **Todos los IDs SERÁN** de tipo `UUID` generados con `gen_random_uuid()`.
11. **Toda tabla de negocio INCLUIRÁ** los campos: `id`, `clinica_id`, `activo`, `fecha_creacion`, `fecha_modificacion`.
12. **Los registros NUNCA SE ELIMINARÁN**. Solo se desactivarán (`activo = false`).
13. **Los comentarios de la BD SERÁN** en español (idioma del negocio).

### Código

14. **Las clases C# USARÁN** PascalCase. Las variables camelCase. Las tablas SQL snake_case.
15. **Los métodos asíncronos TERMINARÁN** en `Async`. Ejemplo: `GetAllAsync`, `CreateAsync`.
16. **Todas las respuestas de la API USARÁN** el wrapper `ApiResponse<T>`.
17. **Los DTOs SERÁN SIEMPRE** diferentes de las Entities. Nunca retornar una Entity directamente.
18. **Las interfaces PRECEDRÁN** de la letra `I`. Ejemplo: `IPacienteService`.

### Permisos

19. **El sistema MANEJARÁ** exactamente 3 tipos de permiso: `READ`, `CREATE`, `UPDATE`. No existe `DELETE`.
20. **Todo endpoint de API DEBERÁ** verificar permisos con `[RequirePermission(modulo, tipo)]`.
21. **El perfil ADMIN TENDRÁ** acceso completo a todos los módulos de su tenant (no del sistema global).

### Testing

22. **Todo módulo nuevo REQUERIRÁ** tests unitarios en `[Proyecto].BLL.Tests`.
23. **Todo endpoint crítico REQUERIRÁ** tests de integración en `[Proyecto].API.Tests`.
24. **El patrón TDD SERÁ** aplicado: escribir el test que falla → implementar → pasar el test.

### Seguridad

25. **Los archivos de Supabase Storage ESTARÁN** en buckets privados. Las URLs usarán tokens temporales.
26. **Las claves secretas NUNCA SE COMMITEARÁN** al repositorio. Usar variables de entorno o Secrets Manager.
27. **HTTPS SERÁ** obligatorio en todos los entornos (desarrollo incluido con certificado local).

### Agentes de IA

28. **Cada módulo TENDRÁ** un archivo `spec.md` antes de que el agente escriba código.
29. **El flujo del agente SIEMPRE SERÁ**: spec → plan → tasks → implement. Nunca saltar de spec a código.
30. **El agente LEERÁ** este AGENTS.md al inicio de cada sesión antes de cualquier acción.

---

## Convención de Idiomas

| Elemento | Idioma |
|---|---|
| Interfaz de usuario | Español |
| Nombres de tablas y columnas SQL | Español (snake_case) |
| Comentarios de BD | Español |
| Clases, métodos, variables C# | Inglés |
| Comentarios de código | Inglés |
| Documentación técnica (docs/) | Español |

---

## Glosario Mínimo del Dominio

> Cada proyecto deberá extender este glosario con sus propios términos de negocio.

| Término técnico | Descripción |
|---|---|
| `tenant` | Organización/empresa que usa el sistema en modo SaaS |
| `clinica_id` / `tenant_id` | Discriminador universal de tenant en todas las tablas |
| `activo` | Flag booleano que reemplaza el DELETE físico |
| `RLS` | Row Level Security — aislamiento de datos a nivel BD |
| `BLL` | Business Logic Layer — capa de reglas de negocio |
| `DAL` | Data Access Layer — capa de acceso a datos |
| `DTO` | Data Transfer Object — objeto de transporte entre capas |
| `IOC` | Inversión de Control — contenedor de inyección de dependencias |
| `JWT` | JSON Web Token — mecanismo de autenticación sin estado |
| `EARS` | Easy Approach to Requirements Syntax — formato de reglas de negocio |

---

*AGENTS.md — Plantilla base para proyectos SaaS con Supabase*
*Versión: 1.0.0 | Basada en las mejores prácticas del proyecto Vittal (2026)*
*Este archivo se versiona en Git y es la fuente de verdad para todos los agentes de IA.*
