-- ============================================================
-- MIGRACIÓN 04 - TABLA permisos
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Permisos granulares por perfil y módulo (ADR-009).
-- Modelo de FLAGS BOOLEANOS: puede_leer / puede_crear / puede_actualizar.
--   - NO existe columna 'tipo' (modelo obsoleto descartado)
--   - NO existe columna 'nombre'
--   - Solo existen 3 niveles de permiso: READ, CREATE, UPDATE
--     (no existe DELETE — ver AGENTS.md regla 22)
-- ============================================================

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

COMMENT ON TABLE permisos IS
'Permisos granulares por módulo para cada perfil. Flags booleanos: puede_leer, puede_crear, puede_actualizar. No existe DELETE.';
COMMENT ON COLUMN permisos.id IS 'Identificador único del permiso (UUID generado por la BD)';
COMMENT ON COLUMN permisos.empresa_id IS 'FK a empresas(id): discriminator multi-tenant — redundante con perfil_id para RLS y filtros directos';
COMMENT ON COLUMN permisos.perfil_id IS 'FK a perfiles(id) ON DELETE CASCADE: perfil al que pertenece el permiso';
COMMENT ON COLUMN permisos.modulo IS 'Nombre del módulo del TMS: ordenes, embarques, carriers, rutas, track_trace, documentos, flota, analytics, facturacion, clientes, usuarios, configuracion';
COMMENT ON COLUMN permisos.puede_leer IS 'Permiso READ: ver listados y detalles del módulo';
COMMENT ON COLUMN permisos.puede_crear IS 'Permiso CREATE: crear nuevos registros del módulo';
COMMENT ON COLUMN permisos.puede_actualizar IS 'Permiso UPDATE: editar y desactivar registros del módulo';
COMMENT ON COLUMN permisos.activo IS 'Soft delete universal: false = permiso desactivado';
COMMENT ON COLUMN permisos.fecha_creacion IS 'Timestamp de creación del registro';
COMMENT ON COLUMN permisos.fecha_modificacion IS 'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices obligatorios
CREATE INDEX idx_permisos_empresa_id ON permisos(empresa_id);
CREATE INDEX idx_permisos_activo     ON permisos(activo);
CREATE INDEX idx_permisos_empresa_activo ON permisos(empresa_id, activo);

-- Índice adicional: lookups por perfil (GET /api/perfiles/{id}/permisos)
CREATE INDEX idx_permisos_perfil_id ON permisos(perfil_id);

-- RLS: aislamiento multi-tenant (ADR-003)
ALTER TABLE permisos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_permisos" ON permisos
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_permisos_fecha_modificacion
    BEFORE UPDATE ON permisos
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();