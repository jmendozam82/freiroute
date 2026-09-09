# Contexto @PM — Cierre Sprint 4 / Apertura Sprint 5
# Freiroute TMS · EP-04 Order Management → EP-04 Órdenes Avanzadas + Fixes

---

## Estado acumulado del proyecto

```
Sprint 1 ✅  Auth & Multi-Tenant
Sprint 2 ✅  Administración SaaS
Sprint 3 ✅  Maestros & Catálogos
Sprint 4 ✅  Order Management (EP-04 core)
────────────────────────────────────────
4/26 sprints completados
~292 pts de ~1049 completados (~28%)
943 tests · 148 endpoints · 9 tablas nuevas Sprint 4
```

---

## Sprint 4 — CERRADO OFICIALMENTE ✅

```
Sprint 4 · EP-04 — Order Management Core
─────────────────────────────────────────────────────────────
Build:          0 errores / 0 warnings                   ✅
Tests:          943/943 — 0 fallos (+ 2 skipped CI)      ✅
Cobertura BLL:  85.78 % (objetivo ≥80%)                  ✅
Cobertura API:  79.22 % (objetivo ≥60%)                  ✅
CAs:            64/73 verificados                        ✅
                9 diferidos (2 UI, 5 backend gaps,
                1 CI-only, 1 infra)                      →
Schema cloud:   9 migraciones aplicadas ✅               ✅
Swagger:        148 endpoints                            ✅
Smoke test:     S0–S13 ejecutado                        ✅
G-03 descuento: Cerrado                                  ✅
G-07 Ubicación: Cerrado                                  ✅
─────────────────────────────────────────────────────────────
```

### Lo que se construyó en Sprint 4

```
9 tablas nuevas   → contadores_orden, api_keys_tenant, shipments (esqueleto),
                    ordenes, lineas_orden, historial_estados_orden,
                    plantillas_orden, importaciones_orden

148 endpoints     → vs 98 al inicio del Sprint 4

Módulo de órdenes completo:
  HU-021 · Creación manual con FSM
  HU-022 · Importación CSV fail-soft (ADR-017)
  HU-023 · API externa con X-Api-Key (bcrypt)
  HU-024 · Máquina de estados ADR-019 + numeración ADR-020
  HU-025 · Consolidación en shipment (esqueleto)
  HU-026 · División de órdenes (split)
  HU-027 · Plantillas recurrentes + RecurrenciaOrdenesJob

2 ADRs nuevos     → ADR-019 (FSM órdenes) · ADR-020 (numeración)
```

---

## Estado de deuda técnica al arrancar Sprint 5

### Gaps que van a Sprint 5 (de mayor a menor severidad)

| Gap | Severidad | HU | Descripción | Agente |
|-----|-----------|-----|-------------|--------|
| **G-14** | 🔴 Alta | HU-023 | `GetByClaveHashAsync` no usa `BCrypt.Verify` — API key válida rechazada con 401 | @BackendDev |
| **G-15** | 🔴 Alta | HU-008 | Servicios de órdenes pasan `Detalles` como texto plano → cast `::jsonb` falla → 0 registros auditoría módulo `ordenes` | @BackendDev |
| **G-16** | 🟠 Media | HU-027 | `datosOrden` queda `"{}"` (snapshot vacío al guardar plantilla) | @BackendDev |
| **G-10** | 🟠 Media | HU-027 | `OrigenCreacion` siempre `"MANUAL"` en órdenes de plantilla/recurrencia | @BackendDev |
| **G-17** | 🟡 Baja | HU-021 | Historial: falta registro de creación DRAFT, `OrdenId=Guid.Empty`, `usuarioNombre=null`, `fechaConfirmacion=null` | @BackendDev |
| **G-18** | 🟡 Baja | HU-021 | Listado devuelve UUIDs crudos en `clienteNombre/origenNombre/destinoNombre` | @BackendDev |
| **G-09** | 🟡 Baja | HU-027 | `POST guardar-como-plantilla` responde 200 (convención: Create → 201) | @BackendDev |
| **G-11** | 🟡 Baja | HU-026 | Test integración `PATCH /abonos` (FrApi.patch fix) | @QA |
| **G-12** | 🟡 Baja | HU-022 | `OrdenesController` cobertura 52.4% — faltan 4 tests | @QA |

### Gaps diferidos a Sprint 6 (bloquean EP-05)

| Gap | HU | Descripción |
|-----|-----|-------------|
| **G-13** | HU-025 | `ConsolidarAsync` lanza 500 FK — no inserta el shipment real en BD |
| **G-19** | HU-024 | Detalle PARTIALLY_SPLIT no lista sub-órdenes |
| **G-08** | HU-008 | `AuditoriaActivityResponseDto` muestra UUID sin nombre/email |

### Gaps a largo plazo (sin cambio)

| Gap | Sprint objetivo |
|-----|----------------|
| G-05 | Sprint 6 — moneda secundaria |
| G-01 | Sprint 10 — permisos Aprobar/Exportar |

---

## Desviaciones del Sprint 4 aprobadas (para que @Arquitecto no revierta)

| Desviación | Decisión |
|-----------|---------|
| Clases CSS reales: `fr-badge-*` / `fr-btn-*` (AGENTS.md decía `badge-fr-*`) | ✅ Aprobado — CSS real del proyecto; actualizar AGENTS.md |
| Vista `Detalle.cshtml` (no `Detail`) — convención del área Tenant | ✅ Aprobado |
| `TransicionesDisponibles` serializado como `List<string>` (no `IReadOnlySet`) | ✅ Aprobado |
| MVC usa `User.HasPermission()`, no `[RequireModulePermission]` | ✅ Aprobado — patrón correcto |
| ModoTransporte en órdenes: 5 valores (TERRESTRE/AEREO/MARITIMO/FERROVIARIO/INTERMODAL) sin FTL/LTL | ✅ Aprobado — FTL/LTL son de tarifas (Sprint 3), dominios distintos |
| `shipments` tabla es esqueleto mínimo — no expander hasta Sprint 7 | ✅ Aprobado |

---

## Sprint 5 — Definición

**Nombre:** EP-04 Órdenes Avanzadas + Fixes Sprint 4
**Épica principal:** EP-04 — Order Management (continuación)
**Objetivo:** Cerrar los gaps críticos del Sprint 4 (G-14, G-15)
y entregar las HUs avanzadas de órdenes (HU-028 a HU-032).
**Story Points:** 55 pts estimados
**ADRs nuevos requeridos:** 0 (reutilizar ADR-019 y ADR-020)

### Composición del sprint

```
FIXES PRIORITARIOS (sin pts de backlog — deuda técnica)
  Fix G-14 → @BackendDev: BCrypt.Verify en validación de API key
  Fix G-15 → @BackendDev: serializar Detalles como JSON en auditoría de órdenes
  Fix G-16 → @BackendDev: snapshot real en datosOrden de plantillas
  Fix G-10 → @BackendDev: origenCreacion=RECURRENTE en órdenes de plantilla
  Fix G-17 → @BackendDev: historial completo (creación, OrdenId, usuario, fechaConfirmacion)
  Fix G-18 → @BackendDev: JOIN nombres en listado de órdenes
  Fix G-09 → @BackendDev: guardar-como-plantilla → 201 CreatedAtAction
  Fix G-11 → @QA: test integración FrApi.patch abonos
  Fix G-12 → @QA: tests OrdenesController para subir cobertura ≥60%

HUs EP-04 AVANZADAS (del product backlog)
  HU-028 · Gestión de órdenes de compra vinculadas (PO Integration) — 5 pts
  HU-029 · Priorización dinámica de órdenes — 5 pts
  HU-030 · Gestión de rechazos y re-entregas — 5 pts
  HU-031 · SLA Management por cliente — 8 pts
  HU-032 · Gestión de reclamos (Claims Management) — 8 pts
```

### Dependencias del Sprint 5

```
Sprint 1 → Auth, tenants, perfiles, permisos, auditoría ✅
Sprint 2 → Configuración del tenant ✅
Sprint 3 → clientes (SLA definido en Cliente.entity) ✅
Sprint 4 → ordenes (FSM, historial, estados) ✅
           gaps G-14/G-15/G-16/G-17/G-18 resueltos primero
           (los fixes desbloquean HU-028 a HU-032 que usan auditoría y listados)
```

---

## HU-028 · Gestión de órdenes de compra vinculadas (PO Integration)

**Como** operador, **quiero** vincular órdenes de transporte a órdenes de compra o venta del cliente, **para** trazabilidad end-to-end.

**Criterios de aceptación:**
- [ ] CA-01: Campo `numero_po` en la orden (VARCHAR 100, opcional) además del `referencia_cliente` existente
- [ ] CA-02: Campo `numero_so` (Sales Order) en la orden, independiente del PO
- [ ] CA-03: `GET /api/ordenes?po=PO-20260501` — búsqueda exacta por número de PO
- [ ] CA-04: Una PO puede vincularse a múltiples órdenes (relación 1:N)
- [ ] CA-05: `GET /api/ordenes/por-po/{numero}` → lista todas las órdenes con esa referencia PO
- [ ] CA-06: Vista de trazabilidad: PO → Órdenes asociadas → Estado de cada una
- [ ] CA-07: `numero_po` aparece en `OrdenListDto` y en la columna de búsqueda de la tabla
- [ ] CA-08: `numero_po` aparece en `OrdenResponseDto` (detalle)
- [ ] CA-09: Auditoría registra cuando se vincula/cambia el número de PO

**Impacto en BD:** columna `numero_po VARCHAR(100)` y `numero_so VARCHAR(100)` en tabla `ordenes` (migración ALTER TABLE).
**Módulo de permiso:** `ordenes`
**Estimación:** 5 pts | **Prioridad:** Media

---

## HU-029 · Priorización dinámica de órdenes

**Como** dispatcher, **quiero** que las órdenes de alta prioridad se destaquen automáticamente y se puedan reordenar, **para** planificar primero los envíos más urgentes.

**Criterios de aceptación:**
- [ ] CA-01: Campo `prioridad` ya existe (CRITICO/ALTO/NORMAL/BAJO desde Sprint 4) — extender el comportamiento
- [ ] CA-02: Regla automática de elevación: si `fecha_entrega_requerida <= hoy + 1` y estado CONFIRMED → elevar a ALTO automáticamente (background check diario)
- [ ] CA-03: Regla automática: si cliente es VIP (`tipo_cliente='VIP'`) y orden en DRAFT > 2h → elevar a ALTO
- [ ] CA-04: `GET /api/ordenes/criticas` → órdenes en CRITICO o ALTO con > 4h sin avanzar al siguiente estado
- [ ] CA-05: Alerta en dashboard: "X órdenes críticas sin asignar" si hay CRITICO sin shipment > 2h
- [ ] CA-06: Vista Index de órdenes: columna Prioridad ordenable (CRITICO arriba)
- [ ] CA-07: Badge de prioridad con borde parpadeante para CRITICO (CSS animation `pulse`)
- [ ] CA-08: Auditoría registra cuando el sistema eleva la prioridad automáticamente (ACCION: `AUTO_PRIORIDAD`)
- [ ] CA-09: Notificación al operador asignado cuando su orden escala a CRITICO

**Impacto en BD:** nueva tabla `reglas_prioridad` (configuración por tenant) + job `PrioridadOrdenesJob` (patrón ADR-013).
**Módulo de permiso:** `ordenes`
**Estimación:** 5 pts | **Prioridad:** Media

---

## HU-030 · Gestión de rechazos y re-entregas

**Como** operador, **quiero** registrar rechazos de entrega y gestionar re-entregas, **para** resolver incidencias de entrega fallida.

**Criterios de aceptación:**
- [ ] CA-01: `POST /api/ordenes/{id}/rechazo` — registra el rechazo con motivo obligatorio
- [ ] CA-02: Motivos de rechazo: `CLIENTE_AUSENTE | DIRECCION_INCORRECTA | MERCANCIA_DANADA | RECHAZO_CLIENTE | OTRO`
- [ ] CA-03: El rechazo mueve el estado de la orden a `FAILED_DELIVERY` vía FSM (ADR-019 — transición ya definida desde IN_TRANSIT)
- [ ] CA-04: `POST /api/ordenes/{id}/re-entrega` — crea una nueva orden de re-entrega vinculada a la original
- [ ] CA-05: La re-entrega hereda: cliente, origen, destino, tipo_mercancia, unidad_medida de la orden original
- [ ] CA-06: La re-entrega tiene `orden_origen_id` apuntando a la orden fallida (mismo campo que split)
- [ ] CA-07: La re-entrega inicia en estado `CONFIRMED` (no DRAFT — requiere acción inmediata)
- [ ] CA-08: Cargo de re-entrega calculado: `tarifa.precio_unitario × factor_reentrega` (configurable por tenant, default 1.5×)
- [ ] CA-09: Notificación al cliente cuando se registra el rechazo
- [ ] CA-10: `GET /api/ordenes/{id}/re-entregas` → historial de re-entregas de esa orden
- [ ] CA-11: Auditoría registra RECHAZO_ENTREGA y CREAR_REENTREGA

**Impacto en BD:** tabla `rechazos_entrega` (id, empresa_id, orden_id, motivo, descripcion, usuario_id, fecha_creacion).
**Módulo de permiso:** `ordenes`
**Estimación:** 5 pts | **Prioridad:** Media

---

## HU-031 · SLA Management por cliente

**Como** gerente de operaciones, **quiero** definir SLAs de entrega por cliente y monitorear su cumplimiento, **para** gestionar compromisos contractuales.

**Criterios de aceptación:**
- [ ] CA-01: SLA ya definido en `Cliente` (`sla_dias_entrega`, `sla_ventana_inicio`, `sla_ventana_fin`) desde Sprint 3 — extender el monitoreo
- [ ] CA-02: `GET /api/ordenes/sla-en-riesgo` → órdenes donde `fecha_entrega_requerida - now() < 24h` y estado < `IN_TRANSIT`
- [ ] CA-03: `GET /api/clientes/{id}/sla-cumplimiento` → % de órdenes entregadas a tiempo en los últimos 30 días
- [ ] CA-04: Campo calculado `sla_status` en `OrdenResponseDto`: `EN_RIESGO | CRITICO | OK | VENCIDO`
- [ ] CA-05: `sla_status = VENCIDO` si `fecha_entrega_requerida < now()` y orden no está en `DELIVERED | CLOSED | CANCELLED`
- [ ] CA-06: `sla_status = CRITICO` si la entrega vence en < 2h y no está en `IN_TRANSIT` o posterior
- [ ] CA-07: `GET /api/reportes/sla` → reporte de cumplimiento SLA por cliente (periodo configurable)
- [ ] CA-08: Alerta diaria a gerente cuando clientes VIP tienen SLA < 90% en el mes
- [ ] CA-09: El campo `sla_status` alimenta el badge visual en la vista de lista de órdenes

**Impacto en BD:** ninguno — usa campos existentes. Requiere lógica calculada en BLL + job de monitoreo.
**Módulo de permiso:** `ordenes` (lectura) + `analytics` (reportes)
**Estimación:** 8 pts | **Prioridad:** Media

---

## HU-032 · Gestión de reclamos (Claims Management)

**Como** cliente o administrador, **quiero** registrar y gestionar reclamos por incidencias de entrega, **para** obtener resolución formal y compensación.

**Criterios de aceptación:**
- [ ] CA-01: `POST /api/reclamos` — crear reclamo con: `orden_id`, `tipo` (DANO/PERDIDA/RETRASO/OTRO), `descripcion`, `monto_reclamado`
- [ ] CA-02: El reclamo vincula al `orden_id` (y opcionalmente al `shipment_id` cuando exista)
- [ ] CA-03: Estados del reclamo: `ABIERTO → EN_REVISION → APROBADO/RECHAZADO → CERRADO`
- [ ] CA-04: `PATCH /api/reclamos/{id}/estado` → transición de estado con motivo obligatorio
- [ ] CA-05: Adjuntar evidencia (referencias de URL de documentos — Supabase Storage se implementa en Sprint 11)
- [ ] CA-06: `GET /api/reclamos` → listado paginado de reclamos del tenant (filtros: estado, tipo, fecha)
- [ ] CA-07: `GET /api/reclamos/{id}` → detalle con historial de estados
- [ ] CA-08: Notificación al cliente en cada cambio de estado
- [ ] CA-09: `GET /api/reportes/reclamos` → reporte exportable por período, tipo y resolución
- [ ] CA-10: `GET /api/clientes/{id}/reclamos` → reclamos del cliente
- [ ] CA-11: Auditoría registra CREAR_RECLAMO, CAMBIO_ESTADO_RECLAMO
- [ ] CA-12: Solo ADMIN puede mover a APROBADO/RECHAZADO (validación de rol en BLL)

**Impacto en BD:**
```sql
CREATE TABLE reclamos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id),
    orden_id        UUID NOT NULL REFERENCES ordenes(id),
    numero_reclamo  VARCHAR(30),           -- generado igual que numero_orden
    tipo            VARCHAR(20) NOT NULL,  -- DANO|PERDIDA|RETRASO|OTRO
    descripcion     TEXT NOT NULL,
    monto_reclamado NUMERIC(15,2),
    estado          VARCHAR(20) NOT NULL DEFAULT 'ABIERTO',
    referencias_evidencia TEXT[],          -- URLs a Supabase Storage (Sprint 11)
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    creado_por      UUID REFERENCES usuarios(id),
    modificado_por  UUID REFERENCES usuarios(id)
);

CREATE TABLE historial_estados_reclamo (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id),
    reclamo_id      UUID NOT NULL REFERENCES reclamos(id),
    estado_anterior VARCHAR(20),
    estado_nuevo    VARCHAR(20) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID REFERENCES usuarios(id),
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

**Módulo de permiso:** `ordenes`
**Estimación:** 8 pts | **Prioridad:** Media

---

## Estado de la BD al inicio del Sprint 5

### Tablas existentes al cerrar Sprint 4

```
Sprint 1: empresas, usuarios, perfiles, permisos,
          sesiones, invitaciones, auditoria_actividad,
          tokens_reset_password
Sprint 2: planes, suscripciones, pagos_suscripcion,
          configuracion
Sprint 3: ubicaciones, zonas_entrega, ubicacion_zonas,
          tipos_mercancia, unidades_medida, tipos_embalaje,
          clientes, contactos_cliente, tarifas_base,
          recargos_tarifa
Sprint 4: contadores_orden, api_keys_tenant, shipments,
          ordenes, lineas_orden, historial_estados_orden,
          plantillas_orden, importaciones_orden
```

### Tablas nuevas Sprint 5

```
rechazos_entrega    (HU-030)
historial_estados_reclamo (HU-032)
reclamos            (HU-032)
reglas_prioridad    (HU-029, opcional — si se implementa configuración por tenant)
```

### Columnas a agregar a tablas existentes

```sql
-- HU-028: PO/SO reference en ordenes
ALTER TABLE ordenes ADD COLUMN numero_po VARCHAR(100);
ALTER TABLE ordenes ADD COLUMN numero_so VARCHAR(100);
CREATE INDEX idx_ordenes_numero_po ON ordenes(empresa_id, numero_po)
    WHERE numero_po IS NOT NULL;
```

---

## Fixes de deuda técnica — instrucciones para @BackendDev

### Fix G-14 — BCrypt.Verify (PRIORITARIO)

**Causa raíz:** `GetByClaveHashAsync` hace `WHERE clave_hash = @ClaveHash` comparando el hash contra el valor crudo. BCrypt no es reversible — la comparación siempre falla.

**Fix:**
```csharp
// ApiKeyTenantRepository.GetByClaveHashAsync
public async Task<ApiKeyTenant?> GetByClaveHashAsync(string rawKey)
{
    // Traer TODAS las keys activas y verificar en memoria con BCrypt.Verify
    const string sql = "SELECT * FROM api_keys_tenant WHERE activo = true";
    var keys = await _db.QueryAsync<ApiKeyTenant>(sql);
    return keys.FirstOrDefault(k =>
        BCrypt.Net.BCrypt.Verify(rawKey, k.ClaveHash));
}
```

**Test requerido:** test de integración real (sin mock del repositorio) que:
1. Crea una API Key → obtiene rawKey
2. Llama `ValidarApiKeyAsync(rawKey)` → espera `(true, empresaId)`
3. Llama con rawKey modificado → espera `(false, Guid.Empty)`

### Fix G-15 — Auditoría JSON (PRIORITARIO)

**Causa raíz:** los servicios de órdenes pasan objetos anónimos directamente a `RegistrarAsync` como `detalles`. El repo hace `@Detalles::jsonb` pero el parámetro llega como string de texto plano → error 22P02.

**Fix en cada servicio de órdenes:**
```csharp
// ANTES (incorrecto):
await _auditoriaService.RegistrarAsync(
    "ordenes", "CREATE", empresaId, usuarioId,
    detalles: new { ordenId = id, referencia = dto.ReferenciaCliente });

// DESPUÉS (correcto — serializar como JSON string):
await _auditoriaService.RegistrarAsync(
    "ordenes", "CREATE", empresaId, usuarioId,
    detalles: JsonSerializer.Serialize(new { ordenId = id, referencia = dto.ReferenciaCliente }));
```

Aplicar en: `OrdenService`, `OrdenImportService`, `OrdenApiExternaService`, `PlantillaOrdenService`.

**Verificar después:**
```bash
GET /api/auditoria?modulo=ordenes
# Esperado: total > 0, registros con detalles legibles
```

### Fix G-16 — Snapshot de plantilla

**Causa raíz:** `GuardarComoPlantillaAsync` serializa `new OrdenRequestDto()` vacío en lugar de mapear los campos de la orden existente.

**Fix:**
```csharp
// Cargar la orden completa ANTES de serializar
var orden = await _ordenRepository.GetByIdAsync(ordenId, empresaId)
    ?? throw new NotFoundException("Orden", ordenId);

var snapshot = new OrdenRequestDto
{
    ClienteId       = orden.ClienteId,
    OrigenId        = orden.OrigenId,
    DestinoId       = orden.DestinoId,
    TipoMercanciaId = orden.TipoMercanciaId,
    UnidadMedidaId  = orden.UnidadMedidaId,
    TipoEmbalajeId  = orden.TipoEmbalajeId,
    Cantidad        = orden.Cantidad,
    PesoKg          = orden.PesoKg,
    VolumenM3       = orden.VolumenM3,
    ModoTransporte  = orden.ModoTransporte,
    NivelServicio   = orden.NivelServicio,
    Prioridad       = orden.Prioridad,
    Instrucciones   = orden.Instrucciones
};
plantilla.DatosOrden = JsonSerializer.Serialize(snapshot);
```

### Fix G-17 — Historial completo

**Cuatro sub-fixes:**
1. **Registro en CreateAsync:** insertar historial `null → DRAFT` al crear la orden (ya hay código, revisar que `OrdenId` se pase correctamente con el ID retornado por `CreateAsync`)
2. **OrdenId=Guid.Empty:** verificar que `InsertHistorialAsync` recibe el ID real de la orden, no `Guid.Empty` (probablemente se asigna antes del RETURNING id)
3. **usuarioNombre=null:** agregar JOIN `LEFT JOIN usuarios u ON u.id = h.usuario_id` en `GetHistorialAsync` — mismo patrón que `GetHistorialAsync` de auditoría
4. **fechaConfirmacion=null:** verificar que `UpdateEstadoAsync` persiste `FechaConfirmacion` — el SQL usa `COALESCE(@FechaConfirmacion, fecha_confirmacion)`, revisar que el parámetro no llegue null cuando debería tener valor

### Fix G-18 — JOINs en listado

**Causa raíz:** el query de `GetAllAsync` (listado paginado) no tiene los JOINs de `clientes`, `ubicaciones` para resolver los nombres.

**Fix:** agregar al SELECT del listado:
```sql
JOIN clientes    c      ON c.id      = o.cliente_id      AND c.empresa_id = o.empresa_id
JOIN ubicaciones u_orig ON u_orig.id = o.origen_id       AND u_orig.empresa_id = o.empresa_id
JOIN ubicaciones u_dest ON u_dest.id = o.destino_id      AND u_dest.empresa_id = o.empresa_id
```
Y en el SELECT: `c.nombre AS cliente_nombre, u_orig.nombre AS origen_nombre, u_dest.nombre AS destino_nombre`

### Fix G-09 — guardar-como-plantilla → 201

```csharp
// OrdenesController — cambiar:
return Ok(ApiResponse<PlantillaOrdenResponseDto>.Ok(result));
// Por:
return CreatedAtAction(
    nameof(PlantillasOrdenController.GetById),
    "PlantillasOrden",
    new { id = result.Id },
    ApiResponse<PlantillaOrdenResponseDto>.Ok(result));
```

### Fix G-10 — OrigenCreacion=RECURRENTE

```csharp
// OrdenService.CreateAsync — agregar parámetro opcional:
public async Task<OrdenResponseDto> CreateAsync(
    OrdenRequestDto dto,
    Guid empresaId,
    Guid usuarioId,
    string origenCreacion = OrigenCreacion.Manual)   // ← nuevo parámetro
{
    var orden = new Orden
    {
        // ...
        OrigenCreacion = origenCreacion,  // ← usar el parámetro
    };
}

// PlantillaOrdenService — pasar el valor correcto:
await _ordenService.CreateAsync(
    dto, empresaId, usuarioId,
    origenCreacion: OrigenCreacion.Recurrente);  // ← pasar RECURRENTE
```

---

## Orden de implementación Sprint 5

```
Semana 1 — FIXES SPRINT 4 (todos son @BackendDev)
  Día 1-2: G-14 (API key) + G-15 (auditoría JSON) — PRIORITARIOS
  Día 3:   G-16 (snapshot) + G-10 (OrigenCreacion) + G-09 (201)
  Día 4:   G-17 (historial) + G-18 (JOINs listado)
  Día 5:   @QA: G-11 (test patch abonos) + G-12 (cobertura OrdenesController)
           Smoke de fixes: verificar G-14 (API key 201) + G-15 (auditoría órdenes > 0)

Semana 2 — HUs NUEVAS
  @Arquitecto: HU-028 + HU-029 + HU-030 (entities, DTOs, interfaces)
  @IngenieroDatos: migraciones (ALTER TABLE ordenes + tablas nuevas) + repositorios
  @BackendDev: servicios + controllers HU-028 a HU-032

Semana 3 — HUs COMPLETAR
  @BackendDev: HU-031 (SLA) + HU-032 (Reclamos) — los más complejos
  @QA: tests BLL ≥80% + API ≥60%
  @FrontendDev: vistas de las 5 HUs nuevas
```

---

## Contexto técnico acumulado para el nuevo chat

### Stack y convenciones (sin cambio)

```
Backend:    ASP.NET Core .NET 8 · C# · Dapper
BD:         Supabase PostgreSQL · RLS por empresa_id
Auth:       JWT claims · X-Api-Key para integración externa
Patrones:   BusinessException → 422 · Create → 201 · Soft delete
            empresa_id en todas las queries · RLS como segunda capa
            PeriodicTimer para background jobs (ADR-013)
```

### Módulos de permiso reales

```
ordenes · embarques · carriers · rutas · track_trace · documentos
flota · analytics · facturacion · clientes · usuarios · configuracion
```

### ADRs vigentes al inicio Sprint 5

```
ADR-001: Stack tecnológico
ADR-002: Arquitectura N-tier
ADR-003: Multi-tenant RLS
ADR-004: Planes y suscripciones
ADR-005: Soft delete universal
ADR-006: Permisos RCU sin delete
ADR-007: JWT claims tenant
ADR-008: ApiResponse pattern
ADR-009: Modelo de permisos flags
ADR-010: Onboarding wizard
ADR-011: Cifrado TOTP AES-256
ADR-012: Signed URLs logos
ADR-013: Background job vencimientos (patrón PeriodicTimer)
ADR-014: Geocodificación mapas (Nominatim)
ADR-015: Tarifas flete (versionado)
ADR-016: Dependencias entity-dto-utility
ADR-017: Importación CSV fail-soft
ADR-018: Point-in-polygon zonas
ADR-019: Máquina de estados orden (FSM)
ADR-020: Numeración automática órdenes
```

### Notas de implementación importantes

```
1. FrApi.patch fue corregido en Sprint 4 (retrocompatible)
   — no reinventar, usar el helper corregido

2. Clases CSS del Design System:
   fr-badge-* (no badge-fr-*) — AGENTS.md está desactualizado
   fr-btn-*

3. MVC usa User.HasPermission() en vistas
   NO existe [RequireModulePermission] en el área Tenant

4. TransicionesDisponibles → List<string> en JSON (no IReadOnlySet)

5. Las vistas del área Tenant se llaman Detalle.cshtml (no Detail)

6. ModoTransporte en ÓRDENES: 5 valores
   TERRESTRE|AEREO|MARITIMO|FERROVIARIO|INTERMODAL
   ModoTransporte en TARIFAS: FTL|LTL|AEREO|MARITIMO|FERROVIARIO|INTERMODAL
   Son dominios distintos — no mezclar

7. shipments tabla es esqueleto de 6 columnas — Sprint 7 la expande
   No agregar columnas en Sprint 5

8. El test de concurrencia de numeración está Skipped
   Ejecutar con supabase start en CI antes de merge a main
```

---

## Prompt de arranque Sprint 5

Usar este prompt al abrir el nuevo chat en el Proyecto Claude:

```
Continuamos el desarrollo de Freiroute TMS.

Sprint 5 — EP-04 Órdenes Avanzadas + Fixes Sprint 4.

Lee antes de empezar:
1. AGENTS.md
2. docs/framework/deuda-tecnica.md
3. docs/framework/contexto-pm-sprint4-cierre.md (este archivo)

Estado: Sprint 4 cerrado ✅
  · 943 tests · 148 endpoints · BD sincronizada
  · Módulo de órdenes base implementado (FSM, importación, API externa, plantillas)
  · Gaps G-14 y G-15 críticos — resolver PRIMERO antes de cualquier HU nueva

Fixes prioritarios Sprint 5:
  🔴 G-14 — API key BCrypt.Verify (HU-023 rota en producción)
  🔴 G-15 — Auditoría de órdenes falla silenciosamente (22P02)
  🟠 G-16, G-10, G-17, G-18, G-09 — fixes de calidad
  🟡 G-11, G-12 — tests adicionales @QA

HUs nuevas Sprint 5 (EP-04 avanzadas):
  HU-028 · PO Integration
  HU-029 · Priorización dinámica
  HU-030 · Rechazos y re-entregas
  HU-031 · SLA Management
  HU-032 · Claims Management

Generar artefactos del Sprint 5:
  · Spec HU-028 a HU-032 con tablas SQL y CAs completos
    (consolidadas en sprint-05-EP04-ordenes-avanzadas.md)
  · Prompt Fase 1 @Arquitecto
  (los fixes van embebidos en los prompts de cada agente,
   no como artefactos independientes)

¿Arrancamos con los artefactos?
```

---

*Documento de traspaso — Freiroute TMS*
*Sprint 4 → Sprint 5*
*Versión: 1.0 | Fecha: 2026-09-08*
*Autor: @PM*
*Próximo sprint: EP-04 Órdenes Avanzadas + Fixes Sprint 4*
