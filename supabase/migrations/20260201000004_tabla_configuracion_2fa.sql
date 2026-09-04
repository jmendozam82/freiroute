-- ============================================================
-- MIGRACIÓN 04 - TABLA configuracion_2fa
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Configuración de autenticación de dos factores por usuario.
-- Patrón estándar con RLS — datos sensibles del tenant.
-- Puntos críticos:
--   - totp_secret almacenado CIFRADO con AES-256-GCM (ADR-011)
--   - codigos_recuperacion como hashes SHA-256
--   - Un usuario tiene exactamente una configuración 2FA (UNIQUE)
-- ============================================================

CREATE TABLE IF NOT EXISTS configuracion_2fa (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Relaciones
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    -- TOTP
    totp_secret         VARCHAR(1000),
    totp_habilitado     BOOLEAN NOT NULL DEFAULT false,
    -- Email 2FA
    email_habilitado    BOOLEAN NOT NULL DEFAULT false,
    -- Códigos de recuperación (almacenados como hashes SHA-256)
    codigos_recuperacion TEXT[],
    -- Control (soft delete universal)
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    -- Un usuario tiene exactamente una configuración 2FA
    CONSTRAINT uq_2fa_usuario UNIQUE (usuario_id)
);

COMMENT ON TABLE configuracion_2fa IS
    'Configuración de autenticación de dos factores por usuario. El totp_secret se almacena cifrado con AES-256-GCM.';
COMMENT ON COLUMN configuracion_2fa.id IS
    'Identificador único de la configuración 2FA (UUID generado por la BD)';
COMMENT ON COLUMN configuracion_2fa.empresa_id IS
    'FK a la empresa del usuario. Para RLS y aislamiento multi-tenant.';
COMMENT ON COLUMN configuracion_2fa.usuario_id IS
    'FK al usuario. Constraint UNIQUE: un usuario tiene exactamente una configuración 2FA.';
COMMENT ON COLUMN configuracion_2fa.totp_secret IS
    'Secret TOTP cifrado con AES-256-GCM (ADR-011). Formato: base64(iv[12] + tag[16] + ciphertext). VARCHAR(1000) para acomodar el texto cifrado.';
COMMENT ON COLUMN configuracion_2fa.totp_habilitado IS
    'true si el usuario tiene habilitada la autenticación TOTP (app autenticadora)';
COMMENT ON COLUMN configuracion_2fa.email_habilitado IS
    'true si el usuario tiene habilitada la autenticación por email (código 6 dígitos)';
COMMENT ON COLUMN configuracion_2fa.codigos_recuperacion IS
    'Array de 8 hashes SHA-256 de códigos de recuperación de un solo uso';
COMMENT ON COLUMN configuracion_2fa.activo IS
    'Soft delete: false = 2FA desactivado para este usuario';
COMMENT ON COLUMN configuracion_2fa.fecha_creacion IS
    'Timestamp de creación del registro';
COMMENT ON COLUMN configuracion_2fa.fecha_modificacion IS
    'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices
CREATE INDEX idx_2fa_empresa_id ON configuracion_2fa(empresa_id);
CREATE INDEX idx_2fa_usuario_id ON configuracion_2fa(usuario_id);

-- RLS — datos sensibles del tenant (ADR-003)
ALTER TABLE configuracion_2fa ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_configuracion_2fa" ON configuracion_2fa
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_2fa_fecha_modificacion
    BEFORE UPDATE ON configuracion_2fa
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
