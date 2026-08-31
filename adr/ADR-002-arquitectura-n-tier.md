# ADR-002: Arquitectura N-Tier de 8 Proyectos vs. Alternativas

| Campo | Valor |
|---|---|
| **ID** | ADR-002 |
| **Título** | Arquitectura N-Tier de 8 proyectos en lugar de Arquitectura Limpia o Monolito Modular |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-15 |
| **Decidido por** | Arquitecto de software + Tech Lead |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El equipo evaluó la estructura base del código para un sistema SaaS médico con múltiples módulos que crecería durante varios Sprints. Los criterios de decisión fueron:

1. **Claridad para agentes de IA**: el agente debe saber exactamente dónde crear cada archivo
2. **Onboarding rápido**: un desarrollador nuevo debe ubicarse en < 30 minutos
3. **Separación de responsabilidades**: ninguna capa puede saltarse otra
4. **Escalabilidad de equipos**: diferentes agentes/desarrolladores trabajando en capas distintas sin conflictos

---

## Decisión

**Usaremos N-Tier con exactamente 8 proyectos** separados dentro de una misma solución .NET:

```
[Proyecto].Aplicacion   ← Presentación (MVC + Razor)
[Proyecto].API          ← REST Endpoints (Web API + JWT)
[Proyecto].BLL          ← Reglas de negocio (Services + Validators)
[Proyecto].DAL          ← Acceso a datos (Repositories + Dapper)
[Proyecto].Entity       ← Modelos de dominio
[Proyecto].DTO          ← Objetos de transferencia
[Proyecto].IOC          ← Inyección de dependencias
[Proyecto].Utility      ← Helpers y extensiones compartidas
```

---

## Alternativas Evaluadas

### Opción A: Arquitectura Limpia / Clean Architecture (RECHAZADA para este contexto)

**Ventajas de Clean Architecture:**
- Dominio completamente aislado del framework
- Ideal para dominios complejos con lógica de negocio muy rica
- Los use cases son unidades de trabajo explícitas

**Desventajas que motivaron su rechazo:**
- Para un equipo con agentes de IA, la abstracción adicional (use cases, ports, adapters) aumenta la ambigüedad sobre dónde crear cada archivo
- En dominios CRUD-intensivos (catálogos, citas), el overhead de Clean Architecture es injustificado
- Requiere mayor madurez del equipo para no crear anti-patrones (use cases que son simples pass-through)
- Los 4-5 proyectos extra (Domain, Application, Infrastructure, Presentation) complican el grafo de dependencias sin beneficio claro en este tipo de dominio

### Opción B: Monolito Modular (RECHAZADA)

**Ventajas:**
- Un solo proyecto — menos configuración de referencias
- Deploy más simple

**Desventajas:**
- Sin enforcement físico de las capas — un desarrollador puede romper la separación accidentalmente
- No permite asignar un agente de IA a una capa específica (todo está mezclado)
- Dificulta los tests unitarios (todo en el mismo namespace)

### Opción C: N-Tier de 8 Proyectos (ELEGIDA) ✅

**Ventajas:**
- El compilador C# hace cumplir la separación de capas (referencias de proyecto)
- La ubicación de cada archivo es determinista — cero ambigüedad para agentes de IA
- Cada agente trabaja en una capa sin conflictos con los demás
- Los tests de BLL no tienen dependencia física del DAL
- Escala linealmente: agregar un módulo = misma estructura en cada proyecto

**Desventajas aceptadas:**
- Más archivos de proyecto (`.csproj`) a mantener
- Las referencias entre proyectos deben ser explícitas en el setup inicial
- Overhead de solución: 8 proyectos vs. 1 para funcionalidad equivalente en sistemas pequeños

---

## Regla de Oro del N-Tier (consecuencia arquitectónica)

```
Aplicacion → API → BLL → DAL → Supabase/PostgreSQL

❌ PROHIBIDO:
  - API llama a DAL directamente
  - Aplicacion llama a BLL directamente
  - Vista contiene lógica de negocio
  - Entity expuesta directamente por la API (usar siempre DTOs)
```

Esta regla es verificada por el compilador a través de las referencias de proyecto.

---

## Consecuencias

### Positivas
- Los agentes de IA tienen cero ambigüedad sobre dónde crear cada artefacto
- La separación es física (no solo por convención) — el compilador la enforza
- Los tests unitarios de BLL no requieren infraestructura (mockan interfaces del DAL)
- Permite asignación de roles de agente por capa: `@Arquitecto`, `@IngenieroDatos`, `@BackendDev`, `@FrontendDev`

### Negativas / Trade-offs aceptados
- Setup inicial más largo (8 proyectos + referencias)
- Para proyectos muy pequeños (< 5 módulos), puede sentirse excesivo
- El `DependencyInjection.cs` centralizado debe ser actualizado con cada nuevo servicio

### Criterio de revisión
Si el proyecto evoluciona hacia un dominio con lógica de negocio muy compleja (cálculos financieros, workflows multi-paso, motores de reglas), considerar migrar gradualmente hacia Clean Architecture extrayendo el BLL como capa de Application con use cases explícitos.

---

## Referencias

- [Microsoft — N-Tier Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/n-tier)
- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- ADR-001 — Dapper como ORM (relacionado: necesidad de DAL separado)
