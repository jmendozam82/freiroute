# Spec: Sprint 2 — EP-02 Administración SaaS & Tenants

**Sprint:** 02
**Épica:** EP-02 — Administración SaaS & Gestión de Tenants
**Historias:** HU-004 · HU-005 · HU-009 · HU-010 · HU-011 · HU-012 · HU-013 · HU-014
**Story Points:** ~94 pts
**Objetivo del Sprint:** Tener el panel de administración SaaS completo,
el onboarding de tenants funcional, la gestión de usuarios del tenant
operativa, y la autenticación reforzada con OAuth y 2FA.
**ADRs aplicables:** ADR-003 · ADR-004 · ADR-009 · ADR-010

---

## Dependencias

```
Sprint 1 (completado) → base de auth, tenants, perfiles, permisos
Sprint 2 (este)       → base para Sprint 3 (Maestros y Catálogos)

HU-004 y HU-005 dependen de Supabase Auth (ya configurado en Sprint 1)
HU-012 dependen de HU-010 (planes deben existir antes del wizard)
HU-013 dependen de HU-001 Sprint 1 (usuarios ya modelados)
```

---

## Orden de Implementación

```
1. @Arquitecto   → Entities + DTOs + Interfaces (todas las HU)
2. @IngenieroDatos → Migraciones + Repositorios
3. @BackendDev   → BLL Services + API Controllers
                   (incluye HU-004 OAuth y HU-005 2FA)
4. @QA           → Tests BLL ≥80% + API ≥60%
5. @FrontendDev  → Vistas del sprint
```

---

## Tablas de Base de Datos del Sprint 2

### Tabla: `planes`

```sql
CREATE TABLE IF NOT EXISTS planes (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre                  VARCHAR(100) NOT NULL,
    codigo                  VARCHAR(50)  NOT NULL UNIQUE,
    descripcion             TEXT,
    -- Límites operativos
    limite_usuarios         INTEGER NOT NULL DEFAULT 5,
    limite_embarques_mes    INTEGER NOT NULL DEFAULT 500,
    limite_storage_gb       INTEGER NOT NULL DEFAULT 1,
    -- Precio
    precio_mensual          NUMERIC(10,2) NOT NULL DEFAULT 0,
    precio_anual            NUMERIC(10,2) NOT NULL DEFAULT 0,
    moneda                  VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Módulos disponibles (array de strings)
    modulos_disponibles     TEXT[] NOT NULL DEFAULT '{}',
    -- Control
    es_publico              BOOLEAN NOT NULL DEFAULT true,
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ
);

COMMENT ON TABLE planes IS
    'Planes de suscripción del SaaS Freiroute. Gestionados por Super Admin.';
COMMENT ON COLUMN planes.codigo IS
    'Código único del plan: STARTER, PROFESSIONAL, ENTERPRISE';
COMMENT ON COLUMN planes.limite_usuarios IS
    '-1 significa ilimitado';
COMMENT ON COLUMN planes.limite_embarques_mes IS
    '-1 significa ilimitado';
COMMENT ON COLUMN planes.modulos_disponibles IS
    'Array con los códigos de módulos disponibles para este plan';

CREATE INDEX idx_planes_codigo  ON planes(codigo);
CREATE INDEX idx_planes_activo  ON planes(activo);

-- NO tiene empresa_id — es catálogo global del SaaS
-- NO tiene RLS — solo el Super Admin la gestiona

CREATE TRIGGER trg_planes_fecha_modificacion
    BEFORE UPDATE ON planes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

-- Datos iniciales de planes
INSERT INTO planes (nombre, codigo, descripcion,
    limite_usuarios, limite_embarques_mes, limite_storage_gb,
    precio_mensual, precio_anual,
    modulos_disponibles) VALUES
(
    'Starter', 'STARTER',
    'Ideal para empresas de transporte pequeñas',
    5, 500, 1, 99.00, 990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace','documentos']
),
(
    'Professional', 'PROFESSIONAL',
    'Para empresas en crecimiento con operaciones medianas',
    25, 5000, 10, 299.00, 2990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace',
          'documentos','analytics','facturacion','clientes','flota']
),
(
    'Enterprise', 'ENTERPRISE',
    'Para grandes operaciones de transporte sin límites',
    -1, -1, 100, 799.00, 7990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace',
          'documentos','analytics','facturacion','clientes',
          'flota','usuarios','configuracion']
)
ON CONFLICT (codigo) DO NOTHING;
```

---

### Tabla: `suscripciones`

```sql
CREATE TABLE IF NOT EXISTS suscripciones (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    plan_id                 UUID NOT NULL REFERENCES planes(id) ON DELETE RESTRICT,
    -- Ciclo de facturación
    tipo_ciclo              VARCHAR(20) NOT NULL DEFAULT 'MENSUAL',
    fecha_inicio            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_vencimiento       TIMESTAMPTZ NOT NULL,
    fecha_cancelacion       TIMESTAMPTZ,
    -- Estado
    estado                  VARCHAR(50) NOT NULL DEFAULT 'TRIAL',
    -- Precio pactado al contratar (puede diferir del plan actual)
    precio_pactado          NUMERIC(10,2) NOT NULL DEFAULT 0,
    moneda_pactada          VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    creado_por_id           UUID REFERENCES usuarios(id),
    CONSTRAINT uq_suscripcion_empresa_activa
        UNIQUE (empresa_id, activo) DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE suscripciones IS
    'Suscripciones activas de cada empresa. Una empresa tiene una suscripción activa.';
COMMENT ON COLUMN suscripciones.tipo_ciclo IS
    'MENSUAL, ANUAL';
COMMENT ON COLUMN suscripciones.estado IS
    'TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED';
COMMENT ON COLUMN suscripciones.precio_pactado IS
    'Precio negociado al contratar — puede diferir del precio actual del plan';

CREATE INDEX idx_suscripciones_empresa_id       ON suscripciones(empresa_id);
CREATE INDEX idx_suscripciones_plan_id          ON suscripciones(plan_id);
CREATE INDEX idx_suscripciones_estado           ON suscripciones(estado);
CREATE INDEX idx_suscripciones_vencimiento      ON suscripciones(fecha_vencimiento);
CREATE INDEX idx_suscripciones_activo           ON suscripciones(activo);

-- Sin RLS — el Super Admin gestiona todas las suscripciones

CREATE TRIGGER trg_suscripciones_fecha_modificacion
    BEFORE UPDATE ON suscripciones
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `pagos`

```sql
CREATE TABLE IF NOT EXISTS pagos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    suscripcion_id      UUID NOT NULL REFERENCES suscripciones(id) ON DELETE RESTRICT,
    -- Datos del pago
    monto               NUMERIC(10,2) NOT NULL,
    moneda              VARCHAR(10) NOT NULL DEFAULT 'USD',
    metodo_pago         VARCHAR(50) NOT NULL DEFAULT 'MANUAL',
    referencia          VARCHAR(200),
    notas               TEXT,
    -- Estado
    estado              VARCHAR(50) NOT NULL DEFAULT 'COMPLETED',
    -- Período cubierto
    periodo_desde       TIMESTAMPTZ NOT NULL,
    periodo_hasta       TIMESTAMPTZ NOT NULL,
    -- Control
    registrado_por_id   UUID REFERENCES usuarios(id),
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- Sin fecha_modificacion — los pagos son inmutables
);

COMMENT ON TABLE pagos IS
    'Registro de pagos de suscripción. Inmutable — no se editan ni eliminan.';
COMMENT ON COLUMN pagos.metodo_pago IS
    'MANUAL, STRIPE, PAYPAL, TRANSFERENCIA, EFECTIVO';
COMMENT ON COLUMN pagos.estado IS
    'COMPLETED, PENDING, FAILED, REFUNDED';

CREATE INDEX idx_pagos_empresa_id     ON pagos(empresa_id);
CREATE INDEX idx_pagos_suscripcion_id ON pagos(suscripcion_id);
CREATE INDEX idx_pagos_fecha          ON pagos(fecha_creacion DESC);
```

---

### Tabla: `configuracion_2fa`

```sql
CREATE TABLE IF NOT EXISTS configuracion_2fa (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    -- TOTP
    totp_secret         VARCHAR(500),
    totp_habilitado     BOOLEAN NOT NULL DEFAULT false,
    -- Email 2FA
    email_habilitado    BOOLEAN NOT NULL DEFAULT false,
    -- Códigos de recuperación (almacenados como hashes)
    codigos_recuperacion TEXT[],
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_2fa_usuario UNIQUE (usuario_id)
);

COMMENT ON TABLE configuracion_2fa IS
    '2FA por usuario. El totp_secret se almacena cifrado.';
COMMENT ON COLUMN configuracion_2fa.totp_secret IS
    'Secret TOTP cifrado con AES-256. Nunca se almacena en claro.';
COMMENT ON COLUMN configuracion_2fa.codigos_recuperacion IS
    'Array de 8 hashes SHA-256 de códigos de un solo uso.';

CREATE INDEX idx_2fa_empresa_id  ON configuracion_2fa(empresa_id);
CREATE INDEX idx_2fa_usuario_id  ON configuracion_2fa(usuario_id);

ALTER TABLE configuracion_2fa ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_2fa" ON configuracion_2fa
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_2fa_fecha_modificacion
    BEFORE UPDATE ON configuracion_2fa
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `codigos_2fa_temporales`

```sql
CREATE TABLE IF NOT EXISTS codigos_2fa_temporales (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    codigo_hash         VARCHAR(500) NOT NULL,
    tipo                VARCHAR(20) NOT NULL DEFAULT 'EMAIL',
    usado               BOOLEAN NOT NULL DEFAULT false,
    fecha_expiracion    TIMESTAMPTZ NOT NULL,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE codigos_2fa_temporales IS
    'Códigos 2FA de un solo uso enviados por email. Expiran en 10 minutos.';

CREATE INDEX idx_2fa_temp_usuario ON codigos_2fa_temporales(usuario_id);
CREATE INDEX idx_2fa_temp_expira  ON codigos_2fa_temporales(fecha_expiracion);
```

---

### Columnas adicionales en tabla `empresas`

```sql
-- Migración: agregar campos de onboarding y plan
ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS plan_id
        UUID REFERENCES planes(id) ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS onboarding_paso_actual
        INTEGER NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS onboarding_completado
        BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS estado
        VARCHAR(50) NOT NULL DEFAULT 'TRIAL';

COMMENT ON COLUMN empresas.onboarding_paso_actual IS
    'Paso actual del wizard: 1-5. 0 = no iniciado.';
COMMENT ON COLUMN empresas.onboarding_completado IS
    'true cuando el Admin completó el wizard de configuración inicial';
COMMENT ON COLUMN empresas.estado IS
    'TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED';

-- Vincular empresa raíz al plan ENTERPRISE
UPDATE empresas
SET plan_id = (SELECT id FROM planes WHERE codigo = 'ENTERPRISE'),
    estado = 'ACTIVE',
    onboarding_completado = true
WHERE id = '00000000-0000-0000-0000-000000000001';
```

---

## Historias de Usuario del Sprint

---

### HU-004 · Autenticación OAuth 2.0 — Diferida de Sprint 1

**Como** usuario, **quiero** iniciar sesión con mi cuenta de Google o
Microsoft, **para** acceder sin gestionar otra contraseña.

**Criterios de aceptación:**
- [ ] CA-01: Botón "Continuar con Google" en Login redirige al flujo OAuth de Supabase Auth
- [ ] CA-02: Botón "Continuar con Microsoft" funcional de igual forma
- [ ] CA-03: Si el email OAuth ya existe en `usuarios` → vincular `supabase_user_id` y hacer login
- [ ] CA-04: Si el email OAuth es nuevo → crear usuario con estado `ACTIVE` y perfil base del tenant
- [ ] CA-05: El token resultante es el JWT interno de Freiroute con los mismos claims de HU-003
- [ ] CA-06: Login OAuth registrado en `auditoria_actividad` con acción `LOGIN_OAUTH`

**Implementación:**
- Supabase Auth maneja el OAuth externamente
- El callback llega a `POST /api/auth/oauth/callback`
- El endpoint recibe el `access_token` de Supabase, valida con Supabase Auth,
  resuelve o crea el usuario en la tabla `usuarios` y emite el JWT interno

**Endpoints API:**
```
POST /api/auth/oauth/callback
Body: { provider: "google" | "microsoft", supabaseToken: "..." }
Response: LoginResponseDto (mismo que HU-003)
```

**Estimación:** 5 pts | **Asignado a:** @BackendDev → @FrontendDev

---

### HU-005 · Autenticación de Dos Factores (2FA) — Diferida de Sprint 1

**Como** administrador, **quiero** habilitar 2FA para usuarios del sistema,
**para** reforzar la seguridad de acceso.

**Criterios de aceptación:**
- [ ] CA-01: El usuario puede activar 2FA TOTP desde su perfil — sistema genera QR con el secret
- [ ] CA-02: El usuario puede activar 2FA por email (código 6 dígitos, válido 10 min)
- [ ] CA-03: El Admin puede hacer 2FA obligatorio para todos los usuarios del tenant
- [ ] CA-04: Al activar 2FA → generar 8 códigos de recuperación de un solo uso
- [ ] CA-05: Si 2FA falla 3 veces consecutivas → cerrar sesión y registrar en auditoría
- [ ] CA-06: El usuario puede desactivar 2FA solo tras verificar el código actual

**Flujo de login con 2FA activo:**
```
1. POST /api/auth/login → credenciales OK → si 2FA activo:
   Response: { requires2fa: true, tempToken: "..." } (HTTP 202)
2. POST /api/auth/2fa/verify
   Body: { tempToken, codigo }
   Response: LoginResponseDto completo (HTTP 200)
```

**Endpoints API:**
```
GET  /api/auth/2fa/setup          → genera QR TOTP para el usuario autenticado
POST /api/auth/2fa/activate       → activa 2FA con primer código válido
POST /api/auth/2fa/verify         → verifica código en flujo de login
POST /api/auth/2fa/deactivate     → desactiva 2FA (requiere código actual)
GET  /api/auth/2fa/recovery-codes → obtiene los códigos de recuperación
POST /api/auth/2fa/recovery-codes/regenerate → regenera los 8 códigos
```

**Tabla BD:** `configuracion_2fa` + `codigos_2fa_temporales`

**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev

---

### HU-009 · Panel de Administración Global (Super Admin)

**Como** Super Admin de Freiroute, **quiero** un panel central de gestión
de todos los tenants, **para** monitorear y administrar la plataforma completa.

**Criterios de aceptación:**
- [ ] CA-01: Dashboard con métricas globales: total tenants activos, nuevos este mes,
  MRR (Monthly Recurring Revenue), embarques del día en toda la plataforma
- [ ] CA-02: Lista de todos los tenants con: nombre, plan, estado, fecha de registro,
  próximo vencimiento, usuarios activos
- [ ] CA-03: Filtros: por plan, por estado, por país, búsqueda por nombre
- [ ] CA-04: Acciones por tenant: Ver detalle / Cambiar plan / Suspender / Cancelar /
  Reactivar / Impersonar Admin
- [ ] CA-05: Impersonación: Super Admin puede acceder al sistema como Admin de un tenant
  (registrado en auditoría con acción `IMPERSONACION`)
- [ ] CA-06: Vista de detalle de tenant: datos, suscripción, historial de pagos,
  usuarios, embarques del mes, storage usado
- [ ] CA-07: Exportación de lista de tenants a Excel/CSV

**Endpoints API:**
```
GET  /api/admin/dashboard          → métricas globales del SaaS
GET  /api/admin/empresas           → lista de todos los tenants (paginado)
GET  /api/admin/empresas/{id}      → detalle completo del tenant
PUT  /api/admin/empresas/{id}/plan → cambiar plan del tenant
PUT  /api/admin/empresas/{id}/estado → suspender/reactivar/cancelar
POST /api/admin/empresas/{id}/impersonar → genera JWT de impersonación
GET  /api/admin/empresas/export    → exportar lista a CSV
```

**Estimación:** 13 pts | **Asignado a:** @Arquitecto → @BackendDev → @FrontendDev → @QA

---

### HU-010 · Gestión de Planes de Suscripción

**Como** Super Admin, **quiero** definir y gestionar planes de suscripción
con límites y precios, **para** monetizar la plataforma.

**Criterios de aceptación:**
- [ ] CA-01: CRUD completo de planes (nombre, código, descripción, precio mensual/anual,
  límite usuarios, embarques/mes, storage, módulos disponibles)
- [ ] CA-02: Los módulos disponibles se seleccionan de la lista de 12 módulos del sistema
- [ ] CA-03: Precio anual con descuento calculado automáticamente (sugerencia: 2 meses gratis)
- [ ] CA-04: Un plan no puede eliminarse si tiene empresas suscritas activas
- [ ] CA-05: Cambio de límites en un plan existente notifica a los tenants afectados
- [ ] CA-06: Vista de cuántas empresas tienen cada plan

**Endpoints API:**
```
GET    /api/admin/planes
GET    /api/admin/planes/{id}
POST   /api/admin/planes
PUT    /api/admin/planes/{id}
DELETE /api/admin/planes/{id}/deactivate
GET    /api/admin/planes/{id}/empresas   → empresas suscritas a este plan
```

**Estimación:** 8 pts | **Asignado a:** @Arquitecto → @IngenieroDatos → @BackendDev → @QA

---

### HU-011 · Facturación Recurrente de Tenants (SaaS Billing — Manual)

**Como** Super Admin, **quiero** gestionar el ciclo de facturación de cada
tenant, **para** controlar los ingresos de la plataforma.

**Criterios de aceptación:**
- [ ] CA-01: Vista de suscripciones: empresa, plan, fecha inicio, fecha vencimiento,
  estado, monto, próximo cobro
- [ ] CA-02: Registro manual de pago: empresa, monto, método (efectivo, transferencia,
  otro), referencia, período cubierto
- [ ] CA-03: Al registrar pago → actualizar `fecha_vencimiento` de la suscripción
  según el ciclo (mensual +30 días, anual +365 días) y estado → ACTIVE
- [ ] CA-04: Alertas automáticas al Super Admin: tenants con vencimiento en 15, 7 y 1 día
- [ ] CA-05: Después de vencimiento sin pago → cambiar estado a `PAST_DUE` automáticamente
- [ ] CA-06: Después de 7 días en `PAST_DUE` → cambiar estado a `SUSPENDED`
- [ ] CA-07: Estado `SUSPENDED` → el tenant solo puede hacer login y ver el mensaje
  de suspensión (no accede a ningún módulo operativo)
- [ ] CA-08: Historial completo de pagos por tenant con exportación a PDF/Excel
- [ ] CA-09: Dashboard financiero: MRR, ARR, churn del mes, tenants por estado

**Endpoints API:**
```
GET  /api/admin/suscripciones                  → lista de suscripciones
GET  /api/admin/suscripciones/{id}             → detalle
POST /api/admin/suscripciones                  → crear suscripción nueva
PUT  /api/admin/suscripciones/{id}             → actualizar
POST /api/admin/suscripciones/{id}/pago        → registrar pago manual
GET  /api/admin/suscripciones/{id}/pagos       → historial de pagos
GET  /api/admin/dashboard/financiero           → MRR, ARR, churn
```

**Job de verificación de vencimientos:**
```csharp
// Freiroute.API/BackgroundJobs/VencimientoSuscripcionJob.cs
// Ejecutar diariamente a las 00:00 UTC
// 1. Buscar suscripciones ACTIVE con fecha_vencimiento < NOW() → PAST_DUE
// 2. Buscar suscripciones PAST_DUE con más de 7 días → SUSPENDED
// 3. Notificar al Super Admin de los cambios
// 4. Notificar al Admin del tenant de su nuevo estado
```

**Implementación:** Usar `IHostedService` o `BackgroundService` de .NET
(sin Hangfire en Sprint 2 — agregar en Sprint 3 si se necesita).

**Estimación:** 13 pts | **Asignado a:** @BackendDev → @QA

---

### HU-012 · Onboarding Wizard para Nuevos Tenants

**Como** nuevo Admin de tenant, **quiero** un asistente de configuración
inicial, **para** configurar mi empresa rápidamente al activar la cuenta.

**Criterios de aceptación:**
- [ ] CA-01: El wizard se activa automáticamente en el primer login del Admin
  del tenant si `onboarding_completado = false`
- [ ] CA-02: Paso 1 (Datos de empresa): nombre, RUC/NIT, dirección, teléfono, industria
- [ ] CA-03: Paso 2 (Identidad visual): logo, color primario, color secundario con preview
- [ ] CA-04: Paso 3 (Configuración operativa): moneda, zona horaria, formato fecha,
  modos de transporte activos, prefijos de numeración
- [ ] CA-05: Paso 4 (Admin): confirmar datos del Admin y cambiar contraseña si es temporal
- [ ] CA-06: Paso 5 (Equipo): hasta 5 invitaciones por email con rol asignado.
  Botón "Saltar por ahora" disponible
- [ ] CA-07: El progreso se persiste en BD — si cierra el browser puede retomar
- [ ] CA-08: Al completar el paso 5 (o saltarlo) → `onboarding_completado = true`
  y redirigir al Dashboard
- [ ] CA-09: Barra de progreso visible en todo el wizard (% completado)
- [ ] CA-10: El wizard puede retomarse desde el menú "Configuración" si desea
  revisarlo después

**Endpoints API:**
```
GET  /api/onboarding/estado          → paso actual y datos guardados
PUT  /api/onboarding/paso/1          → guardar datos empresa
PUT  /api/onboarding/paso/2          → guardar identidad (+ upload logo)
PUT  /api/onboarding/paso/3          → guardar configuración operativa
PUT  /api/onboarding/paso/4          → guardar datos admin
POST /api/onboarding/paso/5          → enviar invitaciones y completar
POST /api/onboarding/completar       → completar sin invitaciones (skip)
```

**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

### HU-013 · Gestión de Usuarios por Tenant

**Como** Admin del tenant, **quiero** crear, editar, desactivar e invitar
usuarios de mi empresa, **para** gestionar el acceso al sistema.

**Criterios de aceptación:**
- [ ] CA-01: Lista de usuarios con: nombre, email, tipo, perfil, estado, último acceso
- [ ] CA-02: Filtros: por estado, por perfil, búsqueda por nombre/email
- [ ] CA-03: Crear usuario directamente (sin invitación) — Admin crea la cuenta
  y el usuario recibe email con contraseña temporal
- [ ] CA-04: Invitar usuario por email → el invitado recibe link para activar su cuenta
- [ ] CA-05: Editar usuario: nombre, teléfono, perfil asignado, tipo de identidad
- [ ] CA-06: Desactivar usuario — no puede iniciar sesión pero el historial se preserva
- [ ] CA-07: Reactivar usuario desactivado
- [ ] CA-08: El límite de usuarios según el plan se verifica al crear/reactivar
  (`PlanLimiteService.VerificarLimiteUsuariosAsync`)
- [ ] CA-09: Si el tenant está en plan STARTER (límite 5) y tiene 5 usuarios activos
  → crear/reactivar retorna error descriptivo con link al plan superior
- [ ] CA-10: Vista del último acceso de cada usuario
- [ ] CA-11: Admin no puede editar ni desactivar al Super Admin

**Endpoints API:**
```
GET    /api/usuarios                    → lista (ya existe Sprint 1)
GET    /api/usuarios/{id}               → detalle (ya existe Sprint 1)
POST   /api/usuarios                    → crear (ya existe Sprint 1)
PUT    /api/usuarios/{id}               → editar (ya existe Sprint 1)
DELETE /api/usuarios/{id}/deactivate    → desactivar (ya existe Sprint 1)
PUT    /api/usuarios/{id}/reactivar     → reactivar ← NUEVO
POST   /api/usuarios/invitar            → invitar (ya existe Sprint 1)
POST   /api/usuarios/aceptar-invitacion → aceptar (ya existe Sprint 1)
```

**Nuevos en este sprint:**
- `PUT /api/usuarios/{id}/reactivar` → cambia estado SUSPENDED → ACTIVE,
  verifica límite del plan, registra auditoría
- `PlanLimiteService` → verificación de límites

**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

### HU-014 · Configuración General del Tenant

**Como** Admin del tenant, **quiero** configurar los parámetros generales
de mi empresa, **para** adaptar el sistema a mi operación.

**Criterios de aceptación:**
- [ ] CA-01: Datos generales editables: nombre, RUC/NIT, dirección fiscal, teléfono,
  industria, sitio web
- [ ] CA-02: Logo: carga de imagen (PNG/SVG, máx 2MB), preview inmediato,
  almacenado en Supabase Storage bucket privado
- [ ] CA-03: Identidad visual: color primario y secundario con preview del sidebar
- [ ] CA-04: Configuración operativa: moneda principal, zona horaria, formato de fecha
- [ ] CA-05: Numeración de documentos: prefijos y consecutivos configurables
  (embarques, órdenes, carta de porte)
- [ ] CA-06: Configuración de email saliente: remitente, nombre del remitente
  (para notificaciones del sistema)
- [ ] CA-07: Cambios guardados con confirmación — los cambios de logo y color
  se aplican inmediatamente en toda la UI sin recargar
- [ ] CA-08: Historial de cambios de configuración en auditoría

**Endpoints API:**
```
GET  /api/configuracion              → configuración actual del tenant
PUT  /api/configuracion              → actualizar configuración general
POST /api/configuracion/logo         → upload del logo (multipart/form-data)
DELETE /api/configuracion/logo       → eliminar logo actual
GET  /api/configuracion/numeracion   → consecutivos actuales
PUT  /api/configuracion/numeracion   → actualizar prefijos y consecutivos
```

**Nota sobre upload de logo:**
- Supabase Storage bucket: `logos-tenants` (privado)
- Path: `{empresa_id}/logo.{ext}`
- Generar signed URL con expiración de 24h para mostrar el logo en la UI
- Al actualizar logo → invalidar la URL anterior

**Estimación:** 5 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

## Estructura de Archivos a Crear en el Sprint 2

### @Arquitecto entrega:

```
Freiroute.Entity/
├── Plan.cs
├── Suscripcion.cs
├── Pago.cs
├── Configuracion2fa.cs
└── Codigo2faTempora.cs

Freiroute.DTO/
├── Admin/
│   ├── DashboardGlobalResponseDto.cs
│   ├── DashboardFinancieroResponseDto.cs
│   ├── ImpersonarResponseDto.cs
│   └── CambiarPlanRequestDto.cs
├── Plan/
│   ├── PlanRequestDto.cs
│   └── PlanResponseDto.cs
├── Suscripcion/
│   ├── SuscripcionRequestDto.cs
│   ├── SuscripcionResponseDto.cs
│   ├── PagoRequestDto.cs
│   └── PagoResponseDto.cs
├── Onboarding/
│   ├── OnboardingEstadoResponseDto.cs
│   ├── OnboardingPaso1RequestDto.cs
│   ├── OnboardingPaso2RequestDto.cs
│   ├── OnboardingPaso3RequestDto.cs
│   ├── OnboardingPaso4RequestDto.cs
│   └── OnboardingPaso5RequestDto.cs
├── Auth/
│   ├── OAuthCallbackRequestDto.cs   ← HU-004
│   ├── Verificar2faRequestDto.cs    ← HU-005
│   ├── Activar2faRequestDto.cs
│   └── Setup2faResponseDto.cs
└── Configuracion/
    ├── ConfiguracionRequestDto.cs
    ├── ConfiguracionResponseDto.cs
    ├── NumeracionRequestDto.cs
    └── NumeracionResponseDto.cs

Freiroute.DAL/Interfaces/
├── IPlanRepository.cs
├── ISuscripcionRepository.cs
├── IPagoRepository.cs
├── IConfiguracion2faRepository.cs
└── IConfiguracionRepository.cs

Freiroute.BLL/Interfaces/
├── IPlanService.cs
├── ISuscripcionService.cs
├── IOnboardingService.cs
├── IPlanLimiteService.cs
├── IConfiguracionService.cs
└── IAdminDashboardService.cs
```

### @IngenieroDatos entrega:

```
supabase/migrations/
├── 20260201000001_tabla_planes.sql
├── 20260201000002_tabla_suscripciones.sql
├── 20260201000003_tabla_pagos.sql
├── 20260201000004_tabla_configuracion_2fa.sql
├── 20260201000005_tabla_codigos_2fa_temporales.sql
└── 20260201000006_alter_empresas_onboarding_plan.sql

Freiroute.DAL/Repositories/
├── PlanRepository.cs
├── SuscripcionRepository.cs
├── PagoRepository.cs
├── Configuracion2faRepository.cs
└── ConfiguracionRepository.cs
```

### @BackendDev entrega:

```
Freiroute.BLL/
├── Services/
│   ├── AdminDashboardService.cs
│   ├── PlanService.cs
│   ├── SuscripcionService.cs
│   ├── OnboardingService.cs
│   ├── PlanLimiteService.cs
│   └── ConfiguracionService.cs
├── Validators/
│   ├── PlanValidator.cs
│   ├── SuscripcionValidator.cs
│   ├── PagoValidator.cs
│   ├── OnboardingPaso1Validator.cs
│   ├── OnboardingPaso3Validator.cs
│   └── ConfiguracionValidator.cs

Freiroute.API/
├── Controllers/
│   ├── AdminController.cs          ← HU-009 + HU-010 + HU-011
│   ├── OnboardingController.cs     ← HU-012
│   └── ConfiguracionController.cs  ← HU-014
├── BackgroundJobs/
│   └── VencimientoSuscripcionJob.cs ← HU-011
└── Middleware/
    └── OnboardingRedirectMiddleware.cs ← HU-012 CA-01

Actualizar:
├── AuthController.cs      ← agregar OAuth callback + 2FA endpoints
└── UsuariosController.cs  ← agregar PUT /reactivar
```

### @FrontendDev entrega:

```
Freiroute.Aplicacion/Areas/
├── Admin/Views/
│   ├── Dashboard/Index.cshtml          ← HU-009 panel global
│   ├── Planes/
│   │   ├── Index.cshtml
│   │   └── Create.cshtml
│   ├── Suscripciones/
│   │   ├── Index.cshtml
│   │   └── RegistrarPago.cshtml
│   └── Empresas/
│       └── Detalle.cshtml              ← HU-009 vista detalle
├── Tenant/Views/
│   ├── Usuarios/
│   │   ├── Index.cshtml               ← HU-013 (mejorado)
│   │   ├── Create.cshtml
│   │   └── Edit.cshtml
│   └── Configuracion/
│       └── Index.cshtml               ← HU-014
└── Onboarding/Views/
    ├── _LayoutOnboarding.cshtml       ← layout especial sin sidebar
    ├── Paso1.cshtml
    ├── Paso2.cshtml
    ├── Paso3.cshtml
    ├── Paso4.cshtml
    └── Paso5.cshtml

Actualizar:
└── Areas/Auth/Views/Account/Login.cshtml  ← activar botones OAuth (HU-004)
```

---

## Definición de Done (DoD) — Sprint 2

- [ ] `supabase db push` aplicado sin errores — 6 nuevas migraciones
- [ ] `supabase db diff` vacío
- [ ] Panel Super Admin funcional con métricas globales (HU-009)
- [ ] CRUD de planes funcional (HU-010)
- [ ] Registro manual de pagos funcional (HU-011)
- [ ] Job de vencimiento de suscripciones corriendo en background (HU-011)
- [ ] Wizard de onboarding completo — 5 pasos con persistencia (HU-012)
- [ ] Límite de usuarios verificado al crear/reactivar (HU-013)
- [ ] Configuración del tenant guardada — logo en Supabase Storage (HU-014)
- [ ] OAuth Google/Microsoft funcional en UI (HU-004)
- [ ] 2FA TOTP + email funcional con códigos de recuperación (HU-005)
- [ ] `dotnet build` sin warnings
- [ ] `dotnet test` sin fallos
- [ ] Cobertura BLL ≥ 80%
- [ ] Cobertura API ≥ 60%
- [ ] Swagger documentado en todos los endpoints nuevos
- [ ] UI con Design System Freiroute — sin colores hardcodeados
- [ ] PR revisado y aprobado por @PM

---

## Convenciones específicas del Sprint 2

### Verificación de límites del plan
```csharp
// Patrón en todos los servicios que crean recursos limitados:
await _planLimiteService.VerificarLimiteUsuariosAsync(empresaId);
// Si supera el límite → throw BusinessException con mensaje:
// "Has alcanzado el límite de X usuarios de tu plan {NombrePlan}.
//  Actualiza al plan {PlanSuperior} para agregar más usuarios."
```

### Impersonación de tenant
```csharp
// El JWT de impersonación incluye claim adicional:
// "impersonado_por": "{super_admin_user_id}"
// Toda acción durante impersonación se registra en auditoría
// con este claim visible
```

### Estados de empresa en middleware
```csharp
// SuspensionMiddleware — nuevo en Sprint 2:
// Si empresa.estado == SUSPENDED y la ruta no es /auth/* ni /suspension →
// redirigir a /suspension con mensaje de cuenta suspendida
```

---

*Spec Sprint 2 — Freiroute TMS*
*Versión: 1.0 | Fecha: 2026*
*Próximo: Sprint 3 — EP-03 Gestión de Maestros y Catálogos*
