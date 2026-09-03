-- ============================================================
-- MIGRACIÓN 06 - TABLA invitaciones
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Invitaciones de usuarios por email. El token expira en 48 horas.
-- EXCEPCIONES arquitectónicas:
--   - NO tiene campo activo: su ciclo de vida se controla con el
--     campo Estado (PENDING | ACCEPTED | EXPIRED | CANCELLED)
--   - NO tiene trigger de fecha_modificacion: es append-only
--     (los estados se cambian por UPDATE del campo estado, sin
--     necesidad de rastrear modificación — el log de vida está
--     en auditoria_actividad)
--   - SÍ tiene RLS (empresa_isolation_invitaciones)
-- ============================================================

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

COMMENT ON TABLE invitaciones IS
'Invitaciones de usuarios por email. El token expira en 48 horas. Tabla append-only: su ciclo de vida se controla con el campo estado (PENDING, ACCEPTED, EXPIRED, CANCELLED).';
COMMENT ON COLUMN invitaciones.id IS 'Identificador único de la invitación (UUID generado por la BD)';
COMMENT ON COLUMN invitaciones.empresa_id IS 'FK a empresas(id) ON DELETE CASCADE: tenant que emite la invitación';
COMMENT ON COLUMN invitaciones.email IS 'Email del usuario invitado';
COMMENT ON COLUMN invitaciones.perfil_id IS 'FK a perfiles(id): perfil que se asignará al usuario al aceptar la invitación';
COMMENT ON COLUMN invitaciones.token IS 'Token único de invitación (usado en el link de aceptación). Un solo uso';
COMMENT ON COLUMN invitaciones.estado IS 'Estado de la invitación: PENDING, ACCEPTED, EXPIRED, CANCELLED';
COMMENT ON COLUMN invitaciones.fecha_expiracion IS 'Timestamp de expiración del token (48 horas desde la creación)';
COMMENT ON COLUMN invitaciones.fecha_aceptacion IS 'Timestamp en que el usuario aceptó la invitación';
COMMENT ON COLUMN invitaciones.creado_por_id IS 'FK a usuarios(id): usuario del tenant que emitió la invitación';
COMMENT ON COLUMN invitaciones.fecha_creacion IS 'Timestamp de creación de la invitación';

-- Índices adicionales: lookups de invitación
CREATE INDEX idx_invitaciones_token   ON invitaciones(token);
CREATE INDEX idx_invitaciones_empresa ON invitaciones(empresa_id);
CREATE INDEX idx_invitaciones_email   ON invitaciones(email);

-- RLS: aislamiento multi-tenant (ADR-003)
ALTER TABLE invitaciones ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_invitaciones" ON invitaciones
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);