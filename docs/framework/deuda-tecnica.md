# Registro de Deuda Técnica — Freiroute TMS
**Documento:** Gaps entre Product Backlog y lo implementado
**Fecha de identificación:** 2026
**Responsable:** @PM
**Revisado y validado:** @PM contra código + smoke test

> Este documento es la fuente de verdad para el seguimiento de deuda técnica.
> Cada gap tiene su sprint de origen, su impacto real y el sprint objetivo de resolución.
> **Actualizar este documento al cierre de cada sprint.**

---

## Estado de gaps

| ID | Sprint origen | HU | Descripción | Impacto | Sprint resolución | Estado |
|---|---|---|---|---|---|---|
| G-01 | Sprint 1 | HU-006 | Permisos Aprobar y Exportar no modelados | Medio | Sprint 10-12 | 🔴 Abierto |
| G-02 | Sprint 1 | HU-008 | Vista/UI de exportación de auditoría | Bajo | Sprint 3 | 🟢 Cerrado |
| G-03 | Sprint 2 | HU-010 | Cálculo automático de descuento anual | Bajo | Sprint 4 | 🟢 Cerrado |
| G-04 | Sprint 2 | HU-013 | UI de creación directa de usuario sin validar | Bajo | Sprint 3 smoke test | 🟡 Parcial |
| G-05 | Sprint 2 | HU-014 | Moneda secundaria e idioma del sistema | Medio | Sprint 6 | 🔴 Abierto |
| G-06 | Sprint 1 | HU-007 | Recuperación de contraseña — Fix A/B/D completados · Fix C verificado en Fase 5 | Alto | Sprint 3 | 🟢 Cerrado |
| G-07 | Sprint 3 | HU-015 | UbicacionRequestDto no expone CodigoPostal/horarios/instrucciones en Edit | Bajo | Sprint 4 | 🟢 Cerrado |
| G-08 | Sprint 3 | HU-008 | AuditoriaActivityResponseDto muestra UUID sin nombre/email del usuario | Bajo | Sprint 5 | 🟢 Cerrado |
| G-09 | Sprint 4 | HU-027 | POST guardar-como-plantilla respondía 200 (debe ser 201 CreatedAtAction) | Bajo | Sprint 5 | 🟢 Cerrado |
| G-10 | Sprint 4 | HU-027 | OrigenCreacion siempre MANUAL en órdenes de plantilla/recurrencia | Bajo | Sprint 5 | 🟢 Cerrado |
| G-11 | Sprint 4 | HU-026 | Test integración PATCH /abonos — endpoint no existe en src/ | Bajo | EP-07 Facturación | 🔵 Reclasificado |
| G-12 | Sprint 4 | HU-022 | OrdenesController cobertura 52.4% < 60% | Bajo | Sprint 5 | 🟢 Cerrado |
| G-13 | Sprint 4 | HU-025 | ConsolidarAsync lanza 500 FK — no inserta shipment real en BD | Alta | Sprint 6 | 🔴 Abierto |
| G-14 | Sprint 4 | HU-023 | BCrypt.Verify no usado en ApiKey — API key válida rechazada (401) | Alta | Sprint 5 | 🟢 Cerrado |
| G-15 | Sprint 4 | HU-008 | Servicios de órdenes pasan Detalles como texto plano → cast ::jsonb falla → 0 registros auditoría | Alta | Sprint 5 | 🟢 Cerrado |
| G-16 | Sprint 4 | HU-027 | datosOrden queda "{}" (snapshot vacío al guardar plantilla) | Media | Sprint 5 | 🟢 Cerrado |
| G-17 | Sprint 4 | HU-021 | Historial: falta DRAFT, OrdenId vacío, usuarioNombre null, fechaConfirmacion null | Baja | Sprint 5 | 🟢 Cerrado |
| G-18 | Sprint 4 | HU-021 | Listado devuelve UUIDs crudos en clienteNombre/origenNombre/destinoNombre | Baja | Sprint 5 | 🟢 Cerrado |
| G-19 | Sprint 4 | HU-024 | Detalle PARTIALLY_SPLIT no lista sub-órdenes | Baja | Sprint 6 | 🔴 Abierto |
| H-03 | Sprint 5 | HU-032 | ReclamoValidator no valida formato URL en referencias_evidencia (CA-05 huérfano) | Media | Sprint 6 | 🔴 Abierto |
| H-04 | Sprint 5 | HU-029 | PrioridadOrdenService cobertura 91.3% (ramas ElevarAutomáticas sin cubrir) | Baja | Sprint 6 si baja de 80% | 🔵 Monitorear |

**Leyenda:** 🔴 Abierto · 🟡 Parcial · 🟢 Cerrado · 🔵 Reclasificado/Monitorear

---

## Gaps descartados (falsos positivos)

Los siguientes ítems fueron reportados como posibles gaps pero están
**correctamente implementados** — validados contra código y smoke test:

| HU | Ítem reportado | Evidencia de implementación |
|---|---|---|
| HU-009 | Impersonación con log de auditoría | AdminDashboardService.ImpersonarAsync con claim "impersonado_por" + auditoría IMPERSONACION. Endpoint: POST /api/admin/empresas/{id}/impersonar |
| HU-011 | Facturación recurrente SaaS | SuscripcionService.CreateAsync, RegistrarPagoAsync, ProcesarVencimientosAsync. VencimientoSuscripcionJob activo. Confirmado en smoke test Sprint 2 |

> ⚠️ **Corrección:** HU-007 fue clasificada inicialmente como falso positivo.
> Tras revisión profunda se confirmó que ES un gap real (ver G-06).
> La infraestructura de código existe pero el flujo está roto en producción.

---

## Detalle de cada gap

---

### G-01 · HU-006 · Permisos Aprobar y Exportar

**Sprint origen:** Sprint 1
**Severidad:** Media
**Bloquea:** Sprints 10 (Freight Audit — aprobación de pagos) y 12-13 (facturación — exportación)

**Descripción del gap:**
El backlog original (HU-006) definía 6 tipos de permiso:
Ver, Crear, Editar, **Eliminar, Aprobar, Exportar**.

El ADR-006 y ADR-009 simplificaron a solo 3: READ, CREATE, UPDATE.
La justificación fue correcta para el MVP (Eliminar → soft delete,
sin flujos de aprobación aún). Sin embargo, Aprobar y Exportar
serán necesarios en:

- Sprint 10 (EP-07): Workflow de aprobación de pagos a carriers
  (`[RequirePermission("facturacion", PermissionType.Approve)]`)
- Sprint 12-13 (EP-10): Exportación de facturas y reportes
  (`[RequirePermission("reportes", PermissionType.Export)]`)

**Estado actual:**
```csharp
// Solo existen estos 3 tipos en Freiroute.Utility:
public enum PermissionType { Read, Create, Update }

// La tabla permisos tiene:
// puede_leer | puede_crear | puede_actualizar
// Falta:
// puede_aprobar | puede_exportar
```

**Resolución requerida (Sprint 10 — antes de EP-07):**

1. Migración SQL: agregar columnas `puede_aprobar` y `puede_exportar`
   a la tabla `permisos`
2. Actualizar `PermissionType` enum con `Approve` y `Export`
3. Actualizar `RequirePermissionAttribute` para los nuevos tipos
4. Actualizar la UI de gestión de permisos (checkboxes adicionales)
5. Actualizar los datos semilla de perfiles base

**Impacto en tablas:** `permisos`
**Impacto en código:** `PermissionType.cs`, `RequirePermissionAttribute.cs`,
`Permisos.cshtml` (vista de gestión de permisos)

---

### G-02 · HU-008 · Vista/UI de Exportación de Auditoría

**Sprint origen:** Sprint 1 | **Estado:** 🟢 Cerrado en Sprint 3

---

### G-03 · HU-010 · Descuento Anual Automático en Planes

**Sprint origen:** Sprint 2 | **Estado:** 🟢 Cerrado en Sprint 4

---

### G-04 · HU-013 · UI de Creación Directa de Usuario

**Sprint origen:** Sprint 2
**Severidad:** Baja
**Bloquea:** Nada — el flujo de invitación funciona correctamente

**Descripción del gap:**
El backlog define dos formas de agregar usuarios:
1. **Invitación por email** → implementada y validada ✅
2. **Creación directa** (el Admin crea la cuenta y el usuario recibe
   contraseña temporal) → implementada en API pero no validada en UI

**Estado actual:**
```
API:    ✅ POST /api/usuarios → crea usuario con contraseña temporal
UI:     🟡 Vista Create.cshtml existe pero no fue probada en smoke test
        ❓ Se desconoce si el formulario envía correctamente todos los campos
```

**Resolución requerida:** Validar manualmente en el próximo smoke test que incluya
usuarios. Si falla → fix inmediato en ese sprint.

---

### G-05 · HU-014 · Moneda Secundaria e Idioma del Sistema

**Sprint origen:** Sprint 2
**Severidad:** Media
**Bloquea:** Sprint 12-13 (facturación multi-moneda)

**Estado actual:**
```
BD:       empresas.moneda_secundaria ❌ · empresas.idioma ❌
API:      moneda_secundaria ❌ · idioma ❌
Frontend: moneda_secundaria ❌ · idioma ❌
```

**Resolución requerida (Sprint 6):**
```sql
ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS moneda_secundaria VARCHAR(10),
    ADD COLUMN IF NOT EXISTS idioma           VARCHAR(10) NOT NULL DEFAULT 'es';
```
Actualizar ConfiguracionRequestDto/ResponseDto + ConfiguracionRepository + UI Tab Operación.

---

### G-06 · HU-007 · Recuperación de Contraseña

**Sprint origen:** Sprint 1 | **Estado:** 🟢 Cerrado en Sprint 3

---

### G-07 · HU-015 · UbicacionRequestDto campos faltantes

**Sprint origen:** Sprint 3 | **Estado:** 🟢 Cerrado en Sprint 4

---

### G-08 · HU-008 · AuditoriaActivityResponseDto sin NombreUsuario

**Sprint origen:** Sprint 3 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** LEFT JOIN usuarios en AuditoriaRepository + campo UsuarioNombre/EmailUsuario en DTO +
test `AuditoriaServiceTests.GetPagedAsync_CuandoRepositorioDevuelveJoin_PropagaNombreYEmailDelUsuario`

---

### G-09 · HU-027 · guardar-como-plantilla respondía 200

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** CreatedAtAction en PlantillasOrdenController:35 + test API 201

---

### G-10 · HU-027 · OrigenCreacion siempre MANUAL

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** parámetro opcional en OrdenService.CreateAsync + PlantillaOrdenService pasa
OrigenCreacion.Recurrente + test `GenerarOrdenDesdeRecurrencia_..._OrigenCreacionEsRecurrente`

---

### G-11 · HU-026 · Test PATCH /abonos — Reclasificado

**Sprint origen:** Sprint 4
**Estado:** 🔵 Reclasificado — fuera de alcance EP-04

**Nota:** El endpoint `PATCH /api/ordenes/{id}/abonos` no existe en `src/` (0 coincidencias
en toda la solución). El módulo de abonos pertenece a **EP-07 Facturación**.
**Acción:** Crear nueva HU en el backlog de EP-07 con referencia a G-11.

---

### G-12 · HU-022 · OrdenesController cobertura 52.4%

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** +10 tests API en OrdenesControllerTests → cobertura **100%**

---

### G-13 · HU-025 · ConsolidarAsync lanza 500 FK

**Sprint origen:** Sprint 4
**Severidad:** Alta 🔴
**Bloquea:** EP-05 completo (shipments no se pueden crear via consolidación)

**Descripción del gap:**
`ConsolidarAsync` intenta actualizar `ordenes.shipment_id` pero el shipment
no existe todavía en la tabla `shipments` → violación de FK → 500.

**Causa raíz:** el método no inserta el shipment ANTES de actualizar las órdenes.

**Fix requerido (Sprint 6 — PRIMERO antes de cualquier HU de EP-05):**
```csharp
// ShipmentService.ConsolidarAsync — orden de operaciones corregida:
// 1. INSERT INTO shipments (...) → obtener shipment_id
// 2. UPDATE ordenes SET shipment_id = @ShipmentId WHERE id IN (...)
// Actualmente hace 2 sin 1 → FK violation
```

**Impacto en tablas:** `shipments`, `ordenes`
**Impacto en código:** `ShipmentService.ConsolidarAsync` + tests

---

### G-14 · HU-023 · BCrypt.Verify no usado en ApiKey

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** ApiKeyTenantRepository.cs:104 usa BCrypt.Verify(rawKey, ClaveHash) en C# +
tests de integración (rawKey válida → 200, rawKey+"X" → 401)

---

### G-15 · HU-008 · Auditoría órdenes: cast ::jsonb falla

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** JsonSerializer.Serialize en todos los RegistrarAsync de módulo ordenes
(OrdenService:155,220,247,298 · PlantillaOrdenService:87 · OrdenImportService:196 ·
OrdenApiExternaService:77,115) + test `CreateAsync_RegistraAuditoriaConDetallesJsonValido`

---

### G-16 · HU-027 · datosOrden vacío en plantilla

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** PlantillaOrdenService.GuardarComoPlantillaAsync carga la orden con GetByIdAsync
y serializa snapshot real. Test `DatosOrden.Contains("ClienteId")`

---

### G-17 · HU-021 · Historial de órdenes incompleto

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5
**Evidencia:** 4 sub-fixes (DRAFT registrado, OrdenId real del RETURNING, LEFT JOIN
nombre_completo, fechaConfirmacion poblada) + 4 tests BLL

---

### G-18 · HU-021 · Listado órdenes con UUIDs crudos

**Sprint origen:** Sprint 4 | **Estado:** 🟢 Cerrado en Sprint 5 (parcialmente)
**Evidencia:** JOINs a clientes/ubicaciones con nombres legibles. Test `GetAllAsync_MapeaNombresLegiblesViaJoin`

**Nota:** el JOIN del listado paginado (OrdenRepository.cs:230-232) no lleva guard explícito
`AND c.empresa_id = o.empresa_id`. Funcionalmente seguro (UUID + RLS), pero inconsistente
con estándar ADR-003. **Fix de pulido pendiente Sprint 6** por @IngenieroDatos.

---

### G-19 · HU-024 · PARTIALLY_SPLIT no lista sub-órdenes

**Sprint origen:** Sprint 4
**Severidad:** Baja 🟡
**Bloquea:** UX — el usuario no puede ver las sub-órdenes desde el detalle de la original

**Descripción del gap:**
La vista de detalle de una orden con estado `PARTIALLY_SPLIT` no muestra
la lista de sub-órdenes generadas por el split. La API de sub-órdenes puede
existir pero no está enlazada en la vista.

**Resolución requerida (Sprint 6):**
En `Views/Ordenes/Detalle.cshtml`, agregar sección condicional:
```html
@if (Model.Estado == "PARTIALLY_SPLIT")
{
    <!-- Card "Sub-órdenes" con GET /api/ordenes?ordenOrigenId={id} -->
}
```

---

### H-03 · HU-032 · ReclamoValidator sin validación URL

**Sprint origen:** Sprint 5
**Severidad:** Media 🟠
**Detectado por:** @QA en Fase 4

**Descripción del gap:**
`ReclamoValidator` no implementa la regla de formato URL para `referencias_evidencia`
(CA-05 de HU-032 queda huérfano en la capa BLL).

**Compensación actual:** validación del lado cliente con `$.validator.addMethod('esUrlValida', ...)`
en `Reclamos/Crear.cshtml`. Suficiente para usuarios honestos, no para integración API.

**Fix requerido (Sprint 6 — @BackendDev):**
```csharp
// ReclamoValidator.cs — agregar:
RuleFor(x => x.ReferenciasEvidencia)
    .ForEach(url =>
        url.Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
           .WithMessage("Cada referencia de evidencia debe ser una URL válida absoluta."))
    .When(x => x.ReferenciasEvidencia?.Any() == true);
```
Agregar test `ReclamoValidatorTests.Validate_CuandoUrlInvalida_TieneError`.

---

### H-04 · HU-029 · PrioridadOrdenService cobertura 91.3%

**Sprint origen:** Sprint 5
**Severidad:** Baja 🔵
**Estado:** Monitorear — sobre el umbral BLL ≥80%

**Descripción:** Ramas sin cubrir en `ElevarPrioridadesAutomaticasAsync`:
combinaciones donde la orden ya está en CRITICO/ALTO (no debe elevarse)
y regla CLIENTE_VIP con distintos estados de partida.

**Acción:** cubrir en Sprint 6 si la cobertura acumulada baja del 80%.
Si no baja, diferir a Sprint 7.

---

## Plan de resolución por sprint

```
Sprints 1–5 — CERRADOS:
  G-02 🟢  Vista auditoría (Sprint 3)
  G-06 🟢  Recuperación de contraseña (Sprint 3)
  G-03 🟢  Descuento anual sugerido (Sprint 4)
  G-07 🟢  UbicacionRequestDto campos faltantes (Sprint 4)
  G-08 🟢  AuditoriaActivityResponseDto NombreUsuario (Sprint 5)
  G-09 🟢  guardar-plantilla → 201 (Sprint 5)
  G-10 🟢  OrigenCreacion RECURRENTE (Sprint 5)
  G-12 🟢  OrdenesController cobertura 100% (Sprint 5)
  G-14 🟢  BCrypt.Verify en ApiKey (Sprint 5)
  G-15 🟢  Auditoría JSON válido (Sprint 5)
  G-16 🟢  Snapshot real en plantilla (Sprint 5)
  G-17 🟢  Historial órdenes completo (Sprint 5)
  G-18 🟢  JOINs nombres en listado (Sprint 5, pulido pendiente)
  G-11 🔵  Reclasificado → EP-07 Facturación

Sprint 6 — OBJETIVO:
  G-13 🔴  ConsolidarAsync FK 500 (PRIMERO — bloquea EP-05)
  H-03 🟠  ReclamoValidator URL validation
  G-18 🟡  JOIN guard empresa_id (fix pulido)
  G-19 🟡  PARTIALLY_SPLIT lista sub-órdenes
  G-05 🔴  Moneda secundaria + idioma del sistema

Sprint 10 (antes de EP-07 Freight Audit):
  G-01 🔴  Permisos Aprobar y Exportar

Sin fecha asignada:
  G-04 🟡  Validar UI creación directa usuario (próximo smoke test que incluya usuarios)
  H-04 🔵  PrioridadOrdenService 91.3% (monitorear cobertura acumulada)
```

---

## Criterio de cierre de un gap

Un gap se marca como 🟢 Cerrado cuando:
- [ ] El código está implementado y commiteado
- [ ] `dotnet test` sin fallos (tests nuevos o actualizados que cubren el gap)
- [ ] Validado manualmente en smoke test
- [ ] Este documento actualizado con fecha de cierre

---

*Registro de Deuda Técnica — Freiroute TMS*
*Creado: 2026 | Autor: @PM*
*Última actualización: Sprint 5 cerrado — 2026-09-09*
*Actualizar al cierre de cada sprint*
