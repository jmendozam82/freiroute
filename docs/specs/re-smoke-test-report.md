# Reporte de Re-Smoke Test — Freiroute TMS API

> **Fecha:** 2026-09-04/05 · **Ejecutado por:** Agente @PM (re-smoke test)
> **Servidor:** https://localhost:7062 (66 operaciones HTTP / 55 paths, swagger.json)
> **Alcance:** Secciones 2–6 del checklist de re-smoke (crear tenant, onboarding, usuarios/límites, configuración, 2FA)
> **Tipo:** RE-TEST — pueden existir residuos de ejecuciones previas.

---

## Resumen por sección

| Sección | ✅ | ⚠️ | ❌ |
|---|---|---|---|
| **2 — Crear tenant** | 2 | 2 | 0 |
| **3 — Onboarding wizard** | 6 | 4 | 2 |
| **4 — Usuarios/límites** | 2 | 1 | 2 |
| **5 — Configuración** | 3 | 1 | 2 |
| **6 — 2FA** | 1 | 3 | 1 |

**Veredicto global:** ❌ **NO APTO para aprobar el incremento.** Se detectaron 3 bugs de datos/BD críticos (columnas inexistentes, progreso de onboarding no persistente, límite de usuarios no aplicado al crear), 2 bloqueos ambientales (Supabase Storage 403, 2FA roto sin `TOTP_ENCRYPTION_KEY`) y 2 fallos de endpoints (GET/PUT /api/configuracion → 500).

---

## Sección 2 — Crear tenant (re-test)

### PASO 1 — POST /api/empresas — ✅ (Fix 1 OK)
- **HTTP 201** (esperado). Body enviado: `{"nombre":"Trans Demo S.A.","emailAdmin":"demo@transdemo.com","pais":"Nicaragua","planSuscripcion":"STARTER"}`.
- Evidencia (respuesta truncada): `"estado":"TRIAL"`, `"planSuscripcion":"STARTER"`, `"activo":true`, empresa `id=dd3b1d9a-d96d-47bf-b417-7df9eebd8fbd`.
- **[ok]** `empresa.estado == "TRIAL"` ✅ (Fix 1). **[ok]** HTTP 201 ✅.

### PASO 2 — GET /api/admin/suscripciones — ✅
- **HTTP 200**. Filtrada "Trans Demo S.A.":
  - `estado: "TRIAL"`, `estadoLabel: "Prueba"` ✅
  - `fechaInicio: 2026-09-05T01:55:43Z`, `fechaVencimiento: 2026-10-05T01:55:43Z` → hoy+30 ✅
  - `planNombre: "Starter"`, `planCodigo: "STARTER"`, `precioPactado: 99.00 USD`, `tipoCiclo: MENSUAL` ✅

### PASO 3 — GET /api/admin/empresas/{id} — ⚠️
- **HTTP 200**: `{"estado":"TRIAL","planSuscripcion":"STARTER","onboardingCompletado":false,"activo":true,"proximoVencimiento":"2026-10-05T..."}`.
- **[⚠]** El response `Freiroute.DTO.Admin.EmpresaResponseDto` **NO expone la lista de usuarios** (no existe el campo), por lo que la verificación "usuario demo existe / ADMIN / activo" se hizo por vía alternativa con **GET /api/usuarios** (JWT del tenant): `demo@transdemo.com → tipoUsuario:ADMIN, estado:ACTIVE, activo:true` ✅.

### PASO 4 — EMAIL STUB en api.log — ⚠️
- Encontrado en `api.log` (timestamp local):
  - `[19:55:43 WRN] SUPABASE AUTH STUB → signup simulado para demo@transdemo.com (TODO: llamada real en Sprint 2)`
  - `[19:55:43 INF] EMAIL STUB → Para: demo@transdemo.com | Asunto: Bienvenido a Freiroute - Tu cuenta de administrador`
- **[⚠]** La **contraseña temporal `Fr{XXXX}!` NO aparece en el log**: `EmailServiceStub.EnviarAsync` (Freiroute.BLL/Services/EmailServiceStub.cs:22-29) loguea solo `Destinatario` y `Asunto`, descartando el cuerpo. La generación queda en `EmpresaService.GenerarPasswordTemporal()` (línea 379-384: `Fr` + 4 dígitos + `!`).
- Mitigación usada en el re-test: el stub de Supabase Auth (`SupabaseAuthServiceStub`) acepta **cualquier password excepto `"wrong-password"`**, por lo que el login del PASO 5 fue posible con una contraseña de forma válida. **Riesgo de demo**: un tester sin acceso al código no puede extraer la password real del log.

---

## Sección 3 — Onboarding Wizard (re-test)

### PASO 5 — POST /api/auth/login (demo@transdemo.com) — ✅
- **HTTP 200**: `success:true`, `usuario.tipoUsuario:"ADMIN"`, `usuario.nombre:"Administrador de Trans Demo S.A."`, JWT con permisos completos (`configuracion:*`, `usuarios:*`, etc.).

### PASO 6 — Redirect / onboarding UI — ⚠️ (verificado por código + 1 llamada real)
- **[⚠]** Llamada real: `GET /` con `Accept: text/html` + JWT del tenant (onboarding sin completar) → **HTTP 307 → `https://localhost:7062/onboarding?paso=1`** (`OnboardingRedirectMiddleware.cs:64-66`, una sola llamada verifica el redirect).
- **[⚠]** La URL real de redirect es **`/onboarding?paso=1`** (query param), NO `/onboarding/paso/1`. El JS de las vistas del wizard usa `/onboarding/paso/{n}` (Paso1→2, Paso2→3, … Paso5→/dashboard). Naming inconsistente.
- **[ok]** Barra de progreso: la API devuelve `pasoActual:1, porcentajeCompletado:20` (GET /api/onboarding) → "20%" coherente con `_LayoutOnboarding.cshtml` ("Paso X de 5", `width:{Porcentaje}%`).
- **[ok]** Layout sin sidebar: `_LayoutOnboarding.cshtml` es header mínimo (logo Freiroute) + barra de progreso; no renderiza sidebar.
- **[⚠]** **No existe `OnboardingController` en la capa MVC** (`Freiroute.Aplicacion` solo tiene Home, Auth/Account y Admin/*). Las vistas del wizard (Areas/Onboarding/Views/Wizard/Paso1..5) **no son servibles** → el flujo por browser quedará en 404 si se navega directamente.

### PASO 7 — POST /api/onboarding/paso1 — ✅
- **HTTP 200** → `{"message":"Paso 1 guardado"}`. (El verb real es **POST**, no PUT como el checklist.)
- **[⚠]** `industria` enviada ("Logistica y transporte") **no persiste**: la columna `empresas.industria` NO existe (migración 20260101000002 + 20260201000006 no la crean) → `GET /api/onboarding` devuelve `datosPaso1.industria:null`. (Detalle en Riesgos #1.)

### PASO 8 — logo + paso2 — ✅/❌
- **[❌]** **POST /api/onboarding/logo** (multipart PNG 1×1 válido, campo `archivo`) → **HTTP 422** `"No se pudo subir el archivo al almacenamiento."`. Causa en log `[20:00:04 ERR]`: el storage local de Supabase responde `403 {"message":"signature verification failed","code":"AccessDenied"}` — la `Supabase:ServiceRoleKey` del appsettings (demo key, `iss:supabase-demo`) NO es válida contra el JWT secret del supabase local (`super-secret-jwt-token-with-at-least-32-characters-long`, verificado en el contenedor `supabase_auth_freiroute`). (Riesgo #4.)
- **[ok]** **POST /api/onboarding/paso2** `{"colorPrimario":"#1A73E8","colorSecundario":"#0B2545"}` → **HTTP 200** "Paso 2 guardado".

### PASO 9 — POST /api/onboarding/paso3 — ✅ (con ⚠️)
- **HTTP 200** → `{"message":"Paso 3 guardado"}`. Body: `moneda:USD, zonaHoraria:America/Managua, formatoFecha:DD/MM/YYYY, modosTransporteActivos:[FTL,LTL], prefijoEmbarque:TD, prefijoOrden:ORD`.
- **[ok]** `prefijoEmbarque` **TD** persistió en BD (verificado con psql: `prefijo_embarque=TD`).
- **[⚠]** `modosTransporteActivos` **NO persiste** (columna `modos_transporte` no existe) → `datosPaso3.modosTransporteActivos:[]` en GET /api/onboarding.
- **[ok]** Validadores: `INTERMODAL` es opción válida (lista `FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL` en `OnboardingPaso3Validator.cs:13-16`); `MULTIMODAL` NO existe → error de validación "Un modo de transporte no es válido".
- **[ok]** Preview por código (`Paso3.cshtml:166-171`): `` `${val || 'FR'}-${anio}-00001` `` → con prefijo TD y anio 2026 → **`TD-2026-00001`** ✅ (consecutivo fijo `00001` en el onboarding).

### PASO 10 — POST /api/onboarding/paso4 — ✅
- **HTTP 200** → `{"message":"Paso 4 guardado"}`. Body: `nombreCompleto:"Admin Demo", telefono:"+505 8888-0000", cambiarPassword:false`.
- Confirmado por efecto colateral: en el login siguiente (PASO 24) el JWT muestra `"nombre":"Admin Demo"` → el paso 4 **sí actualizó** el nombre del usuario admin (vía `UsuarioService.UpdateAsync`).

### PASO 11 — paso5 + completar + dashboard — ✅/❌
- **[ok]** **POST /api/onboarding/paso5** `{"invitaciones":[{"email":"operador@transdemo.com","perfilId":"eceaa77b-…"}]}` → **HTTP 200** "Paso 5 guardado". EMAIL STUB en log: `[20:01:43] EMAIL STUB → Para: operador@transdemo.com | Asunto: Invitación a Freiroute TMS`. El usuario quedó `PENDING`.
- **[ok]** **POST /api/onboarding/completar** → **HTTP 200** `{"message":"Onboarding completado"}`.
- **[❌]** **El progreso NO persiste**: tras completar, `GET /api/onboarding` sigue en `pasoActual:1, porcentajeCompletado:20, completado:false`. Confirmado en BD: `onboarding_paso_actual=1, onboarding_completado=f`. Causa raíz: `EmpresaRepository.UpdateAsync` (EmpresaRepository.cs:206-234) **no escribe** `onboarding_paso_actual` ni `onboarding_completado`. (Riesgo #2.)
- **[⚠]** Redirect final por código: `Paso5.cshtml:117,119,143` → `window.location.href = '/dashboard'` tras completar. No se pudo ejecutar en browser (sin navegador).
- **[ok]** KPIs "--" por código: `Views/Home/Index.cshtml:22-43` (Embarques hoy `--`, OTD del mes `--`, En tránsito `--`, Carriers activos `--`, con nota "Disponible en Sprint 7"). Sidebar TENANT: `_Layout.cshtml` (Dashboard, Operación, Planificación, Inteligencia, Administración).

---

## Sección 4 — Usuarios y límites de plan (re-test)

### PASO 12 — GET /api/usuarios — ⚠️
- **HTTP 200**. Estado inicial tras el onboarding completo: **2 usuarios con `activo=true`** (no "1"):
  - `demo@transdemo.com — ACTIVE` (único ACTIVE) ✅
  - `operador@transdemo.com — PENDING` (cuenta como activa para el límite: `UsuarioRepository.GetAllAsync` filtra solo `activo=true`).
- **[⚠]** El contador del límite arranca en **2/5**, no 1/5, porque la invitación del PASO 11 ya ocupa un slot del plan Starter (5).

### PASO 13 — Crear 4 usuarios — ✅
- **POST /api/usuarios** (verb real POST; el checklist decía indistinto POST/invitar) con perfil Operador → `usuario1..4@test.com` → **201 cada uno** (estado PENDING). Conteo total posterior: 6 activos.

### PASO 14 — Crear 6to usuario — ❌ (bug)
- **Esperado:** HTTP 422 con mensaje de límite.
- **Actual:** POST /api/usuarios `usuario5@test.com` → **HTTP 201** (`{"message":"Usuario creado exitosamente"}`). Conteo: 7 activos (límite Starter = 5).
- **Causa raíz:** `PlanLimiteService.VerificarLimiteUsuariosAsync` se invoca SOLO en `UsuarioService.ReactivarAsync` (UsuarioService.cs:202). **No se invoca en `CreateAsync` ni en `InvitarAsync`** → el límite no se aplica al alta. (Riesgo #3.)

### PASO 15 — Deactivate / Reactivate — ⚠️
- **[ok]** **PATCH /api/usuarios/{id}/deactivate** (verb real PATCH, no PUT) usuario4 → **HTTP 200** `"Usuario desactivado"`. Conteo: 6 activos.
- **[❌]** **PATCH /api/usuarios/{id}/reactivate** usuario4 → **HTTP 422** `"Se alcanzó el límite de usuarios del plan Starter (5). Considere mejorar al plan Professional."` (esperado 200). Causa: el conteo era 6/5 por el bug del PASO 14 (la creación no validó el límite).
- **[ok]** Con conteo 4/5 (tras desactivar además u2 y u3), el reactivate de usuario4 → **HTTP 200 con `UsuarioResponseDto` completo** (`estado:ACTIVE, activo:true`, `"message":"Usuario reactivado"`). El endpoint devuelve el DTO, no bool ✅. El enforcement del límite en reactivate funciona correctamente.

---

## Sección 5 — Configuración del tenant (re-test)

### PASO 16 — /tenant/configuracion (4 tabs) — ✅ (por inspección de código)
- `Areas/Tenant/Views/Configuracion/Index.cshtml:23-37`: **4 tabs** — "Datos generales", "Identidad visual", "Operación", "Numeración" (equivalen a Datos, Visual, Operación, Numeración del checklist).
- **[⚠]** Deuda: **no existe `TenantController`** en la capa MVC → la ruta `/tenant/configuracion` no es servible (solo existe la vista).

### PASO 17 — PUT /api/configuracion — ❌ (bug)
- **Esperado:** HTTP 200 (cambio de teléfono).
- **Actual:** **HTTP 500** `{"message":"Ocurrió un error interno en el servidor"}` para GET y PUT. Log: `[20:08:08 ERR]` / `[20:09:00 ERR] Excepción no controlada en /api/configuracion - 42703: column "industria" does not exist`.
- **Causa raíz:** `ConfiguracionRepository.GetConfiguracionAsync`/`UpdateConfiguracionAsync` referencian columnas que **no existen** en `empresas`: `industria`, `sitio_web`, `email_remitente`, `nombre_remitente` (ninguna migración las crea; verificado contra information_schema). (Riesgo #1.)

### PASO 18 — Color primario en tiempo real — ⚠️
- Por código (`Index.cshtml:424-452`): el handler `btnGuardarColores` + listeners de color pickers actualizan `previewMarkLeft` / `previewLogoBox` (preview del sidebar) en tiempo real con `document...style.background = e.target.value`.
- **[⚠]** El preview funciona client-side, pero **guardar falla** (PUT /api/configuracion → 500, PASO 17). No se pudo verificar en browser.

### PASO 19 — PUT /api/configuracion/numeracion — ✅
- **HTTP 200**: `{"prefijoEmbarque":"TDS","consecutivoEmbarque":1,...}` (prefijo TD → TDS). GET numeracion confirma TDS persistido.
- **[ok]** Preview por código (`Index.cshtml:405-412`): `` `${val||'FR'}-${anio}-${String(cons).padStart(5,'0')}` `` con cons=1 y anio=2026 → **`TDS-2026-00001`** ✅.
- **[⚠]** Inconsistencia menor: la **tabla** de numeración muestra `consecutivo+1` (`String((d.ConsecutivoEmbarque||0)+1)...`) → "TDS-2026-00002", mientras el preview del input usa `consecutivo` → "TDS-2026-00001".

### PASO 20 — Logo (subir / eliminar) — ❌/✅
- **[❌]** **POST /api/configuracion/logo** → **HTTP 422** `"No se pudo subir el archivo al almacenamiento."` (misma causa storage del PASO 8; log `[20:09:21 ERR]`).
- **[ok]** **DELETE /api/configuracion/logo** → **HTTP 200** `"Logo eliminado"` (idempotente sin logo previo). El borrado del preview dependería de un logo subido previamente — no verificable por el fallo del POST.

---

## Sección 6 — 2FA (re-test)

### PASO 21 — /account/perfil: badge "Sin 2FA" + botón — ✅ (por inspección de código)
- `Views/Account/Perfil.cshtml:51-64`: con `ViewData["Tiene2FA"]==false` renderiza badge **"Sin 2FA"** (rojo) y botón **"Activar 2FA"** → `window.location.href='/account/2fa/setup'`.
- **[⚠]** Deuda: no existe `AccountController` raíz en la capa MVC que sirva `/account/perfil` (las vistas raíz `Views/Account/*` no tienen controller).

### PASO 22 — POST /api/auth/2fa/setup — ✅
- **HTTP 200**. (El verb real es **POST**, no GET como el checklist.)
- Evidencia: `secret:"OQJ5VXG2BPO7V7G7CZLFT472PND6X4OU"` (base32, 20 bytes), `qrCodeUrl:"otpauth://totp/Freiroute%20TMS:demo%40transdemo.com?...&algorithm=SHA1&digits=6&period=30"`, **`codigosRecuperacion: [8 códigos]`** (se muestran UNA vez en el setup).

### PASO 23 — Calcular TOTP + activar + recovery-codes — ❌
- **TOTP** calculado en PowerShell (RFC 6238, HMACSHA1, 6 dígitos, step 30, window ±1) — implementación estándar; código válido para el secret dado.
- **POST /api/auth/2fa/activar** `{"tipo":"TOTP","codigo":"<totp>"}` → **HTTP 500** `"Ocurrió un error interno en el servidor"`.
- **Causa raíz (log `[20:10:04 ERR]`):** `System.Security.SecurityException: Error de descifrado` en `AesGcmEncryptor.Decrypt` (AuthService.cs:476). Con `Security:TotpEncryptionKey=""` (appsettings) el constructor usa `RandomKeyFallback()` (AuthService.cs:77-79, 741-745): clave aleatoria **por instancia**. Como `AuthService` es **scoped** (DependencyInjection.cs:74), el setup cifra el secret con la clave de su instancia y el activar descifra con OTRA instancia → tag GCM inválido → SecurityException. **El flujo 2FA es imposible de completar sin `TOTP_ENCRYPTION_KEY`** (contrario a la premisa del checklist de que no se necesitaba). (Riesgo #5.)
- **GET /api/auth/2fa/recovery-codes** → **HTTP 422** (documentado en swagger: "SIEMPRE retorna 422"): `"Los códigos de recuperación solo se muestran al activar 2FA..."`. [⚠] El checklist esperaba 8 códigos aquí; el diseño real los entrega una sola vez en el setup (PASO 22), y en la BD solo se guardan hashes.

### PASO 24 — Login con 2FA pendiente — ⚠️
- **HTTP 200** (esperado 202 + TempToken **solo si** el 2FA estuviera activado). Como el activar falla (PASO 23), el usuario quedó `TotpHabilitado=false` y el login completa normal. El flujo 202 no es alcanzable hasta corregir Riesgo #5.

### PASO 25 — verify 2FA + sesión funcional — ⚠️
- Happy path **no ejecutable**: requiere 2FA activado (bloqueado por PASO 23).
- El endpoint **POST /api/auth/2fa/verify** existe y responde coherentemente: con `tempToken` inválido → **HTTP 422** `"Sesión 2FA inválida o expirada. Vuelva a iniciar sesión."`.
- Sesión funcional equivalente verificada: múltiples GET autenticados con el JWT del tenant admin respondieron 200 durante todo el re-test (`/api/usuarios`, `/api/onboarding`, `/api/configuracion/numeracion`, `/api/perfiles`).

---

## Errores en consola del servidor (api.log)

Lista completa de `ERR` generados durante el re-test (Serilog escribe en `api.log`; `api.err.log` está vacío):

| Timestamp (local) | Endpoint | Error |
|---|---|---|
| 20:00:04 | POST /api/onboarding/logo | `Supabase Storage subida fallida: BadRequest - {"statusCode":"403","error":"Unauthorized","message":"signature verification failed","code":"AccessDenied"}` |
| 20:08:08 | GET /api/configuracion | `42703: column "industria" does not exist` |
| 20:09:00 | PUT /api/configuracion | `42703: column "industria" of relation "empresas" does not exist` |
| 20:09:21 | POST /api/configuracion/logo | `Supabase Storage subida fallida ... 403 signature verification failed` |
| 20:10:04 | POST /api/auth/2fa/activar | `System.Security.SecurityException: Error de descifrado` (AesGcmEncryptor.Decrypt:117 ← AuthService.Activar2faAsync:476) — stack completo en log |
| 20:12:08 | GET /api/configuracion | `42703: column "industria" does not exist` (re-verificación) |

Warnings esperados (stub): `SUPABASE AUTH STUB → login/signup simulado` (no son errores).

---

## Riesgos y observaciones para el equipo

1. **CRÍTICO — Faltan columnas en la tabla `empresas`.** El código (Entity `Empresa`, DTOs, `OnboardingService`, `ConfiguracionRepository`) referencia `industria`, `modos_transporte`, `sitio_web`, `email_remitente`, `nombre_remitente`, pero ninguna migración las crea (verificado: migraciones 0002 y 20260201000006 + `information_schema`). Consecuencias: GET/PUT `/api/configuracion` → 500; `industria` (paso1) y `modosTransporte` (paso3) no persisten. **Acción:** nueva migración `ALTER TABLE empresas ADD COLUMN ...` + corregir `EmpresaRepository` SELECT/UPDATE.
2. **CRÍTICO — `EmpresaRepository.UpdateAsync` no persiste `onboarding_paso_actual` ni `onboarding_completado`** (tampoco `prefijo_carta_porte`/`consecutivo_carta_porte`). El wizard "guarda" y "completa" con 200 pero la BD nunca avanza (paso_actual=1, completado=f tras ejecutar los 5 pasos + completar). **Acción:** añadir las columnas al UPDATE y al SELECT de `GetByIdAsync/GetAllAsync/GetByEmailAdminAsync`.
3. **ALTO — Límite de usuarios no se aplica al crear/invitar.** `VerificarLimiteUsuariosAsync` solo se llama en `ReactivarAsync`. Un tenant Starter puede superar 5 usuarios sin 422. **Acción:** invocar el chequeo en `CreateAsync` e `InvitarAsync` (y en onboarding paso 5).
4. **ALTO — Logo upload roto en dev (403).** `Supabase:ServiceRoleKey` (demo key `iss:supabase-demo`) no valida contra el JWT secret del supabase local (`super-secret-jwt-token-with-at-least-32-characters-long`). **Acción:** generar keys locales válidas (o mover a `secrets` del CLI) y verificar el bucket `logos-tenants`.
5. **ALTO — 2FA imposible sin `TOTP_ENCRYPTION_KEY`.** Con la key vacía, `RandomKeyFallback()` produce una clave distinta por instancia scoped → el `activar` nunca descifra lo que `setup` cifró. **Acción:** configurar `TOTP_ENCRYPTION_KEY` (base64 de 32 bytes) en appsettings/env, y considerar validar la key al arranque.
6. **Naming inconsistente de endpoints vs checklist/swagger:** pasos de onboarding son **POST** (no PUT); `/api/auth/2fa/setup` es **POST** (no GET); `deactivate/reactivate` son **PATCH** (no PUT); `GET /api/auth/2fa/recovery-codes` siempre 422 por diseño; redirect del middleware `/onboarding?paso=1` (query) vs JS de vistas `/onboarding/paso/{n}` (path).
7. **Capa MVC incompleta (deuda de UI):** existen vistas de Onboarding (Paso1-5), Tenant (Configuracion, Usuarios) y raíz (Account: Perfil/Setup2fa/Verify2fa) **sin controllers** que las sirvan (`OnboardingController`, `TenantController`, `AccountController` raíz) → rutas 404 en el navegador. Solo el flujo API es completo.
8. **Menor — Email stub no loguea el cuerpo**: la contraseña temporal del alta de tenant no es observable en logs (dificulta demos end-to-end manuales).
9. **Menor — Preview de numeración inconsistente:** la tabla muestra `consecutivo+1` ("TDS-2026-00002") y el preview del input usa `consecutivo` ("TDS-2026-00001").
10. **Nota de diseño:** `GET /api/admin/empresas/{id}` no incluye los usuarios del tenant (la verificación de "usuario ADMIN activo" requiere `GET /api/usuarios` con JWT del tenant o impersonación).

---

*Generado automáticamente por el agente @PM — re-smoke test 2026-09-04/05.*
*Próximo paso sugerido: corregir Riesgos #1–#5 (bloqueantes), re-ejecutar secciones 3–6, y luego re-correr CI con cobertura.**

---

# POST-FIX VERIFICATION — 2026-09-04 (Sprint 3)

## Resumen

| # | Bloqueante | Estado | Evidencia |
|---|---|---|---|
| 1 | Columnas faltantes en `empresas` | ✅ | Migración `20260202000001_agregar_columnas_empresa_configuracion.sql` aplicada; `information_schema` confirma las 5 columnas; GET/PUT `/api/configuracion` → 200 |
| 2 | Wizard no persiste avance | ✅ | `ActualizarOnboardingAsync` + llamadas en `OnboardingService`; tras los 5 pasos → `paso_actual=5`, `completado=t` en BD |
| 3 | Límite de plan al crear/invitar | ✅ | 6to usuario → **422** "Se alcanzó el límite de usuarios del plan Starter (5)" |
| 4 | Storage 403 (logo) | ✅ | Upload OK (200) + **fix adicional**: body JSON `{"expiresIn": N}` en `/object/sign` (antes 400 "body must be object"); signed URL devuelta y persistida en `logo_url` |
| 5 | 2FA cifrado | ✅ | `TOTP_ENCRYPTION_KEY` en appsettings.Development.json y `RandomKeyFallback` eliminado; activar → **200**; login 2FA → **202** + verify → **200** JWT |

## Detalle de ejecución

### Prerequisitos
- `dotnet build Freiroute.sln` → **0 errores / 0 warnings**
- `dotnet test Freiroute.sln` → **438 superados / 0 fallos** (324 BLL + 114 API; antes 431)
- Servidor reiniciado con clave TOTP configurada → arranque sin error, `VencimientoSuscripcionJob iniciado`

### Smoke post-fix (empresa nueva: "Trans Demo PostFix")

| Paso | Resultado | Evidencia |
|---|---|---|
| POST /api/empresas | ✅ 201 | `estado=TRIAL` (Fix 1 del smoke original intacto) |
| Log EMAIL STUB | ✅ | "Para: demo.postfix@transdemo.com" · "Contraseña temporal: **Fr2397!**" (Fix 7) |
| POST /api/auth/login (password temporal) | ✅ 200 | `tipo_usuario=ADMIN`, JWT emitido |
| POST /api/onboarding/paso1 | ✅ 200 | `pasoActual` pasó de 1 → **2** (Fix 2) |
| POST /api/onboarding/logo (PNG 1×1) | ✅ 200 | signed URL generada (Fix 4 + fix body `/sign`) |
| POST /api/onboarding/paso2/3/4/5 + completar | ✅ 200 | estado final: `pasoActual=5, completado=true, 100%` |
| BD: `modos_transporte_activos` | ✅ | `{FTL,LTL,INTERMODAL}` persistido (Fix 8) |
| BD: `prefijo_embarque` | ✅ | `TD` persistido |
| 4× POST /api/usuarios (→5 activos) | ✅ 201 ×4 | contador llega a límite Starter |
| 6to POST /api/usuarios | ✅ 422 | "Se alcanzó el límite de usuarios del plan Starter (5)" (Fix 3) |
| POST /api/auth/2fa/setup | ✅ 200 | secret + QR + 8 recovery codes |
| POST /api/auth/2fa/activar (TOTP calculado RFC 6238) | ✅ 200 | "2FA activado" — sin error de descifrado (Fix 5) |
| POST /api/auth/login con 2FA activo | ✅ **202** | `requires2fa: true` + `tempToken` |
| POST /api/auth/2fa/verify | ✅ 200 | JWT completo; GET /api/configuracion → 200 con columnas nuevas |

## Fixes adicionales detectados durante la verificación
- **SupabaseStorageService.GetSignedUrlAsync**: el POST a `/object/sign` se enviaba **sin body JSON** → Supabase respondía 400 `body must be object`. Se añadió `Content = {"expiresIn": N}` tipo `application/json`. (Este era el error real detrás del 422 de logo; la parte 403 ya se resolvió con la `ServiceRoleKey` de `supabase status`.)

## Archivos modificados/creados en esta ronda
- `supabase/migrations/20260202000001_agregar_columnas_empresa_configuracion.sql` (nuevo)
- `src/Freiroute.DAL/Interfaces/IEmpresaRepository.cs`, `Repositories/EmpresaRepository.cs`
- `src/Freiroute.DAL/Interfaces/IConfiguracionRepository.cs`, `Repositories/ConfiguracionRepository.cs`
- `src/Freiroute.BLL/Services/OnboardingService.cs`, `UsuarioService.cs`, `ConfiguracionService.cs`, `EmailServiceStub.cs`, `SupabaseStorageService.cs`, `AuthService.cs`
- `src/Freiroute.Utility/Security/AesGcmEncryptor.cs`
- `src/Freiroute.API/Program.cs`, `Middleware/OnboardingRedirectMiddleware.cs`
- `src/Freiroute.Aplicacion/Program.cs`, `Areas/Onboarding/Controllers/WizardController.cs` (nuevo), `Areas/Auth/Controllers/AccountController.cs`
- `src/Freiroute.DTO/Configuracion/ConfiguracionResponseDto.cs`, `src/Freiroute.Entity/Empresa.cs`
- `tests/Freiroute.BLL.Tests/Services/AuthServiceTests.cs`, `OnboardingServiceTests.cs`, `UsuarioServiceTests.cs`
- `.gitignore` → `**/appsettings.Development.json` (secrets fuera de git; archivo retirado del índice)
- `src/Freiroute.API/appsettings.Development.json` (NO trackeado: Supabase keys + `Security:TotpEncryptionKey`)

## Riesgos residuales
1. **Deuda de UI (persistente):** los controllers MVC del wizard existen, pero la validación/estilo de las vistas nuevas requiere revisión visual; el flag para redirigir al wizard tras login (307 → `/onboarding/paso/{n}`) quedó corregido en middleware.
2. **Endpoint naming:** los pasos del wizard son **POST** (`/api/onboarding/paso1..5`) y `deactivate/reactivate` son PATCH — documentado, no es defecto.
3. **Bucket `logos-tenants`** debe existir en cualquier entorno (dev/prod) antes de subir logos; en local se creó vía Supabase Studio.
4. ~~Columna carta porte pendiente~~ **RESUELTO (verificado):** `prefijo_carta_porte`, `consecutivo_carta_porte`, `prefijo_orden`, `consecutivo_orden`, `zona_horaria`, `formato_fecha`, `consecutivo_embarque` existen en BD (`information_schema` confirmado) y están incluidos en las queries de `ConfiguracionRepository`/`EmpresaRepository` (líneas 52-54, 163-177). GET/PUT `/api/configuracion` → 200 con datos completos (incluye `onboarding_completado`, `industria`, `logoUrl`).

*Cierre: re-smoke post-fix ejecutado por @PM — 2026-09-04. Todos los bloqueantes resueltos; servidor activo en https://localhost:7062 (PID 18740).*