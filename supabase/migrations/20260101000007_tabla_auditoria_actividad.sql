-- ============================================================
-- MIGRACIÓN 07 - TABLA auditoria_actividad
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Log inmutable de todas las acciones del sistema (HU-008).
-- EXCEPCIONES arquitectónicas:
--   - Es INMUTABLE: NO tiene activo, NO tiene fecha_modificacion,
--     NO tiene trigger de UPDATE
--   - La política RLS es solo FOR SELECT (el INSERT lo hace el
--     sistema interno vía service_role, no el usuario final)
--   - Retención mínima: 12 meses (registros antiguos se archivan,
--     nunca se eliminan)
--   - Índice especial: idx_auditoria_fecha en fecha_creacion DESC
-- ============================================================

CREATE TABLE IF NOT EXISTS auditoria_actividad (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID REFERENCES empresas(id) ON DELETE SET NULL,
    usuario_id      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modulo          VARCHAR(100) NOT NULL,
    accion          VARCHAR(50) NOT NULL,
    entidad_tipo    VARCHAR(100),
    entidad_id      UUID,
    ip_address      INET,
    user_agent      TEXT,
    detalles        JSONB,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE auditoria_actividad IS
'Log inmutable de todas las acciones del sistema. Nadie puede editar ni eliminar registros. Retención mínima: 12 meses (los más antiguos se archivan, no se eliminan).';
COMMENT ON COLUMN auditoria_actividad.id IS 'Identificador único del registro de auditoría (UUID generado por la BD)';
COMMENT ON COLUMN auditoria_actividad.empresa_id IS 'FK a empresas(id) ON DELETE SET NULL: tenant afectado. Nullable porque el SUPER_ADMIN opera sin tenant';
COMMENT ON COLUMN auditoria_actividad.usuario_id IS 'FK a usuarios(id) ON DELETE SET NULL: usuario que realizó la acción';
COMMENT ON COLUMN auditoria_actividad.modulo IS 'Módulo del TMS afectado: auth, empresas, perfiles, usuarios, ordenes, embarques, etc.';
COMMENT ON COLUMN auditoria_actividad.accion IS 'Acción realizada: LOGIN, LOGOUT, LOGIN_FAILED, CREATE, UPDATE, DEACTIVATE, EXPORT, VIEW, CAMBIO_ESTADO';
COMMENT ON COLUMN auditoria_actividad.entidad_tipo IS 'Nombre de la entidad afectada (ej: Usuario, Perfil, Embarque)';
COMMENT ON COLUMN auditoria_actividad.entidad_id IS 'ID del registro afectado';
COMMENT ON COLUMN auditoria_actividad.ip_address IS 'Dirección IP del cliente (tipo INET de PostgreSQL)';
COMMENT ON COLUMN auditoria_actividad.user_agent IS 'User-Agent del navegador/cliente que realizó la acción';
COMMENT ON COLUMN auditoria_actividad.detalles IS 'JSON con datos adicionales: valores anteriores/nuevos, contexto de la acción';
COMMENT ON COLUMN auditoria_actividad.fecha_creacion IS 'Timestamp inmutable de la acción registrada';

-- Índice obligatorio: lookups por empresa para el panel de auditoría
CREATE INDEX idx_auditoria_empresa_id ON auditoria_actividad(empresa_id);

-- Índice especial: ordenación por fecha DESC (consultas de auditoría por rango de fechas)
CREATE INDEX idx_auditoria_fecha ON auditoria_actividad(fecha_creacion DESC);

-- Índices adicionales: filtros frecuentes del panel (HU-008)
CREATE INDEX idx_auditoria_usuario_id ON auditoria_actividad(usuario_id);
CREATE INDEX idx_auditoria_modulo     ON auditoria_actividad(modulo);
CREATE INDEX idx_auditoria_accion     ON auditoria_actividad(accion);

-- RLS: aislamiento multi-tenant SOLO para lectura (ADR-003)
ALTER TABLE auditoria_actividad ENABLE ROW LEVEL SECURITY;

-- Política de solo SELECT: el INSERT lo realiza el sistema interno
-- (service_role), nunca el usuario final.
CREATE POLICY "empresa_isolation_auditoria" ON auditoria_actividad
    FOR SELECT
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);