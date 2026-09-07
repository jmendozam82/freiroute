-- ============================================================
-- MIGRACIÓN 06 - TABLA tipos_embalaje
-- Freiroute TMS - Sprint 3 EP-03 (HU-018, ADR-003)
-- ============================================================
-- Tipos de embalaje del tenant: pallets, cajas, tambores,
-- contenedores. Usados al registrar la carga en las órdenes de
-- transporte (Sprint 4-5).
-- Los embalajes estándar (PLT, CAJA, TAM, CTN20, CTN40, GRA, BOB)
-- se siembran en la empresa raíz (migración 20260906000011) y se
-- copian a cada tenant nuevo (HU-018 CA-04).
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS tipos_embalaje (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    codigo              VARCHAR(20) NOT NULL,
    descripcion         TEXT,
    capacidad_kg        NUMERIC(10,3),
    capacidad_m3        NUMERIC(10,3),
    apilable            BOOLEAN NOT NULL DEFAULT true,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_embalaje_codigo_empresa UNIQUE (empresa_id, codigo)
);

COMMENT ON TABLE tipos_embalaje IS
    'Tipos de embalaje del tenant: pallets, cajas, tambores, contenedores.
     Usados al registrar la carga en las órdenes de transporte.';
COMMENT ON COLUMN tipos_embalaje.codigo IS
    'Código corto: PLT (pallet), CAJA, TAM (tambor), CTN (contenedor),
     BOB (bobina), GRA (a granel)';

CREATE INDEX idx_embalaje_empresa_id     ON tipos_embalaje(empresa_id);
CREATE INDEX idx_embalaje_activo         ON tipos_embalaje(activo);
CREATE INDEX idx_embalaje_empresa_activo ON tipos_embalaje(empresa_id, activo);

ALTER TABLE tipos_embalaje ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_embalaje" ON tipos_embalaje
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_embalaje_fecha_modificacion
    BEFORE UPDATE ON tipos_embalaje
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();