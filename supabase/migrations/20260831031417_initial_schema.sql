-- ============================================================
-- MIGRACIÓN INICIAL - FREIROUTE TMS
-- Schema base multi-tenant para SaaS de transporte
-- Incluye: empresas, perfiles, usuarios, permisos
-- Campos de auditoría: creado_por, modificado_por
-- Identidad de usuarios: tipo_identidad, numero_identidad
-- Super Admin global vs Admin de tenant
-- ============================================================

-- ============================================================
-- TABLA: empresas (tenants del SaaS TMS)
-- ============================================================
CREATE TABLE IF NOT EXISTS empresas (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre                  VARCHAR(200) NOT NULL,
    slug                    VARCHAR(100) NOT NULL UNIQUE,
    plan                    VARCHAR(50) NOT NULL DEFAULT 'starter',  -- starter, professional, enterprise
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    creado_por              UUID,  -- UUID del usuario que creó (referencia a auth.users)
    modificado_por          UUID   -- UUID del usuario que modificó por última vez
);

COMMENT ON TABLE empresas IS 'Empresas de transporte que usan freiroute en modo SaaS';
COMMENT ON COLUMN empresas.slug IS 'Identificador único URL-friendly de la empresa';
COMMENT ON COLUMN empresas.plan IS 'Plan de suscripción: starter, professional, enterprise';
COMMENT ON COLUMN empresas.creado_por IS 'UUID del usuario que creó el registro (auditoría)';
COMMENT ON COLUMN empresas.modificado_por IS 'UUID del usuario que modificó por última vez (auditoría)';

-- Índices
CREATE INDEX idx_empresas_activo ON empresas(activo);
CREATE INDEX idx_empresas_slug ON empresas(slug);

-- RLS
ALTER TABLE empresas ENABLE ROW LEVEL SECURITY;

-- Super Admin puede ver todas las empresas
CREATE POLICY "super_admin_all_access" ON empresas
    FOR ALL
    USING (
        (current_setting('app.current_user_role', true)) = 'SUPER_ADMIN'
    );

-- Admin de tenant solo ve su propia empresa
CREATE POLICY "tenant_admin_own_empresa" ON empresas
    FOR SELECT
    USING (
        id = (current_setting('app.current_empresa_id', true))::UUID
        AND (current_setting('app.current_user_role', true)) IN ('ADMIN', 'SUPER_ADMIN')
    );

-- ============================================================
-- TABLA: perfiles (roles de usuario)
-- ============================================================
CREATE TABLE IF NOT EXISTS perfiles (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(100) NOT NULL,
    descripcion             TEXT,
    es_super_admin          BOOLEAN NOT NULL DEFAULT false,  -- Super Admin global (solo uno por sistema)
    es_admin_tenant         BOOLEAN NOT NULL DEFAULT false,  -- Admin del tenant/empresa
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    creado_por              UUID,
    modificado_por          UUID
);

COMMENT ON TABLE perfiles IS 'Perfiles de usuario con sus permisos por módulo. es_super_admin = Super Admin global (gestiona todas las empresas). es_admin_tenant = Admin de su empresa únicamente';
COMMENT ON COLUMN perfiles.es_super_admin IS 'Super Admin global: puede crear y gestionar TODAS las empresas/tenants';
COMMENT ON COLUMN perfiles.es_admin_tenant IS 'Admin de tenant: solo administra la empresa donde fue creado';

-- Índices
CREATE INDEX idx_perfiles_empresa_id ON perfiles(empresa_id);
CREATE INDEX idx_perfiles_activo ON perfiles(activo);

-- RLS
ALTER TABLE perfiles ENABLE ROW LEVEL SECURITY;

-- Super Admin ve todos los perfiles de todas las empresas
CREATE POLICY "super_admin_perfiles_all" ON perfiles
    FOR ALL
    USING (
        (current_setting('app.current_user_role', true)) = 'SUPER_ADMIN'
    );

-- Admin de tenant ve perfiles de su empresa
CREATE POLICY "tenant_admin_perfiles_own" ON perfiles
    FOR ALL
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
        AND (current_setting('app.current_user_role', true)) IN ('ADMIN', 'SUPER_ADMIN')
    );

-- Usuarios normales ven perfiles de su empresa (solo lectura para asignación)
CREATE POLICY "user_perfiles_own_empresa" ON perfiles
    FOR SELECT
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
        AND activo = true
    );

-- ============================================================
-- TABLA: usuarios
-- ============================================================
CREATE TABLE IF NOT EXISTS usuarios (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    perfil_id               UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    auth_user_id            UUID UNIQUE,  -- ID de Supabase Auth
    nombre                  VARCHAR(100) NOT NULL,
    apellido                VARCHAR(100) NOT NULL,
    email                   VARCHAR(255) NOT NULL UNIQUE,
    telefono                VARCHAR(20),
    avatar_url              TEXT,
    -- Identidad legal (requerido para auditoría y compliance)
    tipo_identidad          VARCHAR(20) NOT NULL,  -- DNI, RUC, PASAPORTE, CEDULA, NIT, OTRO
    numero_identidad        VARCHAR(50) NOT NULL,
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    creado_por              UUID,  -- UUID del usuario que creó
    modificado_por          UUID   -- UUID del usuario que modificó
);

COMMENT ON TABLE usuarios IS 'Usuarios del sistema con su perfil, empresa y datos de identidad legal';
COMMENT ON COLUMN usuarios.tipo_identidad IS 'Tipo de documento de identidad: DNI, RUC, PASAPORTE, CEDULA, NIT, OTRO';
COMMENT ON COLUMN usuarios.numero_identidad IS 'Número de documento de identidad único';
COMMENT ON COLUMN usuarios.auth_user_id IS 'ID del usuario en Supabase Auth (vinculación)';
COMMENT ON COLUMN usuarios.creado_por IS 'UUID del usuario que creó el registro (auditoría)';
COMMENT ON COLUMN usuarios.modificado_por IS 'UUID del usuario que modificó por última vez (auditoría)';

-- Índices
CREATE INDEX idx_usuarios_empresa_id ON usuarios(empresa_id);
CREATE INDEX idx_usuarios_perfil_id ON usuarios(perfil_id);
CREATE INDEX idx_usuarios_email ON usuarios(email);
CREATE INDEX idx_usuarios_activo ON usuarios(activo);
CREATE INDEX idx_usuarios_numero_identidad ON usuarios(numero_identidad);

-- Constraint único: número de identidad único por empresa
CREATE UNIQUE INDEX uq_usuarios_empresa_identidad ON usuarios(empresa_id, tipo_identidad, numero_identidad) WHERE activo = true;

-- RLS
ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;

-- Super Admin ve todos los usuarios
CREATE POLICY "super_admin_usuarios_all" ON usuarios
    FOR ALL
    USING (
        (current_setting('app.current_user_role', true)) = 'SUPER_ADMIN'
    );

-- Admin de tenant gestiona usuarios de su empresa
CREATE POLICY "tenant_admin_usuarios_manage" ON usuarios
    FOR ALL
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
        AND (current_setting('app.current_user_role', true)) IN ('ADMIN', 'SUPER_ADMIN')
    );

-- Usuario ve su propio perfil y compañeros de su empresa (solo lectura)
CREATE POLICY "user_usuarios_own_empresa" ON usuarios
    FOR SELECT
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
    );

-- Usuario puede actualizar su propio perfil (datos no sensibles)
CREATE POLICY "user_usuarios_update_own" ON usuarios
    FOR UPDATE
    USING (
        id = (current_setting('app.current_user_id', true))::UUID
    )
    WITH CHECK (
        id = (current_setting('app.current_user_id', true))::UUID
        AND empresa_id = (current_setting('app.current_empresa_id', true))::UUID
    );

-- ============================================================
-- TABLA: permisos (READ/CREATE/UPDATE por módulo y perfil)
-- ============================================================
CREATE TABLE IF NOT EXISTS permisos (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    perfil_id               UUID NOT NULL REFERENCES perfiles(id) ON DELETE CASCADE,
    modulo                  VARCHAR(100) NOT NULL,
    tipo                    VARCHAR(20) NOT NULL CHECK (tipo IN ('READ', 'CREATE', 'UPDATE')),
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por              UUID
);

COMMENT ON TABLE permisos IS 'Permisos granulares READ/CREATE/UPDATE por módulo y perfil. No existe DELETE: los registros solo se desactivan (activo=false)';
COMMENT ON COLUMN permisos.tipo IS 'Tipos de permiso: READ (ver/listar), CREATE (crear), UPDATE (editar/desactivar). No existe DELETE';
COMMENT ON COLUMN permisos.creado_por IS 'UUID del usuario que asignó el permiso (auditoría)';

-- Índices
CREATE INDEX idx_permisos_perfil_id ON permisos(perfil_id);
CREATE INDEX idx_permisos_modulo ON permisos(modulo);

-- Constraint único: un permiso por perfil, módulo y tipo
CREATE UNIQUE INDEX uq_permisos_perfil_modulo_tipo ON permisos(perfil_id, modulo, tipo) WHERE activo = true;

-- RLS
ALTER TABLE permisos ENABLE ROW LEVEL SECURITY;

-- Super Admin gestiona todos los permisos
CREATE POLICY "super_admin_permisos_all" ON permisos
    FOR ALL
    USING (
        (current_setting('app.current_user_role', true)) = 'SUPER_ADMIN'
    );

-- Admin de tenant gestiona permisos de su empresa (via join con perfiles)
CREATE POLICY "tenant_admin_permisos_manage" ON permisos
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM perfiles p
            WHERE p.id = permisos.perfil_id
            AND p.empresa_id = (current_setting('app.current_empresa_id', true))::UUID
            AND (current_setting('app.current_user_role', true)) IN ('ADMIN', 'SUPER_ADMIN')
        )
    );

-- Usuario ve permisos de su perfil (para saber qué puede hacer)
CREATE POLICY "user_permisos_own_perfil" ON permisos
    FOR SELECT
    USING (
        perfil_id = (current_setting('app.current_perfil_id', true))::UUID
        AND activo = true
    );

-- ============================================================
-- TABLA: auditoria_actividad (Log de accesos y actividad)
-- ============================================================
CREATE TABLE IF NOT EXISTS auditoria_actividad (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    usuario_id              UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modulo                  VARCHAR(100) NOT NULL,
    accion                  VARCHAR(50) NOT NULL,  -- LOGIN, LOGOUT, CREATE, READ, UPDATE, DEACTIVATE, EXPORT, ERROR
    entidad_tipo            VARCHAR(100),        -- Nombre de la tabla/entidad afectada
    entidad_id              UUID,                -- ID del registro afectado
    ip_address              INET,
    user_agent              TEXT,
    detalles                JSONB,               -- Detalles adicionales de la acción
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE auditoria_actividad IS 'Log de auditoría de accesos y actividad del sistema para compliance y seguridad';
COMMENT ON COLUMN auditoria_actividad.accion IS 'Acción realizada: LOGIN, LOGOUT, CREATE, READ, UPDATE, DEACTIVATE, EXPORT, ERROR';
COMMENT ON COLUMN auditoria_actividad.detalles IS 'JSON con detalles adicionales: campos modificados, valores anteriores, etc.';

-- Índices
CREATE INDEX idx_auditoria_empresa_id ON auditoria_actividad(empresa_id);
CREATE INDEX idx_auditoria_usuario_id ON auditoria_actividad(usuario_id);
CREATE INDEX idx_auditoria_modulo ON auditoria_actividad(modulo);
CREATE INDEX idx_auditoria_fecha_creacion ON auditoria_actividad(fecha_creacion DESC);
CREATE INDEX idx_auditoria_accion ON auditoria_actividad(accion);

-- RLS
ALTER TABLE auditoria_actividad ENABLE ROW LEVEL SECURITY;

-- Super Admin ve toda la auditoría
CREATE POLICY "super_admin_auditoria_all" ON auditoria_actividad
    FOR SELECT
    USING (
        (current_setting('app.current_user_role', true)) = 'SUPER_ADMIN'
    );

-- Admin de tenant ve auditoría de su empresa
CREATE POLICY "tenant_admin_auditoria_own" ON auditoria_actividad
    FOR SELECT
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
        AND (current_setting('app.current_user_role', true)) IN ('ADMIN', 'SUPER_ADMIN')
    );

-- ============================================================
-- FUNCIÓN: Actualizar fecha_modificacion automáticamente
-- ============================================================
CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers para actualizar fecha_modificacion
CREATE TRIGGER trigger_empresas_fecha_modificacion
    BEFORE UPDATE ON empresas
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

CREATE TRIGGER trigger_perfiles_fecha_modificacion
    BEFORE UPDATE ON perfiles
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

CREATE TRIGGER trigger_usuarios_fecha_modificacion
    BEFORE UPDATE ON usuarios
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

-- ============================================================
-- DATOS INICIALES: Super Admin y Empresa por defecto
-- ============================================================

-- NOTA: El Super Admin se crea via Supabase Auth Dashboard o script de seed
-- El rol SUPER_ADMIN se asigna en la tabla perfiles con es_super_admin = true
-- y empresa_id = NULL (o una empresa "system" especial)

-- Perfil base Super Admin (se crea manualmente o via seed)
-- INSERT INTO perfiles (id, empresa_id, nombre, descripcion, es_super_admin, es_admin_tenant, activo, creado_por)
-- VALUES (gen_random_uuid(), NULL, 'SUPER_ADMIN', 'Super Administrador Global - Acceso total al sistema', true, false, true, NULL);

-- Perfil base Admin Tenant (template)
-- INSERT INTO perfiles (id, empresa_id, nombre, descripcion, es_super_admin, es_admin_tenant, activo, creado_por)
-- VALUES (gen_random_uuid(), '<empresa_id>', 'ADMIN', 'Administrador de la empresa', false, true, true, '<user_id>');