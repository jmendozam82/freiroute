-- ============================================================
-- MIGRACIÓN 03 - TABLA perfiles
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Roles/perfiles de usuario por empresa. Cada tenant define sus
-- propios perfiles; los perfiles base del sistema (ADMIN, DISPATCHER,
-- OPERADOR, CONDUCTOR, CLIENTE) se crean automáticamente con
-- es_sistema = true (HU-001, HU-006).
-- Tabla de negocio: cumple la estructura obligatoria completa
-- (id, empresa_id, activo, fecha_creacion, fecha_modificacion) y RLS.
-- ============================================================

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

COMMENT ON TABLE perfiles IS
'Roles/perfiles de usuario por empresa. Cada empresa tiene sus propios perfiles y perfiles base del sistema con es_sistema = true.';
COMMENT ON COLUMN perfiles.id IS 'Identificador único del perfil (UUID generado por la BD)';
COMMENT ON COLUMN perfiles.empresa_id IS 'FK a empresas(id): discriminator multi-tenant — cada perfil pertenece a una sola empresa';
COMMENT ON COLUMN perfiles.nombre IS 'Nombre del perfil (ej: Administrador, Dispatcher, Operador)';
COMMENT ON COLUMN perfiles.descripcion IS 'Descripción de las responsabilidades del perfil';
COMMENT ON COLUMN perfiles.tipo_perfil IS 'Tipo de perfil: SUPER_ADMIN, ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE, CUSTOM';
COMMENT ON COLUMN perfiles.es_sistema IS 'true = perfil creado por el sistema (no se puede desactivar ni eliminar)';
COMMENT ON COLUMN perfiles.activo IS 'Soft delete universal: false = perfil desactivado';
COMMENT ON COLUMN perfiles.fecha_creacion IS 'Timestamp de creación del registro';
COMMENT ON COLUMN perfiles.fecha_modificacion IS 'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices obligatorios
CREATE INDEX idx_perfiles_empresa_id ON perfiles(empresa_id);
CREATE INDEX idx_perfiles_activo     ON perfiles(activo);
CREATE INDEX idx_perfiles_empresa_activo ON perfiles(empresa_id, activo);

-- Índice adicional para lookups por tipo de perfil dentro del tenant
CREATE INDEX idx_perfiles_empresa_tipo ON perfiles(empresa_id, tipo_perfil);

-- RLS: aislamiento multi-tenant (ADR-003)
ALTER TABLE perfiles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_perfiles" ON perfiles
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_perfiles_fecha_modificacion
    BEFORE UPDATE ON perfiles
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();