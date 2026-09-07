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
| G-02 | Sprint 1 | HU-008 | Vista/UI de exportación de auditoría | Bajo | Sprint 3 (pendiente FrontendDev) | 🟡 Parcial |
| G-03 | Sprint 2 | HU-010 | Cálculo automático de descuento anual | Bajo | Sprint 4 | 🔴 Abierto |
| G-04 | Sprint 2 | HU-013 | UI de creación directa de usuario sin validar | Bajo | Sprint 3 (validar en smoke test) | 🟡 Parcial |
| G-05 | Sprint 2 | HU-014 | Moneda secundaria e idioma del sistema | Medio | Sprint 6 | 🔴 Abierto |
| G-06 | Sprint 1 | HU-007 | Recuperación de contraseña rota — SupabaseAuth es stub + link en Login no validado + no existe reset manual por Admin | Alto | Sprint 3 | 🔴 Abierto |

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

## Plan de resolución por sprint

```
Sprint 3 (actual) — PRIORITARIO:
  G-06 → @BackendDev: integrar Supabase Auth Admin API para reset
          password real + endpoint reset manual por Admin
          + verificar link en Login.cshtml
  G-02 → @FrontendDev: vista de auditoría + sidebar link
  G-04 → Validar en smoke test de Sprint 3

Sprint 4:
  G-03 → @FrontendDev: sugerencia de descuento anual en UI de planes

Sprint 6 (antes de EP-05 Carriers):
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
