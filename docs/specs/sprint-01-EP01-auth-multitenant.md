# Spec: Sprint 1 — EP-01 Infraestructura Multi-Tenant & Auth

**Sprint:** 01
**Épica:** EP-01 — Infraestructura Multi-Tenant & Autenticación
**Historias:** HU-001 · HU-002 · HU-003 · HU-004 · HU-005 · HU-006 · HU-007 · HU-008
**Story Points:** 63 pts
**Objetivo del Sprint:** Tener la plataforma operativa con registro de tenants, autenticación JWT segura, RBAC funcional y auditoría de accesos. Todo lo demás del sistema depende de este sprint.
**ADRs aplicables:** ADR-001 · ADR-002 · ADR-003

---

## Dependencias

```
Sprint 1 (este) → base para todos los sprints siguientes
Prerequisito:    Repositorio GitHub creado + Supabase proyecto creado
```

---

## Orden de Implementación por Capa

```
1. @Arquitecto  → Entities + DTOs + Interfaces (todas las HU del sprint)
2. @IngenieroDatos → Migraciones SQL (tablas base del sistema)
3. @BackendDev  → BLL Services + API Controllers
4. @QA          → Tests unitarios + integración
5. @FrontendDev → Vistas Razor (login, registro, dashboard base)
```

---

## Tablas de Base de Datos del Sprint 1

### Diagrama de relaciones

```
empresas (tenant raíz)
    │
    ├── usuarios ──────── perfiles
    │       │                 │
    │       └── sesiones      └── permisos
    │
    └── auditoria_actividad
```

### Tabla: `empresas`

```sql
CREATE TABLE IF NOT EXISTS empresas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre              VARCHAR(200) NOT NULL,
    ruc_nit             VARCHAR(50),
    email_admin         VARCHAR(200) NOT NULL UNIQUE,
    telefono            VARCHAR(50),
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    ciudad              VARCHAR(100),
    direccion           TEXT,
    logo_url            TEXT,
    color_primario      VARCHAR(7)  DEFAULT '#1A73E8',
    color_secundario    VARCHAR(7)  DEFAULT '#0B2545',
    plan_suscripcion    VARCHAR(50) NOT NULL DEFAULT 'STARTER',
    estado              VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    -- Configuración operativa
    moneda_principal    VARCHAR(10) NOT NULL DEFAULT 'USD',
    zona_horaria        VARCHAR(100) NOT NULL DEFAULT 'America/Managua',
    idioma              VARCHAR(10) NOT NULL DEFAULT 'es',
    formato_fecha       VARCHAR(20) NOT NULL DEFAULT 'DD/MM/YYYY',
    -- Numeración de documentos
    prefijo_embarque    VARCHAR(10) NOT NULL DEFAULT 'FR',
    consecutivo_embarque INTEGER NOT NULL DEFAULT 1,
    prefijo_orden       VARCHAR(10) NOT NULL DEFAULT 'ORD',
    consecutivo_orden   INTEGER NOT NULL DEFAULT 1
);

COMMENT ON TABLE empresas IS 'Tabla raíz de tenants del SaaS Freiroute. Cada registro es una empresa de transporte suscrita.';
COMMENT ON COLUMN empresas.id IS 'Identificador único del tenant';
COMMENT ON COLUMN empresas.plan_suscripcion IS 'Plan contratado: STARTER, PROFESSIONAL, ENTERPRISE';
COMMENT ON COLUMN empresas.estado IS 'Estado del tenant: ACTIVE, SUSPENDED, CANCELLED';
COMMENT ON COLUMN empresas.color_primario IS 'Color hex para personalización white-label';
COMMENT ON COLUMN empresas.consecutivo_embarque IS 'Contador para numeración de embarques FR-YYYY-NNNNN';

CREATE INDEX idx_empresas_estado ON empresas(estado);
CREATE INDEX idx_empresas_plan   ON empresas(plan_suscripcion);
```

> ⚠️ `empresas` NO tiene `empresa_id` propio ni RLS — es la tabla raíz. El Super Admin la gestiona sin filtro de tenant.

---

### Tabla: `perfiles`

```sql
CREATE TABLE IF NOT EXISTS perfiles (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    descripcion         TEXT,
    tipo_perfil         VARCHAR(50) NOT NULL DEFAULT 'CUSTOM',
    es_sistema          BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE perfiles IS 'Roles/perfiles de usuario por empresa. Cada empresa tiene sus propios perfiles.';
COMMENT ON COLUMN perfiles.tipo_perfil IS 'SUPER_ADMIN, ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE, CUSTOM';
COMMENT ON COLUMN perfiles.es_sistema IS 'true = perfil creado por el sistema (no se puede eliminar)';

CREATE INDEX idx_perfiles_empresa_id ON perfiles(empresa_id);
CREATE INDEX idx_perfiles_activo     ON perfiles(activo);
CREATE INDEX idx_perfiles_empresa_activo ON perfiles(empresa_id, activo);

ALTER TABLE perfiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_perfiles" ON perfiles
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_perfiles_fecha_modificacion
    BEFORE UPDATE ON perfiles
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `permisos`

```sql
CREATE TABLE IF NOT EXISTS permisos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id) ON DELETE CASCADE,
    modulo              VARCHAR(100) NOT NULL,
    puede_leer          BOOLEAN NOT NULL DEFAULT false,
    puede_crear         BOOLEAN NOT NULL DEFAULT false,
    puede_actualizar    BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_permiso_perfil_modulo UNIQUE (perfil_id, modulo)
);

COMMENT ON TABLE permisos IS 'Permisos granulares por módulo para cada perfil. Solo READ, CREATE, UPDATE — no existe DELETE.';
COMMENT ON COLUMN permisos.modulo IS 'Nombre del módulo: ordenes, embarques, carriers, rutas, etc.';
COMMENT ON COLUMN permisos.puede_leer IS 'Permiso READ: ver listados y detalles';
COMMENT ON COLUMN permisos.puede_crear IS 'Permiso CREATE: crear nuevos registros';
COMMENT ON COLUMN permisos.puede_actualizar IS 'Permiso UPDATE: editar y desactivar registros';

CREATE INDEX idx_permisos_empresa_id ON permisos(empresa_id);
CREATE INDEX idx_permisos_perfil_id  ON permisos(perfil_id);
CREATE INDEX idx_permisos_activo     ON permisos(activo);

ALTER TABLE permisos ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_permisos" ON permisos
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_permisos_fecha_modificacion
    BEFORE UPDATE ON permisos
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `usuarios`

```sql
CREATE TABLE IF NOT EXISTS usuarios (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    -- Identificación
    tipo_identidad      VARCHAR(20) NOT NULL DEFAULT 'CEDULA',
    numero_identidad    VARCHAR(50),
    nombre_completo     VARCHAR(200) NOT NULL,
    email               VARCHAR(200) NOT NULL,
    telefono            VARCHAR(50),
    foto_url            TEXT,
    -- Auth (Supabase Auth)
    supabase_user_id    UUID UNIQUE,
    -- Estado
    tipo_usuario        VARCHAR(50) NOT NULL DEFAULT 'OPERADOR',
    estado              VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    ultimo_acceso       TIMESTAMPTZ,
    intentos_fallidos   INTEGER NOT NULL DEFAULT 0,
    bloqueado_hasta     TIMESTAMPTZ,
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_usuario_email_empresa UNIQUE (email, empresa_id)
);

COMMENT ON TABLE usuarios IS 'Usuarios del sistema por empresa. La autenticación la gestiona Supabase Auth; este registro guarda el perfil de negocio.';
COMMENT ON COLUMN usuarios.tipo_identidad IS 'CEDULA, PASAPORTE, RUC, DNI';
COMMENT ON COLUMN usuarios.tipo_usuario IS 'SUPER_ADMIN, ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE';
COMMENT ON COLUMN usuarios.estado IS 'PENDING (invitado), ACTIVE (activo), SUSPENDED, LOCKED';
COMMENT ON COLUMN usuarios.supabase_user_id IS 'FK hacia auth.users de Supabase — vincula la identidad de autenticación con el perfil de negocio';

CREATE INDEX idx_usuarios_empresa_id     ON usuarios(empresa_id);
CREATE INDEX idx_usuarios_perfil_id      ON usuarios(perfil_id);
CREATE INDEX idx_usuarios_email          ON usuarios(email);
CREATE INDEX idx_usuarios_supabase_id    ON usuarios(supabase_user_id);
CREATE INDEX idx_usuarios_activo         ON usuarios(activo);
CREATE INDEX idx_usuarios_empresa_activo ON usuarios(empresa_id, activo);

ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_usuarios" ON usuarios
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_usuarios_fecha_modificacion
    BEFORE UPDATE ON usuarios
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `invitaciones`

```sql
CREATE TABLE IF NOT EXISTS invitaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    email               VARCHAR(200) NOT NULL,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id),
    token               VARCHAR(200) NOT NULL UNIQUE,
    estado              VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    fecha_expiracion    TIMESTAMPTZ NOT NULL,
    fecha_aceptacion    TIMESTAMPTZ,
    creado_por_id       UUID REFERENCES usuarios(id),
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE invitaciones IS 'Invitaciones de usuarios por email. El token expira en 48 horas.';
COMMENT ON COLUMN invitaciones.estado IS 'PENDING, ACCEPTED, EXPIRED, CANCELLED';

CREATE INDEX idx_invitaciones_token     ON invitaciones(token);
CREATE INDEX idx_invitaciones_empresa   ON invitaciones(empresa_id);
CREATE INDEX idx_invitaciones_email     ON invitaciones(email);
```

---

### Tabla: `auditoria_actividad`

```sql
CREATE TABLE IF NOT EXISTS auditoria_actividad (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID REFERENCES empresas(id) ON DELETE SET NULL,
    usuario_id      UUID,
    modulo          VARCHAR(100) NOT NULL,
    accion          VARCHAR(50)  NOT NULL,
    entidad_tipo    VARCHAR(100),
    entidad_id      UUID,
    ip_address      INET,
    user_agent      TEXT,
    detalles        JSONB,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE auditoria_actividad IS 'Log inmutable de todas las acciones del sistema. Retención mínima: 12 meses.';
COMMENT ON COLUMN auditoria_actividad.accion IS 'LOGIN, LOGOUT, CREATE, UPDATE, DEACTIVATE, EXPORT, VIEW, CAMBIO_ESTADO';
COMMENT ON COLUMN auditoria_actividad.detalles IS 'JSON con datos adicionales: valores anteriores/nuevos, contexto';

CREATE INDEX idx_auditoria_empresa_id  ON auditoria_actividad(empresa_id);
CREATE INDEX idx_auditoria_usuario_id  ON auditoria_actividad(usuario_id);
CREATE INDEX idx_auditoria_fecha       ON auditoria_actividad(fecha_creacion DESC);
CREATE INDEX idx_auditoria_modulo      ON auditoria_actividad(modulo);
CREATE INDEX idx_auditoria_accion      ON auditoria_actividad(accion);

ALTER TABLE auditoria_actividad ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_auditoria" ON auditoria_actividad
    FOR SELECT
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);
```

---

### Función global y datos iniciales

```sql
-- Función update_fecha_modificacion (crear PRIMERO, antes de los triggers)
CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_fecha_modificacion() IS
'Actualiza fecha_modificacion automáticamente en cada UPDATE. Usada por todos los triggers del sistema.';

-- ── Datos iniciales del Super Admin ──────────────────────────────────────────
-- Empresa raíz del SaaS (Freiroute como tenant propio)
INSERT INTO empresas (
    id, nombre, email_admin, pais, plan_suscripcion, estado,
    color_primario, color_secundario
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Freiroute SaaS Admin',
    'admin@freiroute.com',
    'Nicaragua',
    'ENTERPRISE',
    'ACTIVE',
    '#1A73E8',
    '#0B2545'
) ON CONFLICT DO NOTHING;

-- Perfil Super Admin del sistema
INSERT INTO perfiles (
    id, empresa_id, nombre, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000010',
    '00000000-0000-0000-0000-000000000001',
    'Super Administrador',
    'SUPER_ADMIN',
    true
) ON CONFLICT DO NOTHING;

-- Perfil Admin estándar para nuevos tenants (plantilla)
INSERT INTO perfiles (
    id, empresa_id, nombre, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000011',
    '00000000-0000-0000-0000-000000000001',
    'Administrador de Empresa',
    'ADMIN',
    true
) ON CONFLICT DO NOTHING;
```

---

## Historias de Usuario del Sprint

---

### HU-001 · Registro de nuevo tenant

**Como** Super Admin de Freiroute, **quiero** registrar una nueva empresa en la plataforma, **para** activarla como tenant independiente con su propio espacio de datos.

**Criterios de aceptación:**
- [ ] CA-01: El sistema crea un registro en `empresas` con UUID único generado por BD
- [ ] CA-02: Se crean automáticamente los perfiles base del tenant: Admin, Dispatcher, Operador, Conductor, Cliente
- [ ] CA-03: Se envía email de bienvenida al `email_admin` con link al onboarding wizard
- [ ] CA-04: El tenant queda en estado `ACTIVE` por defecto
- [ ] CA-05: La operación queda registrada en `auditoria_actividad`
- [ ] CA-06: Si el `email_admin` ya existe en otra empresa, retorna error 409 con mensaje claro
- [ ] CA-07: Solo el `SUPER_ADMIN` puede registrar nuevas empresas (403 para otros roles)

**Entidades:**
- Entity: `Empresa`
- RequestDto: `EmpresaRequestDto`
- ResponseDto: `EmpresaResponseDto`
- Tabla BD: `empresas`

**Endpoint API:**
```
POST /api/empresas
Authorization: Bearer {JWT_SUPER_ADMIN}
```

**Reglas de negocio:**
- Al crear empresa → crear perfiles base con `es_sistema = true` (transacción)
- Perfiles base a crear: ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE
- El `prefijo_embarque` por defecto es las 2 primeras letras del nombre de la empresa en mayúsculas

**Estimación:** 8 pts | **Asignado a:** @Arquitecto → @IngenieroDatos → @BackendDev → @QA

---

### HU-002 · Aislamiento de datos por tenant (RLS)

**Como** arquitecto del sistema, **quiero** que cada tenant acceda únicamente a sus propios datos, **para** garantizar seguridad y privacidad total entre empresas.

**Criterios de aceptación:**
- [ ] CA-01: RLS habilitado en tablas: `perfiles`, `permisos`, `usuarios`, `invitaciones`, `auditoria_actividad`
- [ ] CA-02: `TenantMiddleware` extrae `empresa_id` del JWT e inyecta `app.current_empresa_id` en cada request
- [ ] CA-03: Test de penetración: usuario de empresa A no puede leer datos de empresa B aunque manipule el request
- [ ] CA-04: Toda query en repositorios incluye `AND empresa_id = @EmpresaId` explícitamente
- [ ] CA-05: El `SUPER_ADMIN` puede acceder a todas las empresas (bypass controlado de RLS)
- [ ] CA-06: Si `empresa_id` no está en el JWT, el request retorna 401

**Implementación:**
- `TenantMiddleware.cs` en `Freiroute.API/Middleware/`
- Política RLS en cada tabla de negocio (ver SQL en sección de tablas)
- Test de aislamiento: crear datos en empresa A, intentar leer con JWT de empresa B → debe retornar vacío/404

**Estimación:** 13 pts | **Asignado a:** @IngenieroDatos + @BackendDev → @QA

---

### HU-003 · Registro e inicio de sesión de usuario

**Como** usuario del sistema, **quiero** registrarme e iniciar sesión con email y contraseña, **para** acceder a la plataforma de forma segura.

**Criterios de aceptación:**
- [ ] CA-01: Login con email y contraseña retorna JWT con claims: `user_id`, `empresa_id`, `perfil_id`, `tipo_usuario`, `permisos[]`, `nombre`
- [ ] CA-02: JWT válido por 8 horas; refresh token válido 30 días
- [ ] CA-03: Contraseña mínimo 8 caracteres, al menos 1 mayúscula, 1 número y 1 carácter especial
- [ ] CA-04: Cuenta bloqueada tras 5 intentos fallidos consecutivos (campo `bloqueado_hasta` = NOW() + 30 min)
- [ ] CA-05: Login exitoso actualiza `ultimo_acceso` y resetea `intentos_fallidos` a 0
- [ ] CA-06: Login fallido incrementa `intentos_fallidos` en 1
- [ ] CA-07: Usuario en estado `PENDING` o `SUSPENDED` no puede iniciar sesión (mensaje específico)
- [ ] CA-08: La operación queda registrada en `auditoria_actividad` con acción `LOGIN` o `LOGIN_FAILED`

**Endpoints API:**
```
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

**Request de login:**
```json
{
  "email": "usuario@empresa.com",
  "password": "MiPassword123!"
}
```

**Response de login:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresIn": 28800,
    "usuario": {
      "id": "uuid",
      "nombre": "Juan Pérez",
      "email": "juan@empresa.com",
      "tipoUsuario": "DISPATCHER",
      "empresaNombre": "Trans Nicaragua S.A.",
      "permisos": ["ordenes:read", "embarques:read", "embarques:create"]
    }
  }
}
```

**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

### HU-004 · Autenticación OAuth 2.0 (Google / Microsoft)

**Como** usuario, **quiero** iniciar sesión con mi cuenta de Google o Microsoft, **para** acceder sin recordar otra contraseña.

**Criterios de aceptación:**
- [ ] CA-01: Botón "Continuar con Google" funcional en la pantalla de login
- [ ] CA-02: Botón "Continuar con Microsoft" funcional en la pantalla de login
- [ ] CA-03: Si el email del proveedor ya existe en `usuarios`, se vincula la cuenta (no crea duplicado)
- [ ] CA-04: Si es nuevo usuario OAuth, se crea con estado `ACTIVE` y se le asigna el perfil base del tenant
- [ ] CA-05: El token SSO de Google/Microsoft se mapea al JWT interno de Freiroute con los mismos claims
- [ ] CA-06: El login OAuth queda registrado en `auditoria_actividad`

**Implementación:** Supabase Auth maneja el OAuth. El callback actualiza `supabase_user_id` en la tabla `usuarios`.

**Estimación:** 5 pts | **Asignado a:** @BackendDev → @FrontendDev

---

### HU-005 · Autenticación de dos factores (2FA)

**Como** administrador, **quiero** habilitar 2FA para usuarios del sistema, **para** reforzar la seguridad de acceso.

**Criterios de aceptación:**
- [ ] CA-01: Soporte TOTP (Google Authenticator, Authy) — generación de QR de configuración
- [ ] CA-02: Soporte 2FA por email (código de 6 dígitos, válido 10 minutos)
- [ ] CA-03: El admin puede hacer 2FA obligatorio para todos los usuarios del tenant
- [ ] CA-04: El usuario puede desactivar 2FA solo tras verificar el código actual
- [ ] CA-05: Se generan 8 códigos de recuperación de un solo uso al activar 2FA
- [ ] CA-06: Si 2FA falla 3 veces consecutivas, la sesión se cierra

**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev

---

### HU-006 · Gestión de roles y permisos (RBAC)

**Como** administrador del tenant, **quiero** definir roles con permisos granulares por módulo, **para** controlar qué puede hacer cada usuario.

**Criterios de aceptación:**
- [ ] CA-01: Perfiles base creados automáticamente en todo tenant nuevo: Admin, Dispatcher, Operador, Conductor, Cliente
- [ ] CA-02: El admin puede crear perfiles personalizados adicionales
- [ ] CA-03: Los permisos son por módulo con 3 niveles: READ, CREATE, UPDATE (no DELETE)
- [ ] CA-04: Módulos con permiso: ordenes, embarques, carriers, rutas, track_trace, documentos, flota, analytics, facturacion, clientes, usuarios, configuracion
- [ ] CA-05: Cambio de permiso aplicado en el próximo login del usuario (no requiere reiniciar servidor)
- [ ] CA-06: El `SUPER_ADMIN` tiene acceso total implícito — no usa la tabla `permisos`
- [ ] CA-07: Log en `auditoria_actividad` al cambiar permisos de un perfil

**Endpoints API:**
```
GET    /api/perfiles
POST   /api/perfiles
PUT    /api/perfiles/{id}
DELETE /api/perfiles/{id}/deactivate

GET    /api/perfiles/{id}/permisos
PUT    /api/perfiles/{id}/permisos      ← actualiza todos los permisos del perfil
```

**Implementación del atributo `[RequirePermission]`:**
```csharp
// Freiroute.API/Attributes/RequirePermissionAttribute.cs
public class RequirePermissionAttribute : ActionFilterAttribute
{
    private readonly string _modulo;
    private readonly PermissionType _tipo;

    public RequirePermissionAttribute(string modulo, PermissionType tipo)
    {
        _modulo = modulo;
        _tipo   = tipo;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Super Admin siempre tiene acceso
        if (user.IsInRole("SUPER_ADMIN")) return;

        // Verificar permiso específico
        var permisoClaim = $"{_modulo}:{_tipo.ToString().ToLower()}";
        var permisos = user.FindAll("permisos").Select(c => c.Value);

        if (!permisos.Contains(permisoClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}

public enum PermissionType { Read, Create, Update }
```

**Estimación:** 13 pts | **Asignado a:** @Arquitecto → @BackendDev → @FrontendDev → @QA

---

### HU-007 · Recuperación de contraseña

**Como** usuario, **quiero** recuperar mi contraseña por email, **para** retomar acceso si la olvidé.

**Criterios de aceptación:**
- [ ] CA-01: Formulario de recuperación solicita solo el email
- [ ] CA-02: Si el email existe → enviar link con token válido 30 minutos (un solo uso)
- [ ] CA-03: Si el email NO existe → respuesta genérica idéntica (no revelar si existe o no)
- [ ] CA-04: El link expira después de 30 minutos o después del primer uso
- [ ] CA-05: Nueva contraseña debe cumplir los mismos requisitos que el registro
- [ ] CA-06: Tras cambiar contraseña → invalidar todas las sesiones activas del usuario
- [ ] CA-07: Notificación al usuario si alguien solicita recuperación de su cuenta

**Endpoints API:**
```
POST /api/auth/forgot-password    ← recibe email
POST /api/auth/reset-password     ← recibe token + nueva contraseña
```

**Estimación:** 3 pts | **Asignado a:** @BackendDev → @FrontendDev

---

### HU-008 · Auditoría de accesos y actividad

**Como** administrador, **quiero** ver un log de todas las acciones realizadas en el sistema, **para** auditar actividad y detectar comportamientos anómalos.

**Criterios de aceptación:**
- [ ] CA-01: Se registran automáticamente: LOGIN, LOGOUT, LOGIN_FAILED, CREATE, UPDATE, DEACTIVATE, EXPORT, CAMBIO_ESTADO
- [ ] CA-02: Cada registro incluye: usuario, IP, user agent, empresa, módulo, acción, entidad afectada, timestamp
- [ ] CA-03: Vista de auditoria con filtros: usuario, módulo, acción, rango de fechas
- [ ] CA-04: Paginación estándar de 20 registros por página
- [ ] CA-05: Exportación a Excel/CSV del log filtrado
- [ ] CA-06: El log es inmutable — nadie puede editar ni eliminar registros de auditoría (solo el Super Admin puede ver el log completo sin filtro de tenant)
- [ ] CA-07: Retención mínima: 12 meses (registros más antiguos archivados, no eliminados)

**Endpoints API:**
```
GET /api/auditoria?modulo=&accion=&fechaDesde=&fechaHasta=&page=1
GET /api/auditoria/export?[mismos filtros]
```

**Servicio de auditoría (transversal):**
```csharp
// Freiroute.BLL/Interfaces/IAuditoriaService.cs
public interface IAuditoriaService
{
    Task RegistrarAsync(
        string modulo,
        string accion,
        Guid empresaId,
        Guid? usuarioId      = null,
        string? entidadTipo  = null,
        Guid? entidadId      = null,
        object? detalles     = null,
        string? ipAddress    = null,
        string? userAgent    = null);
}
```

**Estimación:** 5 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

## Estructura de Archivos a Crear en el Sprint

### @Arquitecto entrega:

```
Freiroute.Entity/
├── Empresa.cs
├── Perfil.cs
├── Permiso.cs
├── Usuario.cs
└── Invitacion.cs

Freiroute.DTO/
├── Empresa/
│   ├── EmpresaRequestDto.cs
│   └── EmpresaResponseDto.cs
├── Auth/
│   ├── LoginRequestDto.cs
│   ├── LoginResponseDto.cs
│   ├── RefreshTokenRequestDto.cs
│   ├── ForgotPasswordRequestDto.cs
│   └── ResetPasswordRequestDto.cs
├── Perfil/
│   ├── PerfilRequestDto.cs
│   └── PerfilResponseDto.cs
├── Permiso/
│   ├── PermisoRequestDto.cs
│   └── PermisoResponseDto.cs
└── Usuario/
    ├── UsuarioRequestDto.cs
    ├── UsuarioResponseDto.cs
    └── InvitacionRequestDto.cs

Freiroute.DAL/Interfaces/
├── IEmpresaRepository.cs
├── IPerfilRepository.cs
├── IPermisoRepository.cs
├── IUsuarioRepository.cs
└── IAuditoriaRepository.cs

Freiroute.BLL/Interfaces/
├── IEmpresaService.cs
├── IPerfilService.cs
├── IPermisoService.cs
├── IUsuarioService.cs
├── IAuthService.cs
└── IAuditoriaService.cs
```

### @IngenieroDatos entrega:

```
supabase/migrations/
├── 20260101000001_funcion_update_fecha_modificacion.sql
├── 20260101000002_tabla_empresas.sql
├── 20260101000003_tabla_perfiles.sql
├── 20260101000004_tabla_permisos.sql
├── 20260101000005_tabla_usuarios.sql
├── 20260101000006_tabla_invitaciones.sql
├── 20260101000007_tabla_auditoria_actividad.sql
└── 20260101000008_datos_iniciales_super_admin.sql

Freiroute.DAL/Repositories/
├── EmpresaRepository.cs
├── PerfilRepository.cs
├── PermisoRepository.cs
├── UsuarioRepository.cs
└── AuditoriaRepository.cs
```

### @BackendDev entrega:

```
Freiroute.BLL/
├── Services/
│   ├── EmpresaService.cs
│   ├── PerfilService.cs
│   ├── PermisoService.cs
│   ├── UsuarioService.cs
│   ├── AuthService.cs
│   └── AuditoriaService.cs
└── Validators/
    ├── EmpresaValidator.cs
    ├── PerfilValidator.cs
    ├── UsuarioValidator.cs
    └── LoginValidator.cs

Freiroute.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── EmpresasController.cs
│   ├── PerfilesController.cs
│   └── AuditoriaController.cs
├── Middleware/
│   ├── TenantMiddleware.cs
│   └── GlobalExceptionMiddleware.cs
└── Attributes/
    └── RequirePermissionAttribute.cs

Freiroute.Utility/
├── ApiResponse.cs
├── Pagination/PagedResult.cs
└── Exceptions/BusinessException.cs
```

### @QA entrega:

```
tests/Freiroute.BLL.Tests/
├── Services/
│   ├── EmpresaServiceTests.cs
│   ├── PerfilServiceTests.cs
│   ├── AuthServiceTests.cs
│   └── AuditoriaServiceTests.cs
└── Validators/
    ├── EmpresaValidatorTests.cs
    └── LoginValidatorTests.cs

tests/Freiroute.API.Tests/
├── Controllers/
│   ├── AuthControllerTests.cs
│   ├── EmpresasControllerTests.cs
│   └── PerfilesControllerTests.cs
├── Helpers/
│   └── JwtTestHelper.cs
└── TestWebApplicationFactory.cs
```

### @FrontendDev entrega:

```
Freiroute.Aplicacion/
├── wwwroot/
│   ├── css/freiroute.css
│   └── js/freiroute.js
├── Views/Shared/
│   └── _Layout.cshtml
└── Areas/
    ├── Auth/Views/
    │   ├── Login.cshtml
    │   ├── ForgotPassword.cshtml
    │   └── ResetPassword.cshtml
    ├── Admin/Views/
    │   ├── Empresas/
    │   │   ├── Index.cshtml
    │   │   └── Create.cshtml
    │   └── Perfiles/
    │       ├── Index.cshtml
    │       ├── Create.cshtml
    │       └── Permisos.cshtml
    └── Shared/
        └── Dashboard/Index.cshtml
```

---

## Definición de Done (DoD) — Sprint 1

Un Sprint 1 está completo cuando:

- [ ] `supabase db push` aplicado sin errores — todas las tablas creadas con RLS
- [ ] `supabase db diff` sin cambios pendientes
- [ ] Login funcional con email + contraseña desde la UI
- [ ] JWT generado con todos los claims requeridos
- [ ] `[RequirePermission]` funcional — 403 al acceder sin permiso
- [ ] `TenantMiddleware` funcional — empresa_id inyectado en cada request
- [ ] Test de aislamiento multi-tenant pasando (empresa A no ve datos de empresa B)
- [ ] Registro de nueva empresa funcional desde el panel Super Admin
- [ ] Gestión de perfiles y permisos funcional
- [ ] Log de auditoría registrando todas las acciones del sprint
- [ ] `dotnet build` sin warnings
- [ ] `dotnet test` sin fallos
- [ ] Cobertura BLL ≥ 80% (Coverlet)
- [ ] Cobertura API ≥ 60% (Coverlet)
- [ ] Swagger documentado en todos los endpoints del sprint
- [ ] UI de login con Design System Freiroute (no Bootstrap plano)
- [ ] PR revisado y aprobado

---

*Spec Sprint 1 — Freiroute TMS*
*Versión: 1.0 | Fecha: 2026*
*Próximo: Sprint 2 — EP-02 Administración SaaS & Tenants*
