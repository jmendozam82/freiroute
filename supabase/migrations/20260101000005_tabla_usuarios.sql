-- ============================================================
-- MIGRACIÓN 05 - TABLA usuarios
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Usuarios del sistema por empresa. La autenticación la gestiona
-- Supabase Auth; este registro guarda el perfil de negocio y el
-- vínculo con auth.users (supabase_user_id).
-- Tabla de negocio: estructura obligatoria completa + RLS.
-- ============================================================

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
    -- Estado y seguridad de cuenta
    tipo_usuario        VARCHAR(50) NOT NULL DEFAULT 'OPERADOR',
    estado              VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    ultimo_acceso       TIMESTAMPTZ,
    intentos_fallidos   INTEGER NOT NULL DEFAULT 0,
    bloqueado_hasta     TIMESTAMPTZ,
    -- Control (soft delete universal)
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_usuario_email_empresa UNIQUE (email, empresa_id)
);

COMMENT ON TABLE usuarios IS
'Usuarios del sistema por empresa. La autenticación la gestiona Supabase Auth; este registro guarda el perfil de negocio y el vínculo con auth.users.';
COMMENT ON COLUMN usuarios.id IS 'Identificador único del usuario (UUID generado por la BD)';
COMMENT ON COLUMN usuarios.empresa_id IS 'FK a empresas(id): discriminator multi-tenant — cada usuario pertenece a una sola empresa';
COMMENT ON COLUMN usuarios.perfil_id IS 'FK a perfiles(id): perfil/rol asignado al usuario (define sus permisos por módulo)';
COMMENT ON COLUMN usuarios.tipo_identidad IS 'Tipo de documento de identidad: CEDULA, PASAPORTE, RUC, DNI';
COMMENT ON COLUMN usuarios.numero_identidad IS 'Número del documento de identidad';
COMMENT ON COLUMN usuarios.nombre_completo IS 'Nombre completo del usuario';
COMMENT ON COLUMN usuarios.email IS 'Email del usuario. Único por empresa (uq_usuario_email_empresa)';
COMMENT ON COLUMN usuarios.telefono IS 'Teléfono de contacto del usuario';
COMMENT ON COLUMN usuarios.foto_url IS 'URL de la foto de perfil en Supabase Storage (bucket privado)';
COMMENT ON COLUMN usuarios.supabase_user_id IS 'FK hacia auth.users de Supabase: vincula la identidad de autenticación con el perfil de negocio. Único a nivel global';
COMMENT ON COLUMN usuarios.tipo_usuario IS 'Tipo de usuario: SUPER_ADMIN, ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE';
COMMENT ON COLUMN usuarios.estado IS 'Estado de la cuenta: PENDING (invitado), ACTIVE (activo), SUSPENDED, LOCKED';
COMMENT ON COLUMN usuarios.ultimo_acceso IS 'Timestamp del último login exitoso (HU-003)';
COMMENT ON COLUMN usuarios.intentos_fallidos IS 'Contador de intentos de login fallidos consecutivos (bloqueo al llegar a 5)';
COMMENT ON COLUMN usuarios.bloqueado_hasta IS 'Timestamp hasta cuando la cuenta queda bloqueada (NOW() + 30 min tras 5 intentos fallidos)';
COMMENT ON COLUMN usuarios.activo IS 'Soft delete universal: false = usuario desactivado (nunca se borra físicamente)';
COMMENT ON COLUMN usuarios.fecha_creacion IS 'Timestamp de creación del registro';
COMMENT ON COLUMN usuarios.fecha_modificacion IS 'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices obligatorios
CREATE INDEX idx_usuarios_empresa_id     ON usuarios(empresa_id);
CREATE INDEX idx_usuarios_activo         ON usuarios(activo);
CREATE INDEX idx_usuarios_empresa_activo ON usuarios(empresa_id, activo);

-- Índices adicionales: lookups frecuentes de auth y negocio
CREATE INDEX idx_usuarios_perfil_id      ON usuarios(perfil_id);
CREATE INDEX idx_usuarios_email          ON usuarios(email);
CREATE INDEX idx_usuarios_supabase_user_id ON usuarios(supabase_user_id);

-- RLS: aislamiento multi-tenant (ADR-003)
ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_usuarios" ON usuarios
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_usuarios_fecha_modificacion
    BEFORE UPDATE ON usuarios
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();