# ADR-002: Arquitectura N-Tier con 8 proyectos independientes

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
El sistema SaaS Freiroute TMS requiere una estructura de código que permita: separación clara de responsabilidades, testabilidad unitaria independiente por capa, evolución evolutiva de cada módulo sin afectar otros, y escalabilidad del equipo de desarrollo donde diferentes agentes pueden trabajar en capas distintas simultáneamente. Las alternativas tradicionales incluyen soluciones monolíticas o arquitecturas CQRS/Event-Driven más complejas.

## Decisión
El sistema ORGANIZARÁ el código en exactamente **8 proyectos .NET** independientes:

```
src/Freiroute.Entity/      → Entidades de dominio (models POCO)
src/Freiroute.DTO/         → Data Transfer Objects (Request + Response)
src/Freiroute.DAL/         → Data Access Layer (Dapper repositorios + interfaces)
src/Freiroute.BLL/         → Business Logic Layer (Services + FluentValidators)
src/Freiroute.IOC/         → Inversion of Control (registro DI centralizado)
src/Freiroute.Utility/     → Helpers, extensiones, ApiResponse\<T\>, constantes
src/Freiroute.API/         → Web API (.NET 8 REST endpoints)
src/Freiroute.Aplicacion/  → MVC (.NET 8 Razor Views & Areas)
```

Reglas fundamentales:
- **Ninguna capa PODRÁ saltarse otra.** El Controller no llama al DAL. La Vista no llama al BLL.
- **El flujo de datos SIEMPRE SERÁ:** Vista → Controller MVC → API Controller → BLL Service → DAL Repository → Supabase/PostgreSQL.
- Cuando se genere un módulo nuevo, la creación seguirá este orden estricto: Entity → DTO → DAL Interface → DAL Repository → BLL Interface → BLL Service → API Controller → Vistas Razor.

## Alternativas Consideradas
1. **Monolito único (single assembly)** — Descartado porque impide testeo unitario aislado, complica dependencias circulares y hace imposible que múltiples agentes trabajen en paralelo sin conflictos de merge.
2. **Clean Architecture / Onion** — Demasiada complejidad adicional para MVP. No requiere use-cases intermedios ni ports/interfaces externos hasta fase avanzada.
3. **CQRS completo** — Overkill para CRUD standard del MVP. Se reserva para módulos avanzados (Track & Trace, Analytics BI) cuando sea estrictamente necesario.

## Consecuencias
**Positivas:**
- Cada capa es compilable y testeable de forma independiente
- Cambio en la capa de acceso a datos no afecta las vistas MVC
- Testing unitario focalizado en BLL sin infraestructura
- Fácil asignación de agentes IA a capas específicas

**Negativas / Trade-offs:**
- Más archivos de configuración (.csproj) y referencias cruzadas
- Requiere disciplina estricta de no violar jerarquía de capas
- Incremento inicial de tiempo de build (mitigable con builds incrementales)

## Módulos Afectados
Todos. Este ADR define la organización estructural permanente del proyecto.

---
