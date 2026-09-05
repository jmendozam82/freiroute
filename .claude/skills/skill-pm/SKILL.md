---
description: Guía de Sprint Planning y coordinación de agentes para Freiroute TMS. Úsalo cuando necesites planificar un sprint, crear un spec.md de una Historia de Usuario, coordinar agentes de capa en el orden correcto, o gestionar el flujo de trabajo SCRUM del proyecto.
---

# Skill: PM — Orquestador del Proyecto Freiroute TMS

## Flujo de Trabajo Obligatorio

```
1. Leer AGENTS.md completo
2. Leer docs/framework/requerimientos.md y backlog
3. Verificar estado del sprint actual
4. Ejecutar: spec → plan → tasks → implement → test
```

## Template de Spec para Nueva Historia de Usuario

Guardar en `docs/specs/HU-XXX-nombre.md`:

```markdown
# Spec: HU-XXX — [Nombre de la Historia]

## Historia de Usuario
Como [rol], quiero [acción], para [valor de negocio].

## Criterios de Aceptación
- [ ] CA-01: [criterio específico y verificable]
- [ ] CA-02: ...

## Módulo y Tabla Principal
- Módulo: [nombre del módulo TMS]
- Tabla BD: [nombre_tabla]
- Épica: EP-XX

## Entidades Involucradas
- Entity: [NombreEntity.cs]
- RequestDto: [NombreRequestDto.cs]
- ResponseDto: [NombreResponseDto.cs]

## Endpoints API
- GET  /api/[modulo]
- GET  /api/[modulo]/{id}
- POST /api/[modulo]
- PUT  /api/[modulo]/{id}
- PATCH /api/[modulo]/{id}/deactivate

## Reglas de Negocio TMS
1. [Regla específica del dominio de transporte]

## Tests Requeridos
- BLL: [lista de casos de test críticos]
- API: [endpoints a cubrir]

## Notas Técnicas
[Consideraciones de arquitectura, integraciones, estados]
```

## Orden de Coordinación de Capas

| Paso | Agente | Entregable |
|---|---|---|
| 1 | **@arquitecto** | Entity, DTOs, Interfaces, ADR (si aplica) |
| 2 | **@ingenierodatos** | Migración SQL + RLS + Trigger |
| 3 | **@backenddev** | BLL Service + FluentValidator + API Controller |
| 4 | **@qa** | Tests unitarios BLL + Tests integración API |
| 5 | **@frontenddev** | Vistas Razor con Design System Freiroute |
| 6 | **@qa** | Validación final + criterios de aceptación |

## Checklist de Aprobación de PR

```markdown
### Pre-merge Checklist
- [ ] Branch: feature/HU-XXX-nombre-hu (desde develop)
- [ ] spec.md existe en docs/specs/
- [ ] dotnet build sin warnings
- [ ] dotnet test pasa todos los tests
- [ ] BLL cobertura ≥80%
- [ ] API cobertura ≥60%
- [ ] Migración SQL en supabase/migrations/
- [ ] RLS habilitado en tabla nueva
- [ ] Sin secrets en código
- [ ] Logs en inglés
- [ ] UI en español
```

## Gestión del Backlog

El backlog de Freiroute TMS contiene módulos agrupados en épicas:

- **EP-01: Core TMS** — Embarques, Órdenes, Carriers, Conductores
- **EP-02: Tracking** — GPS, Geofences, Track & Trace
- **EP-03: Finanzas** — Tarifas, Facturas, Costos
- **EP-04: Reportes** — OTD, KPIs, Dashboards
- **EP-05: Portal Cliente** — Self-service, POD digital
- **EP-06: Admin SaaS** — Gestión multi-tenant, onboarding

## Estado del Sprint Actual

Ver `docs/sprints/` para el estado actualizado del sprint en curso.
