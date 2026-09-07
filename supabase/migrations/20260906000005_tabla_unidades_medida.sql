-- ============================================================
-- MIGRACIÓN 05 - TABLA unidades_medida
-- Freiroute TMS - Sprint 3 EP-03 (HU-018, ADR-003)
-- ============================================================
-- Catálogo de unidades de medida del tenant para peso, volumen,
-- longitud y temperatura. Incluye el factor de conversión a la
-- unidad base (ej: 1 lb = 0.453592 kg → factor = 0.453592).
-- Las unidades estándar se siembran en la empresa raíz (migración
-- 20260906000011) y se copian a cada tenant nuevo al crearlo
-- (HU-018 CA-02 — UnidadMedidaRepository.CopiarUnidadesEstandarAsync).
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS unidades_medida (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    simbolo             VARCHAR(20) NOT NULL,
    tipo                VARCHAR(50) NOT NULL,
    factor_conversion   NUMERIC(18,8) NOT NULL DEFAULT 1,
    unidad_base         VARCHAR(20) NOT NULL,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_unidad_simbolo_empresa UNIQUE (empresa_id, simbolo)
);

COMMENT ON TABLE unidades_medida IS
    'Catálogo de unidades de medida del tenant para peso, volumen
     y dimensiones. Incluye el factor de conversión a la unidad base.';
COMMENT ON COLUMN unidades_medida.tipo IS
    'PESO, VOLUMEN, LONGITUD, TEMPERATURA';
COMMENT ON COLUMN unidades_medida.factor_conversion IS
    'Factor para convertir a la unidad base del tipo.
     Ej: 1 lb = 0.453592 kg → factor = 0.453592';
COMMENT ON COLUMN unidades_medida.unidad_base IS
    'Unidad base del sistema: kg (peso), m3 (volumen), m (longitud), C (temp)';

CREATE INDEX idx_unidades_empresa_id     ON unidades_medida(empresa_id);
CREATE INDEX idx_unidades_activo         ON unidades_medida(activo);
CREATE INDEX idx_unidades_tipo           ON unidades_medida(tipo);
CREATE INDEX idx_unidades_empresa_activo ON unidades_medida(empresa_id, activo);

ALTER TABLE unidades_medida ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_unidades" ON unidades_medida
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_unidades_fecha_modificacion
    BEFORE UPDATE ON unidades_medida
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();