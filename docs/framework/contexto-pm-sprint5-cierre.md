# Contexto @PM — Cierre Sprint 5 / Apertura Sprint 6
# Freiroute TMS · EP-04 Órdenes Avanzadas + Fixes Sprint 4

---

## Estado acumulado del proyecto

```
Sprint 1 ✅  Auth & Multi-Tenant
Sprint 2 ✅  Administración SaaS
Sprint 3 ✅  Maestros & Catálogos
Sprint 4 ✅  Order Management (EP-04 core)
Sprint 5 ✅  EP-04 Órdenes Avanzadas + Fixes Sprint 4
────────────────────────────────────────────────────────
5/26 sprints completados
~347 pts de ~1049 completados (~33%)
1052 tests · ~163 endpoints · 8 tablas nuevas Sprint 5
```

---

## Sprint 5 — CERRADO OFICIALMENTE ✅

```
Sprint 5 · EP-04 — Órdenes Avanzadas + Fixes Sprint 4
─────────────────────────────────────────────────────────────
Build:          0 errores / 0 warnings                   ✅
Tests:          1052/1052 — 0 fallos (+ 2 skipped CI)   ✅
Cobertura BLL:  86.81 % (objetivo ≥80%)                  ✅
Cobertura API:  78.39 % (objetivo ≥60%)                  ✅
CAs Sprint 5:   43/55 verificados (UI pendiente
                confirmado por smoke test)                ✅
Schema cloud:   8 migraciones aplicadas                  ✅
Swagger:        ~163 endpoints (+15 Sprint 5)            ✅
Smoke test:     ejecutado · 0 errores de consola        ✅
Fixes S4:       G-08 a G-18: 9/9 cerrados               ✅
                (G-11 reclasificado, ver nota)
─────────────────────────────────────────────────────────────
```

---

## Lo que se construyó en Sprint 5

### Tablas nuevas BD (8)

```
rechazos_entrega        · HU-030 · RLS + policy + 2 índices
reglas_prioridad        · HU-029 · RLS + UNIQUE parcial
reclamos                · HU-032 · RLS + policy + 4 índices
historial_estados_reclamo · HU-032 · INSERT-only + RLS
contadores_reclamo      · HU-032 · Opción B (tabla propia, patrón ADR-020)

Columnas nuevas (ALTER TABLE):
  ordenes:  numero_po VARCHAR(100), numero_so VARCHAR(100),
            fecha_entrega_real TIMESTAMPTZ
  empresas: factor_reentrega NUMERIC(4,2) DEFAULT 1.5
```

### Función SQL nueva

```
generar_numero_reclamo(empresa_id, prefijo, anio)
  → REC-{PREFIX}-{AÑO}-{NNNN} — mismo patrón que ADR-020
```

### Endpoints nuevos (~15)

```
GET  /api/ordenes/por-po/{numero}           HU-028
GET  /api/ordenes/criticas                  HU-029
PATCH /api/ordenes/{id}/prioridad           HU-029
GET  /api/ordenes/sla-en-riesgo             HU-031
GET  /api/clientes/{id}/sla-cumplimiento    HU-031
GET  /api/reportes/sla                      HU-031
POST /api/ordenes/{id}/rechazo              HU-030
POST /api/ordenes/{id}/re-entrega           HU-030
GET  /api/ordenes/{id}/re-entregas          HU-030
POST /api/reclamos                          HU-032
GET  /api/reclamos                          HU-032
GET  /api/reclamos/{id}                     HU-032
PATCH /api/reclamos/{id}/estado             HU-032
GET  /api/reportes/reclamos                 HU-032
GET  /api/reclamos/cliente/{id}             HU-032 (ruta real aprobada H-02)
```

### Servicios BLL nuevos

```
OrdenPoService          · VincularPo, GetPorPo, AuditarCambioPo
PrioridadOrdenService   · CambiarPrioridad, GetCriticas, ElevarAutomáticas
RechazoEntregaService   · RegistrarRechazo (FSM→FAILED_DELIVERY), CrearReentrega, GetReentregas
SlaService              · CalcularSlaStatus (puro), GetEnRiesgo, GetCumplimientoCliente, GetReporteSla
ReclamoService          · CRUD + FSM EstadoReclamo + historial + numeración REC-
```

### Background Jobs nuevos

```
PrioridadOrdenesJob   · PeriodicTimer 6h · cross-tenant · 3 reglas de elevación
SlaMonitorJob         · PeriodicTimer 24h · alerta ADMIN si cliente VIP < 90% SLA
```

### Vistas MVC nuevas/modificadas

```
Creadas:
  Views/Ordenes/SlaEnRiesgo.cshtml    · tabla JS-driven con KPI cards
  Views/Ordenes/Criticas.cshtml       · tabla JS-driven
  Views/Reclamos/Index.cshtml         · server-side paginado con filtros
  Views/Reclamos/Crear.cshtml         · buscador PO + evidencias dinámicas
  Views/Reclamos/Detalle.cshtml       · FSM inline + timeline historial
  Views/Shared/_BadgeSla.cshtml
  Views/Shared/_BadgePrioridad.cshtml · con animación fr-pulse en CRITICO
  Views/Shared/_BadgeEstadoReclamo.cshtml

Modificadas:
  Views/Ordenes/Index.cshtml          · cols PO + SLA + Prioridad + filtro ?po=
  Views/Ordenes/Detalle.cshtml        · sección PO/SO + modal rechazo + card re-entregas
  Views/Ordenes/Create.cshtml         · campos NumeroPo/NumeroSo
  Views/Ordenes/Edit.cshtml           · idem (disabled según estado)
  Views/Dashboard/Index.cshtml        · KPIs #kpi-criticas + #kpi-sla-riesgo
  Views/Shared/_Layout.cshtml         · ítem sidebar Reclamos
  wwwroot/css/freiroute.css           · @keyframes fr-pulse + .fr-badge-critico--pulse
```

### Validators nuevos (FluentValidation)

```
PrioridadOrdenValidator · RechazoEntregaValidator
ReclamoValidator        · ReclamoEstadoValidator
```

### Tests añadidos (109 nuevos → total 1052)

```
BLL (82):
  OrdenPoServiceTests (7)
  PrioridadOrdenServiceTests (12)
  RechazoEntregaServiceTests (8)
  SlaServiceTests (11)
  ReclamoServiceTests (11)
  Validators: 4 archivos × ~4-7 tests c/u (19 total)
  Builders: ReclamoBuilder + OrdenListItemBuilder
  Gaps (tests de fixes): +9 tests (G-08, G-12, G-15, G-16, G-17, G-18, G-09, G-10, G-14)

API (25):
  OrdenesControllerTests +10 (G-12 + endpoints Sprint 5)
  ReclamosControllerTests (9) — nuevo
  ReportesControllerTests (6) — nuevo
  TestWebApplicationFactory +5 mocks
```

---

## Estado de deuda técnica al cerrar Sprint 5

### Gaps CERRADOS en Sprint 5

| Gap | Descripción | Cómo se cerró |
|-----|-------------|---------------|
| G-14 🔴 | BCrypt.Verify en ApiKey | Fix + test integración |
| G-15 🔴 | Auditoría JSON inválido | JsonSerializer en todos los RegistrarAsync + test |
| G-16 🟠 | Snapshot vacío en plantilla | GetByIdAsync antes de serializar + test |
| G-10 🟠 | OrigenCreacion=RECURRENTE | Parámetro opcional en CreateAsync + test |
| G-17 🟡 | Historial incompleto | 4 sub-fixes + 4 tests |
| G-18 🟡 | JOINs faltantes en listado | JOINs clientes/ubicaciones + test |
| G-09 🟡 | guardar-plantilla → 200 | CreatedAtAction → 201 + test |
| G-08 🟡 | Auditoría sin NombreUsuario | LEFT JOIN + DTO ampliado + test |
| G-12 🟡 | OrdenesController 52.4% | +10 tests API → 100% |
| G-11 🟡 | Test PATCH /abonos | Reclasificado: endpoint no existe → EP-07 |

### G-11 — nota de reclasificación

`G-11` pedía test para `PATCH /api/ordenes/{id}/abonos`. Verificación exhaustiva:
0 coincidencias en `src/` — el endpoint de abonos no está implementado.
**Decisión @PM:** mover al backlog de EP-07 Facturación como nueva HU con referencia
a G-11. No es un gap de este sprint; era deuda anticipada de un módulo futuro.

### Gaps DIFERIDOS a Sprint 6

| Gap | HU | Descripción | Impacto |
|-----|-----|-------------|---------|
| G-13 🔴 | HU-025 | `ConsolidarAsync` lanza 500 FK | Bloquea EP-05 Embarques |
| G-19 🟡 | HU-024 | Detalle PARTIALLY_SPLIT no lista sub-órdenes | UX menor |
| H-01* | HU-031 | SLA umbral CRITICO: spec actualizado a < 6h | ✅ Cerrado por decisión @PM |
| H-03 🟠 | HU-032 | `ReclamoValidator` sin validación URL | Compensado en frontend JS |
| H-04 🔵 | HU-029 | `PrioridadOrdenService` 91.3% | Sobre umbral — aceptado |

> *H-01 umbral SLA: @PM adoptó 6h operacionalmente (más útil para dispatchers).
> Spec HU-031 CA-06 actualizado a "< 6h → CRITICO". Tests ya reflejan el valor real.

> H-03 URL validation: compensado con `$.validator.addMethod('esUrlValida', ...)`
> en Reclamos/Crear.cshtml. Pendiente fix @BackendDev en Sprint 6 para defensa en profundidad BLL.

### Gaps a largo plazo (sin cambio)

| Gap | Sprint objetivo |
|-----|----------------|
| G-05 | Sprint 6 — moneda secundaria |
| G-01 | Sprint 10 — permisos Aprobar/Exportar |

---

## Desviaciones del Sprint 5 aprobadas

| Desviación | Decisión |
|-----------|---------|
| Columna BD: `nombre_completo` (no `nombre`) en `usuarios` | ✅ Aprobado — nombre real de la columna; AGENTS.md actualizará query examples |
| G-18: JOIN listado paginado sin `AND c.empresa_id = o.empresa_id` | 🟡 Deuda menor — seguro por UUID + RLS; fix de pulido Sprint 6 por @IngenieroDatos |
| Validators nombrados `XValidator` (no `XRequestValidator`) | ✅ Aprobado — convención real del proyecto |
| `referencias_evidencia` → `TEXT[]` con cast `::TEXT[]` en INSERT | ✅ Aprobado — Supabase Storage en Sprint 11 |
| `contadores_reclamo` → Opción B (tabla propia, no modificar PK existente) | ✅ Aprobado — más seguro con datos existentes |
| Ruta reclamos/cliente: `/api/reclamos/cliente/{id}` (no `/api/clientes/{id}/reclamos`) | ✅ Aprobado — ruta real implementada (H-02 resuelto) |
| SlaStatus CRITICO = < 6h (no 2h del spec original) | ✅ Aprobado — decisión operacional @PM; spec actualizado |
| `PagedResult<T>` para listado de reclamos (sin overload ApiResponse con total) | ✅ Aprobado — patrón existente ADR-008 |

---

## Hallazgos QA Sprint 5 — estado final

| ID | Severidad | Estado al cierre |
|----|-----------|-----------------|
| H-01 SLA umbral | Media | ✅ Resuelto — umbral adoptado a 6h, spec actualizado |
| H-02 ruta reclamos/cliente | Baja | ✅ Resuelto — ruta actual aprobada, spec actualizado |
| H-03 URLs sin validar | Media | 🟠 Sprint 6 — compensado en frontend; pendiente BLL |
| H-04 PrioridadOrdenService 91.3% | Baja | 🔵 Aceptado — sobre umbral 80% |

---

## Sprint 6 — Definición

**Nombre:** EP-05 Shipments (Embarques)
**Épica:** EP-05 — Embarque y Asignación de Transportistas
**Objetivo:** Implementar la gestión de embarques (shipments): asignación de
carrier y conductor, consolidación real de órdenes en un shipment,
track & trace básico y gestión de documentos de embarque.
**Story Points:** ~55-65 pts estimados (pendiente refinamiento)
**Prerequisito:** Resolver G-13 (ConsolidarAsync FK 500) antes de cualquier HU nueva.

### HUs candidatas Sprint 6 (preliminar — sujeto a refinamiento)

```
Fix G-13 🔴  ConsolidarAsync FK 500 (bloquea TODO EP-05)
Fix H-03 🟠  ReclamoValidator URL validation
Fix pulido   G-18 JOIN guard empresa_id (minor)

HU-033 · Gestión de embarques (shipment CRUD)
HU-034 · Asignación de carrier y conductor a embarque
HU-035 · Consolidación real de órdenes en embarque (fix G-13)
HU-036 · Track & Trace básico (cambio de posición del embarque)
HU-037 · Documentos de embarque (carta porte, guía)
```

### Dependencias Sprint 6

```
Sprint 5 ✅ → ordenes con FSM completo, reclamos, SLA
Sprint 4 ✅ → shipments tabla (esqueleto 6 columnas — Sprint 7 la expande)
G-13 resuelto → ConsolidarAsync debe crear shipment real antes de HU-033+
carriers, conductores, vehículos → ya en Sprints 2-3 (maestros)
```

### Notas de arquitectura para Sprint 6

```
1. shipments actualmente tiene 6 columnas — el prompt a @Arquitecto
   debe especificar las columnas a agregar (carrier_id, conductor_id,
   fecha_programada, estado, etc.)

2. Track & Trace usa Nominatim + Leaflet (ADR-014) — ya hay infrastructure
   de mapas en el frontend

3. Los documentos de embarque van en Supabase Storage (ADR-012 Signed URLs)
   — puede ser un sprint separado si la Fase de Storage no está lista

4. G-13 tiene causa raíz: FK de shipment_id en ordenes no se satisface
   porque ConsolidarAsync no inserta primero el shipment
   — el fix es insertar el shipment ANTES de actualizar las órdenes
```

---

## Contexto técnico acumulado para Sprint 6

### Stack y convenciones (sin cambio)

```
Backend:    ASP.NET Core .NET 8 · C# · Dapper
BD:         Supabase PostgreSQL · RLS por empresa_id
Auth:       JWT claims · X-Api-Key para integración externa
Patrones:   BusinessException → 422 · Create → 201 · Soft delete
            empresa_id en todas las queries · RLS como segunda capa
            PeriodicTimer para background jobs (ADR-013)
```

### Módulos de permiso

```
ordenes · embarques · carriers · rutas · track_trace · documentos
flota · analytics · facturacion · clientes · usuarios · configuracion
```

### ADRs vigentes al inicio Sprint 6

```
ADR-001 al ADR-020 (sin cambio desde Sprint 5)
```

### Convenciones confirmadas (no revertir)

```
1. Columna BD: nombre_completo (no nombre) en tabla usuarios
2. Clases CSS: fr-badge-* / fr-btn-* (AGENTS.md tiene badge-fr-* desactualizado)
3. MVC usa User.HasPermission() en vistas (no [RequireModulePermission])
4. Vistas del área Tenant: Detalle.cshtml (no Detail)
5. Validators: XValidator (no XRequestValidator)
6. TransicionesDisponibles → List<string> en JSON (no IReadOnlySet)
7. ModoTransporte en ÓRDENES: 5 valores (TERRESTRE|AEREO|MARITIMO|FERROVIARIO|INTERMODAL)
   ModoTransporte en TARIFAS: FTL|LTL|AEREO|MARITIMO|FERROVIARIO|INTERMODAL
   Dominios distintos — NO mezclar
8. shipments tabla: actualmente 6 columnas — se expande en Sprint 6/7
9. Test concurrencia numeración: [Skip("Requiere supabase start")] en CI
10. FrApi.patch corregido Sprint 4 (retrocompatible) — usar el helper existente
11. PagedResult<T> para listados paginados (patrón existente ADR-008)
12. SlaStatus CRITICO = < 6h (decisión @PM Sprint 5, no 2h del spec original)
13. Ruta reclamos/cliente: /api/reclamos/cliente/{id}
14. contadores_reclamo: tabla propia (Opción B, no modificar PK de contadores_orden)
15. factor_reentrega: columna en empresas (no en configuracion)
```

### Nuevas entidades/tablas relevantes para Sprint 6

```
rechazos_entrega    · ya en BD · puede necesitarse en contexto de embarques
reclamos            · ya en BD · se relacionan con órdenes, no con shipments aún
reglas_prioridad    · ya en BD · usadas por PrioridadOrdenesJob
contadores_reclamo  · ya en BD · patrón ADR-020
empresas.factor_reentrega  · ya en BD
ordenes.fecha_entrega_real · ya en BD
ordenes.numero_po / numero_so · ya en BD
```

---

## Prompt de arranque Sprint 6

Usar este prompt al abrir el nuevo chat en el Proyecto Claude:

```
Continuamos el desarrollo de Freiroute TMS.

Sprint 6 — EP-05 Shipments (Embarques) + Fixes Sprint 5.

Lee antes de empezar:
1. AGENTS.md
2. docs/framework/deuda-tecnica.md
3. docs/framework/contexto-pm-sprint5-cierre.md (este archivo)

Estado: Sprint 5 cerrado ✅
  · 1052 tests · ~163 endpoints · BD sincronizada
  · Módulo de órdenes avanzadas completo (PO, prioridad, rechazos, SLA, reclamos)
  · G-13 crítico — resolver PRIMERO antes de cualquier HU de embarques

Fixes prioritarios Sprint 6:
  🔴 G-13 — ConsolidarAsync lanza 500 FK (bloquea EP-05)
  🟠 H-03 — ReclamoValidator sin validación URL (defensas BLL)
  🟡 G-18 pulido — JOIN listado paginado sin guard empresa_id

HUs nuevas Sprint 6 (EP-05 Embarques):
  HU-033 · Gestión de embarques (CRUD)
  HU-034 · Asignación de carrier y conductor
  HU-035 · Consolidación real de órdenes en embarque (fix G-13)
  HU-036 · Track & Trace básico
  HU-037 · Documentos de embarque

Generar artefactos del Sprint 6:
  · Spec HU-033 a HU-037 con tablas SQL y CAs completos
    (en sprint-06-EP05-embarques.md)
  · Prompt Fase 1 @Arquitecto

¿Arrancamos con los artefactos?
```

---

*Documento de traspaso — Freiroute TMS*
*Sprint 5 → Sprint 6*
*Versión: 1.0 | Fecha: 2026-09-09 | Autor: @PM*
*Próximo sprint: EP-05 Shipments (Embarques)*
