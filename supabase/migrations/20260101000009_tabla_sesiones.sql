-- ============================================================
-- MIGRACIÓN 09 - TABLA sesiones
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Reflect tokens activos por usuario (HU-003 CA-02, HU-007 CA-06).
-- El refresh token NUNCA se almacena en claro: solo su hash SHA-256.
--
-- NOTA DE ARMONIZACIÓN (@IngenieroDatos, 2026-09):
--   El campo de soft-revoke se llama "activa" (NO "activo") para
--   mantener coherencia TOTAL con:
--     - Entidad Sesion.cs (propiedad Activa)
--     - SesionRepository.cs (INSERT ... activa / UPDATE SET activa = false)
--   que @BackendDev ya dejó implementados. Los índices, RLS y trigger
--   siguen el estándar de la casa (ADR-003, ADR-004).
-- ============================================================

CREATE TABLE IF NOT EXISTS sesiones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    refresh_token_hash  VARCHAR(64) NOT NULL UNIQUE,
    fecha_expiracion    TIMESTAMPTZ NOT NULL,
    activa              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE sesiones IS
'Refresh tokens activos por usuario. Solo se almacena el hash SHA-256 del token real, nunca el token en claro. activa=false invalida el token sin eliminar el registro (soft revoke).';
COMMENT ON COLUMN sesiones.id IS 'Identificador único de la sesión (UUID generado por la BD)';
COMMENT ON COLUMN sesiones.empresa_id IS 'FK a empresas(id) ON DELETE CASCADE: tenant al que pertenece la sesión';
COMMENT ON COLUMN sesiones.usuario_id IS 'FK a usuarios(id) ON DELETE CASCADE: usuario dueño de la sesión';
COMMENT ON COLUMN sesiones.refresh_token_hash IS 'Hash SHA-256 del refresh token real. Único — busca la sesión por este hash';
COMMENT ON COLUMN sesiones.fecha_expiracion IS 'Timestamp de expiración del refresh token (30 días por defecto, HU-003 CA-02)';
COMMENT ON COLUMN sesiones.activa IS 'Soft revoke del refresh token: false = token invalidado (logout o reset de password) sin eliminar el registro';
COMMENT ON COLUMN sesiones.fecha_creacion IS 'Timestamp de creación de la sesión';
COMMENT ON COLUMN sesiones.fecha_modificacion IS 'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índice obligatorio + índice de negocio: lookup por tenant
CREATE INDEX idx_sesiones_empresa_id ON sesiones(empresa_id);
CREATE INDEX idx_sesiones_usuario_id ON sesiones(usuario_id);
CREATE INDEX idx_sesiones_activa     ON sesiones(activa);

-- Índice para búsqueda rápida del refresh token en el login/refresh (hash es UNIQUE)
CREATE INDEX idx_sesiones_token_hash ON sesiones(refresh_token_hash);

-- RLS: aislamiento multi-tenant (ADR-003)
ALTER TABLE sesiones ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_sesiones" ON sesiones
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_sesiones_fecha_modificacion
    BEFORE UPDATE ON sesiones
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();