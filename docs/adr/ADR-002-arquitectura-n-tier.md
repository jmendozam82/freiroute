# ADR-002: Arquitectura N-Tier con 8 Proyectos

## Estado
Aceptado

## Fecha
2026

## Contexto
El TMS necesita una arquitectura que permita separación de responsabilidades clara, testabilidad de la lógica de negocio de forma independiente a la base de datos, y que los agentes de IA puedan trabajar en capas sin interferir entre sí. También necesita soporte para dos superficies de presentación: una API REST (para integración y mobile) y una UI MVC (para el sistema web).

## Decisión
El sistema se organiza en **8 proyectos .NET** en una solución única (`Freiroute.sln`), siguiendo arquitectura N-Tier estricta.

## Estructura de Proyectos

```
Freiroute.sln
├── src/
│   ├── Freiroute.Entity/       # Entidades de dominio (POCOs puros)
│   ├── Freiroute.DTO/          # DTOs de entrada (Request) y salida (Response)
│   ├── Freiroute.DAL/          # Interfaces + Repositorios con Dapper
│   ├── Freiroute.BLL/          # Interfaces + Services + Validators (FluentValidation)
│   ├── Freiroute.IOC/          # Contenedor de inyección de dependencias
│   ├── Freiroute.Utility/      # Helpers, constantes, ApiResponse<T>, excepciones
│   ├── Freiroute.API/          # Web API: Controllers, Middleware, JWT, Swagger
│   └── Freiroute.Aplicacion/   # MVC: Areas, Controllers, Views, wwwroot
├── tests/
│   ├── Freiroute.BLL.Tests/    # Tests unitarios de BLL (≥80% cobertura)
│   └── Freiroute.API.Tests/    # Tests de integración de API (≥60% cobertura)
├── supabase/
│   └── migrations/             # Migraciones SQL versionadas con Supabase CLI
└── docs/
    ├── adr/                    # Architecture Decision Records
    ├── specs/                  # Spec por Historia de Usuario
    └── framework/              # Backlog, roadmap, design system
```

## Flujo de Datos (inamovible)

```
Vista Razor
    ↓
Controller MVC (Freiroute.Aplicacion)
    ↓  [HttpClient interno]
API Controller (Freiroute.API)
    ↓  [Inyección de dependencias]
BLL Service (Freiroute.BLL)
    ↓  [Inyección de dependencias]
DAL Repository (Freiroute.DAL)
    ↓  [Dapper + NpgsqlConnection]
Supabase / PostgreSQL 15
```

## Reglas de Dependencia entre Proyectos

| Proyecto | Puede referenciar | No puede referenciar |
|---|---|---|
| Entity | Ninguno | Todos los demás |
| DTO | Entity | DAL, BLL, API, Aplicacion |
| DAL | Entity, Utility | BLL, API, Aplicacion, DTO |
| BLL | Entity, DTO, DAL (interfaces), Utility | API, Aplicacion |
| IOC | Entity, DTO, DAL, BLL, Utility | API, Aplicacion |
| Utility | Ninguno | Todos los demás |
| API | BLL (interfaces), DTO, Utility, IOC | Aplicacion, DAL directo |
| Aplicacion | BLL (interfaces), DTO, Utility, IOC | DAL directo, Entity directo |

## Alternativas Consideradas

1. **Clean Architecture (Onion)** — Descartada por complejidad excesiva para el equipo actual. N-Tier es más directo y los agentes de IA la implementan con menos ambigüedad.
2. **Proyecto monolítico único** — Descartada porque imposibilita la testabilidad independiente de BLL y mezcla responsabilidades que los agentes deben manejar por separado.
3. **Microservicios** — Descartada para la fase inicial. El sistema puede evolucionar a microservicios en v3.0 una vez que los límites de dominio estén bien definidos.

## Consecuencias

**Positivas:**
- Cada agente de IA trabaja en su capa sin riesgo de romper otras
- BLL es 100% testeable sin base de datos (Moq del repositorio)
- La API y la UI web pueden evolucionar independientemente
- El IOC centraliza toda la configuración de DI

**Negativas / Trade-offs:**
- Más archivos y proyectos que un monolito simple
- Requiere disciplina para no saltarse capas (controlada por AGENTS.md)

## Módulos Afectados
Todos los módulos del sistema (EP-01 al EP-20).
