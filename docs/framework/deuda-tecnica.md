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
| G-08 | Sprint 3 | HU-008 | AuditoriaActivityResponseDto muestra UUID sin nombre/email del usuario | Bajo | Sprint 5 | 🔴 Abierto |
| G-09 | Sprint 4 | HU-027 | guardar-como-plantilla responde 200 (esperado 201) | Baja | Sprint 5 | 🔴 Abierto |
| G-10 | Sprint 4 | HU-027 | Órdenes desde plantilla con origenCreacion=MANUAL (esperado RECURRENTE) | Media | Sprint 5 | 🔴 Abierto |
| G-11 | Sprint 4 | HU-026 | Falta test PUT /api/ordenes/{id}/abonos (FrApi.patch) | Baja | Sprint 5 | 🔴 Abierto |
| G-12 | Sprint 4 | HU-022 | Cobertura OrdenesController 52.4% < 60% (API tests) | Baja | Sprint 5 | 🔴 Abierto |
| G-13 | Sprint 4 | HU-022 | POST /api/ordenes/consolidar → 500 FK 23503 (no inserta shipment) | Alta | Sprint 6 | 🔴 Abierto |
| G-14 | Sprint 4 | HU-023 | /api/v1/orders rechaza key válida con 401 (validación rota, no usa BCrypt.Verify) | Alta | Sprint 5 | 🔴 Abierto |
| G-15 | Sprint 4 | HU-008 | Auditoría de órdenes falla en silencio: Detalles no es JSON válido (22P02) | Alta | Sprint 5 | 🔴 Abierto |
| G-16 | Sprint 4 | HU-027 | Plantilla guarda datosOrden "{}" (snapshot vacío) | Media | Sprint 5 | 🔴 Abierto |
| G-17 | Sprint 4 | HU-021 | Historial de estados: 1 sola entrada, ordenId=Guid.Empty, usuarioNombre=null, fechaConfirmacion=null | Baja | Sprint 5 | 🔴 Abierto |
| G-18 | Sprint 4 | HU-021 | Listado devuelve UUIDs crudos en clienteNombre/origenNombre/destinoNombre | Baja | Sprint 5 | 🔴 Abierto |
| G-19 | Sprint 4 | HU-024 | Detalle de orden PARTIALLY_SPLIT no lista sub-órdenes (count 0) | Baja | Sprint 6 | 🔴 Abierto |

> ℹ️ **Nota de renumeración (Sprint 4):** el checklist del smoke test Sprint 4 se refería
> a los gaps de plantillas como "G-08..G-11", pero el documento ya tenía el G-08
> asignado (HU-008 auditoría, Sprint 3). Se renumeraron a **G-09..G-12** para no
> colisionar; los bugs nuevos del smoke (S7/S9/S12) son **G-13..G-19**.

**Leyenda:** 🔴 Abierto · 🟡 Parcial (API existe, UI pendiente) · 🟢 Cerrado

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

**Sprint origen:** Sprint 1
**Severidad:** Baja
**Bloquea:** Nada — es funcionalidad de conveniencia

**Descripción del gap:**
El endpoint `GET /api/auditoria/export` existe y retorna CSV con UTF-8 BOM.
La auditoría se registra correctamente en `auditoria_actividad`.

Lo que falta:
- La vista frontend de auditoría no fue construida por @FrontendDev
  (no estaba en las entregas del Sprint 1 ni Sprint 2)
- No hay botón "Exportar" en ninguna pantalla del sistema

**Estado actual:**
```
API:      ✅ GET /api/auditoria?filtros → PagedResult<AuditoriaActivityResponseDto>
API:      ✅ GET /api/auditoria/export → CSV
Frontend: ❌ No existe vista de auditoría
```

**Resolución requerida (Sprint 3 — @FrontendDev):**

Crear vista: `Areas/Admin/Views/Auditoria/Index.cshtml`

```
Columnas: Fecha | Usuario | Módulo | Acción | Entidad | IP
Filtros:  Módulo select | Acción select | Fecha desde/hasta
Botón:    [Exportar CSV] → GET /api/auditoria/export + mismo query
Paginación: 20 registros estándar
Solo visible para: SUPER_ADMIN y ADMIN con permiso configuracion:read
```

Agregar al sidebar de `_Layout.cshtml`:
```html
<!-- Bajo sección Administración para ADMIN -->
<a asp-area="Admin" asp-controller="Auditoria" asp-action="Index"
   class="fr-sidebar-item">
    <i class="ti ti-clipboard-data fr-icon"></i>
    <span>Auditoría</span>
</a>
```

**Impacto en tablas:** Ninguno
**Impacto en código:** Nueva vista + AuditoriaController MVC (si no existe)

---

### G-03 · HU-010 · Descuento Anual Automático en Planes

**Sprint origen:** Sprint 2
**Severidad:** Baja
**Bloquea:** Nada — es UX de conveniencia

**Descripción del gap:**
El backlog definió: "Precio anual con descuento calculado automáticamente
(sugerencia: 2 meses gratis)".

Lo implementado: `precio_mensual` y `precio_anual` son campos
independientes que el Super Admin ingresa manualmente. No hay
cálculo automático ni sugerencia en la UI.

**Estado actual:**
```
BD:       ✅ precio_mensual + precio_anual (campos separados)
API:      ✅ Ambos campos en PlanRequestDto
Frontend: ❌ No muestra el cálculo sugerido al ingresar precio mensual
```

**Resolución requerida (Sprint 4 — @FrontendDev):**

En `Areas/Admin/Views/Planes/Create.cshtml` y `Edit.cshtml`:

```javascript
// Al cambiar precio mensual → sugerir precio anual:
document.getElementById('precioMensual').addEventListener('input', (e) => {
    const mensual  = parseFloat(e.target.value) || 0;
    const sugerido = (mensual * 10).toFixed(2); // 2 meses gratis
    document.getElementById('precioAnualSugerido').textContent =
        `Sugerido: $${sugerido} (2 meses gratis)`;
    // NO auto-rellenar el campo — solo mostrar la sugerencia
});
```

Agregar label debajo del campo `precioAnual`:
```html
<span id="precioAnualSugerido" class="fr-form-hint">
    Ingresa el precio mensual para ver la sugerencia
</span>
```

**Impacto en tablas:** Ninguno
**Impacto en código:** Solo frontend — 10 líneas de JavaScript

**✅ Cerrado — Sprint 4 (smoke test S11):**
Evidencia: `Areas/Admin/Views/Planes/Create.cshtml` (L271-280) y
`Edit.cshtml` (L263-272) implementan la sugerencia dinámica
`Sugerido: ${(mensual * 10).toFixed(2)} (10 meses — 2 meses gratis)`
sin autocompletar el campo `PrecioAnual` (solo orienta al operador).
Con `precioMensual=100` → muestra "Sugerido: $1000.00 (10 meses — 2 meses gratis)".

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
        ❓ Se desconoce si el formulario envía correctamente todos los
           campos (nombre, email, tipo identidad, número identidad,
           teléfono, perfil, tipo usuario)
```

**Resolución requerida:**

En el próximo smoke test (Sprint 3), validar manualmente:
1. Ir a /tenant/usuarios → [Crear usuario]
2. Completar todos los campos del formulario
3. Verificar que POST /api/usuarios retorna 201
4. Verificar que el EMAIL STUB loguea la contraseña temporal
5. Verificar que el usuario aparece en la lista con estado ACTIVE

Si falla alguno → fix inmediato en Sprint 3.

**Impacto en tablas:** Ninguno (si el API funciona correctamente)
**Impacto en código:** Posiblemente ajustes menores en Create.cshtml

---

### G-05 · HU-014 · Moneda Secundaria e Idioma del Sistema

**Sprint origen:** Sprint 2
**Severidad:** Media
**Bloquea:** Sprint 12-13 (facturación multi-moneda) y Sprint 26 (localización)

**Descripción del gap:**
El backlog definía en HU-014:
- "Moneda principal y secundaria" → solo `moneda_principal` implementada
- "Idioma del sistema (ES / EN / PT)" → no implementado en absoluto

**Estado actual:**
```
BD:
  empresas.moneda_principal ✅
  empresas.moneda_secundaria ❌ — campo no existe
  empresas.idioma ❌ — campo no existe

API:
  GET/PUT /api/configuracion → moneda_principal ✅
  moneda_secundaria ❌
  idioma ❌

Frontend:
  Tab "Operación" en configuración → moneda_principal ✅
  moneda_secundaria ❌
  idioma ❌
```

**Resolución requerida (Sprint 6 — antes de EP-05 Carriers con tarifas multi-moneda):**

**Migración SQL:**
```sql
ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS moneda_secundaria VARCHAR(10),
    ADD COLUMN IF NOT EXISTS idioma           VARCHAR(10) NOT NULL DEFAULT 'es';

COMMENT ON COLUMN empresas.moneda_secundaria IS
    'Moneda secundaria del tenant para reportes y conversión. Opcional.';
COMMENT ON COLUMN empresas.idioma IS
    'Idioma del sistema: es (español), en (inglés), pt (portugués Brasil)';
```

**API:** Actualizar `ConfiguracionRequestDto` y `ConfiguracionResponseDto`
con los campos nuevos. Actualizar `ConfiguracionRepository.UpdateConfiguracionAsync`.

**Frontend:** En Tab "Operación" agregar:
```
Moneda secundaria select (opcional): USD, EUR, GTQ, HNL, NIO, etc.
Idioma del sistema select: Español (ES) | English (EN) | Português (PT)
```

**Idioma en JWT:** Cuando se implemente la localización (Sprint 26),
el claim `idioma` se agregará al JWT para que el middleware de
localización seleccione el culture correcto por request.

**Impacto en tablas:** `empresas` — 2 columnas nuevas
**Impacto en código:** Migración, ConfiguracionRepository,
ConfiguracionRequestDto, ConfiguracionResponseDto, Tab Operación UI

---

---

### G-06 · HU-007 · Recuperación de Contraseña — Flujo roto

**Sprint origen:** Sprint 1
**Severidad:** Alta
**Bloquea:** Cualquier usuario que olvide su contraseña no puede recuperar acceso

**⚠️ Corrección de clasificación:** Este gap fue incorrectamente marcado como
"falso positivo" en la primera revisión. La infraestructura de código existe
pero el flujo está roto en 3 puntos críticos.

**Descripción del gap:**

**Problema 1 — Link en Login.cshtml no validado**
```
Login.cshtml debería tener:
  "¿Olvidaste tu contraseña?" → /auth/forgot-password

No fue validado en ningún smoke test.
Estado real: desconocido — puede existir o no.
```

**Problema 2 — SupabaseAuthService es STUB (bloqueante)**
```csharp
// ResetPasswordAsync en AuthService:
await _supabaseAuthService.SignUpAsync(email, nuevaPassword);
// ↑ SupabaseAuthServiceStub solo LOGUEA — no cambia nada en Supabase Auth.
// El usuario que recupera su contraseña sigue sin poder ingresar.
```
Este es el problema más crítico. Sin integrar la API real de Supabase Auth
para el cambio de contraseña, el flujo completo es un espejismo funcional.

**Problema 3 — No existe reset manual por Admin**
```
❌ No existe: PUT /api/usuarios/{id}/reset-password
❌ No existe: PUT /api/usuarios/{id}/generar-password-temporal
El Admin no puede ayudar a un usuario bloqueado.
```

**Resolución requerida (Sprint 3 — @BackendDev prioritario):**

**Fix A — Integrar Supabase Auth Admin API para cambio de contraseña:**
```csharp
// ISupabaseAuthService — agregar método real:
Task<bool> CambiarPasswordAsync(Guid supabaseUserId, string nuevaPassword);

// Implementación real usando Supabase Admin API:
// PATCH https://{ref}.supabase.co/auth/v1/admin/users/{user_id}
// Authorization: Bearer {SERVICE_ROLE_KEY}
// Body: { "password": "nuevaPassword" }
// Esta API requiere el SERVICE_ROLE_KEY (no el JWT del usuario)
```

**Fix B — Endpoint de reset manual por Admin:**
```
PUT /api/usuarios/{id}/reset-password
[Authorize] [RequirePermission("usuarios", Update)]
Body: { "generarTemporal": true }
→ Genera Fr{XXXX}! como contraseña temporal
→ Llama CambiarPasswordAsync en Supabase Auth Admin API
→ Envía email al usuario con la contraseña temporal
→ Auditoría: "usuarios", "RESET_PASSWORD_ADMIN"
```

**Fix C — Verificar link en Login.cshtml:**
```html
<!-- Confirmar que existe en Login.cshtml: -->
<a href="/auth/forgot-password"
   style="font-size:12px; color:var(--fr-text-muted)">
    ¿Olvidaste tu contraseña?
</a>
```

**Fix D — Actualizar ResetPasswordAsync para llamar la API real:**
```csharp
// En AuthService.ResetPasswordAsync, reemplazar el stub:
// ANTES:
await _supabaseAuthService.SignUpAsync(email, dto.NuevoPassword);

// DESPUÉS:
var usuario = await _usuarioRepository
    .GetByEmailGlobalAsync(invitacion.Email);
await _supabaseAuthService.CambiarPasswordAsync(
    usuario!.SupabaseUserId!.Value, dto.NuevoPassword);
// Luego invalidar todas las sesiones activas del usuario
await _sesionRepository
    .RevocarTodasPorUsuarioAsync(usuario.Id, usuario.EmpresaId);
```

**Impacto en tablas:** Ninguno nuevo
**Impacto en código:**
- `ISupabaseAuthService.cs` → agregar `CambiarPasswordAsync`
- `SupabaseAuthServiceStub.cs` → loguear el intento (stub sigue para dev)
- `SupabaseAuthServiceReal.cs` → implementar con Admin API
- `AuthService.ResetPasswordAsync` → usar el método real
- `UsuariosController.cs` → nuevo endpoint reset por Admin
- `Login.cshtml` → verificar y/o agregar link

---

### G-09 · HU-027 · guardar-como-plantilla responde 200 (esperado 201)

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** Nada (el flujo funciona; solo el status code es incorrecto)

**Descripción del gap:**
`POST /api/ordenes/{id}/guardar-como-plantilla` responde **200 OK**.
El criterio de aceptación HU-027 (CA-01) espera **201 Created** (recurso nuevo).

**Evidencia (smoke test S10):** response 200, plantilla creada y listada
(`0db2b414-b1dd-433a-ba8e-a765077c06ea`, "Envio semanal ABC").

**Resolución (Sprint 5 — @BackendDev):** devolver `201 Created`
en `PlantillaOrdenService` / `OrdenesController` + test de integración.

---

### G-10 · HU-027 · Órdenes desde plantilla con origenCreacion=MANUAL

**Sprint origen:** Sprint 4 — **Severidad:** Media
**Bloquea:** Trazabilidad del canal de ingreso (el sistema no distingue
órdenes recurrentes de las manuales)

**Descripción del gap:**
Al crear una orden desde una plantilla (HU-027 CA-02), el campo
`origen_creacion` queda en `MANUAL`. Se espera `RECURRENTE`
(la orden nace de una recurrencia programada).

**Evidencia (smoke test S10):** POST desde plantilla → 201, `origen_creacion="MANUAL"`.

**Resolución (Sprint 5 — @BackendDev):** `PlantillaOrdenService.CrearDesdePlantillaAsync`
debe setear `OrigenCreacion = OrigenCreacionOrden.Recurrente` + test unitario.

---

### G-11 · HU-026 · Falta test de abonos con FrApi.patch

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** Nada (el fix `FrApi.patch` retrocompatible ya está en Fase 5)

**Descripción del gap:**
No existe test QA para `PUT /api/ordenes/{id}/abonos` usando el patrón
`FrApi.patch` (el problema que rompía la consumición del API desde
MVC con jQuery `$.ajax({method:'PATCH'})`).

**Resolución (Sprint 5 — @QA):** test de integración que consume
`/api/ordenes/{id}/abonos` con PATCH vía FrApi y verifica 200 + suma de abono.

---

### G-12 · HU-022 · Cobertura OrdenesController 52.4% < 60%

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** Umbral del pipeline CI solo si se habilita para controllers nuevos

**Descripción del gap:**
Cobertura de `OrdenesController` = **52.4%**, inferior al umbral API del 60%
(AGENTS.md R29). Los flujos no cubiertos: importar CSV, consolidar/desconsolidar,
split, abonos y endpoints de plantillas.

**Resolución (Sprint 5 — @QA):** tests de integración para los endpoints
faltantes hasta ≥60%.

---

### G-13 · HU-022 · POST /api/ordenes/consolidar → 500 FK 23503 ⚠️ NUEVO (smoke S7)

**Sprint origen:** Sprint 4 — **Severidad:** ALTA
**Bloquea:** HU-024 CA-03 (consolidación) — el flujo queda ❌ en el smoke test

**Descripción del gap:**
`POST /api/ordenes/consolidar` lanza **500** con
`23503: foreign key constraint "ordenes_shipment_id_fkey"`.
`OrdenService.ConsolidarAsync` genera `Guid.NewGuid()` como shipment **sin
insertar el registro** (comentario: "We mock shipment creation... belongs to
Sprint 7"); `ShipmentRepository.CreateAsync` existe en DAL pero no se invoca.
Además `ShipmentResumenDto` expone `CantidadOrdenes` (el checklist esperaba
`totalOrdenes`).

**Evidencia (smoke test S7):** 500 en consolidar; desconsolidar con
`"Solo se pueden desconsolidar órdenes en estado ASSIGNED"` (422) ✓.

**Resolución (Sprint 6 — @BackendDev, antes de EP-05 embarques):**
insertar el shipment real (o devolver 501 con la HU de embarques si se
pospone el módulo), y alinear `ShipmentResumenDto` al nombre del checklist.

---

### G-14 · HU-023 · /api/v1/orders rechaza key válida con 401 ⚠️ NUEVO (smoke S9)

**Sprint origen:** Sprint 4 — **Severidad:** ALTA
**Bloquea:** HU-023 CA-02 (integración externa) — el flujo queda ❌ en el smoke test

**Descripción del gap:**
Una API key recién creada (`frk_live_...`, validada como `activo=true`)
devuelve **401 "API Key inválida o inactiva."** al consultar `/api/v1/orders`.
Causa raíz: `ApiKeyTenantRepository.GetByClaveHashAsync` compara
`clave_hash = @valorCrudo` directamente (nunca matchea tras el hashing bcrypt de
la clave) y `OrdenApiExternaService.ValidarApiKeyAsync` pasa la clave cruda.
**No se usa `BCrypt.Verify` en ningún punto.** Los tests mockean el repositorio
y no detectan el fallo. Sin key → 401 ✓ · key inválida → 401 ✓ · `rawKey=null`
en GET ✓ (CA-08) · `ultimoUso` no verificable mientras la validación esté rota.

**Resolución (Sprint 5 — @BackendDev, prioritario):**
`GetByClaveHashAsync` debe traer el registro por `clave_hash` (hash del id de la
key o similar estable) y `ValidarApiKeyAsync` debe ejecutar `BCrypt.Verify(clave, hash)`;
test de integración real (sin mock) cubriendo el 401→200.

---

### G-15 · HU-008 · Auditoría de órdenes falla en silencio ⚠️ NUEVO (smoke S12)

**Sprint origen:** Sprint 4 — **Severidad:** ALTA
**Bloquea:** HU-008 CA-03 (trazabilidad completa) — módulo `ordenes` sin registros

**Descripción del gap:**
`AuditoriaRepository.RegistrarAsync` castea `@Detalles::jsonb`, pero los
servicios de órdenes (`OrdenService`, `OrdenImportService`,
`PlantillaOrdenService`) pasan `Detalles` como **texto plano**
("Creación de orden (Ref: ...)") → `22P02: invalid input syntax for type json`.
El repositorio **no propaga excepciones por diseño** (correcto), pero el
resultado es la pérdida silenciosa de TODA la auditoría de órdenes:
`GET /api/auditoria?modulo=ordenes` → `total=0` (existen 92 registros de
auth/configuracion/onboarding que sí serializan JSON). Evidencia en
`api-sprint4.log`: `ERR Fallo al registrar auditoría. Modulo=ordenes, Accion=CREATE ... 22P02`.

**Resolución (Sprint 5 — @BackendDev, prioritario):**
serializar `Detalles` con `JsonSerializer` en los servicios del módulo ordenes
(patrón ya usado por auth/configuracion) o quitar el cast `::jsonb` del INSERT
(y `::text` del SELECT) en `AuditoriaRepository`. Verificar después con
`GET /api/auditoria?modulo=ordenes`.

---

### G-16 · HU-027 · Plantilla guarda datosOrden "{}" ⚠️ NUEVO (smoke S10)

**Sprint origen:** Sprint 4 — **Severidad:** Media
**Bloquea:** HU-027 CA-01 (la plantilla no captura el snapshot de la orden)

**Descripción del gap:**
Al guardar como plantilla, `datosOrden` queda como `"{}"` (JSON vacío),
aunque `esRecurrente`, `frecuencia=SEMANAL` y `proximaEjecucion=2026-09-15`
se guardan bien. El snapshot del cuerpo de la orden no se persiste.

**Resolución (Sprint 5 — @BackendDev):** capturar el DTO de la orden
(`OrdenRequestDto`) serializado en `datosOrden` al crear la plantilla.

---

### G-17 · HU-021 · Historial de estados incompleto ⚠️ NUEVO (smoke S4)

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** HU-008 CA-01/02 (el historial debe reflejar Inicio → Borrador)

**Descripción del gap (desviaciones menores, verificadas vía API):**
- El historial de una orden solo contiene la transición DRAFT→CONFIRMED
  (falta el registro de creación "Inicio → Borrador").
- `ordenId=Guid.Empty` en las entradas del historial.
- `usuarioNombre=null` en las entradas (falta JOIN con usuarios).
- `fechaConfirmacion=null` tras confirmar la orden (el campo no se actualiza).

**Resolución (Sprint 5 — @BackendDev):** registrar el historial de creación,
mapear `OrdenId`, enriquecer con el nombre del usuario (patrón G-08) y
persistir `fecha_confirmacion` en la transición a CONFIRMED.

---

### G-18 · HU-021 · Listado devuelve UUIDs crudos en nombres ⚠️ NUEVO (smoke S2)

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** UX del listado (los nombres no se resuelven)

**Descripción del gap:**
`GET /api/ordenes` (paginado) devuelve `clienteNombre`, `origenNombre` y
`destinoNombre` como **UUIDs crudos** en lugar de los nombres legibles
(Distribuidora ABC S.A., Almacen Managua, Bodega Leon).

**Resolución (Sprint 5 — @BackendDev):** JOIN con catálogos en el query del
listado (mismo patrón que ya resuelve `tipoMercanciaNombre`, etc.).

---

### G-19 · HU-024 · Detalle PARTIALLY_SPLIT no lista sub-órdenes ⚠️ NUEVO (smoke S6)

**Sprint origen:** Sprint 4 — **Severidad:** Baja
**Bloquea:** UX del detalle (no se ven las sub-órdenes del split)

**Descripción del gap:**
`GET /api/ordenes/{id}` de la orden original tras un split válido muestra
`subOrdenesCount=0` y no incluye la lista de sub-órdenes (el split SÍ creó
las 2 sub-órdenes CONFIRMED — verificadas individualmente).

**Resolución (Sprint 6 — @BackendDev):** devolver las sub-órdenes en el
detalle (JOIN por `orden_padre_id`) o al menos el count correcto.

---

## Plan de resolución por sprint

```
Sprint 3 — CERRADO:
  G-02 → Vista de auditoría implementada por @FrontendDev     🟢
  G-06 → Fix A/B/C/D completados — reset password funcional   🟢

Sprint 3 smoke test (pendiente):
  G-04 → Validar UI de creación directa de usuario            🟡

Sprint 4 — CERRADO:
  G-03 → Sugerencia de descuento anual sin autocompletar (Create+Edit) 🟢
  G-07 → UbicacionResponseDto enriquecido (Fix en commit 71b5f7d)       🟢

Sprint 5:
  G-09 → @BackendDev: guardar-como-plantilla → 201 Created
  G-10 → @BackendDev: origen_creacion=RECURRENTE en órdenes de plantilla
  G-11 → @QA: test PATCH /api/ordenes/{id}/abonos (FrApi.patch)
  G-12 → @QA: subir cobertura OrdenesController a ≥60%
  G-14 → @BackendDev (prioritario): fix validación API key externa
          (BCrypt.Verify + test real sin mock) — HU-023 CA-02
  G-15 → @BackendDev (prioritario): auditoría de órdenes (serializar
          Detalles como JSON o quitar cast ::jsonb) — HU-008
  G-16 → @BackendDev: snapshot real en datosOrden (plantillas)
  G-17 → @BackendDev: historial completo (creación, OrdenId, usuario,
          fecha_confirmacion)
  G-18 → @BackendDev: JOIN nombres en listado de órdenes

Sprint 6 (antes de EP-05 Carriers/embarques):
  G-13 → @BackendDev: consolidar debe insertar shipment real
          (o 501 explícito) — HU-024 CA-03
  G-19 → @BackendDev: sub-órdenes en detalle de PARTIALLY_SPLIT
  G-05 → @IngenieroDatos + @BackendDev + @FrontendDev:
          moneda secundaria + idioma del sistema

Sprint 10 (antes de EP-07 Freight Audit):
  G-01 → @Arquitecto + @IngenieroDatos + @BackendDev + @FrontendDev:
          permisos Aprobar y Exportar en BD + código + UI
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
*Actualizar al cierre de cada sprint*
