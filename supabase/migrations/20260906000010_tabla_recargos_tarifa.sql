-- ============================================================
-- MIGRACIÓN 10 - TABLA recargos_tarifa
-- Freiroute TMS - Sprint 3 EP-03 (HU-020, ADR-003, ADR-015)
-- ============================================================
-- Recargos aplicables a cada tarifa base: combustible, peaje, seguro,
-- manipulación, urgencia, refrigeración, sobredimensión (ADR-015).
--
-- NOTA DE DOMINIO: el recargo HEREDA la vigencia de su tarifa padre
-- (tarifa_id → tarifas_base). No tiene fechas propias; cuando una
-- tarifa se versiona (CerrarVigenciaAsync + nueva versión), la BLL
-- copia los recargos activos a la nueva versión de la tarifa.
--
-- UNIQUE (tarifa_id, codigo_recargo): un mismo recargo (ej: 'PEAJE')
-- no puede repetirse dentro de la misma tarifa.
--
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS recargos_tarifa (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    tarifa_id       UUID NOT NULL REFERENCES tarifas_base(id) ON DELETE CASCADE,
    codigo_recargo  VARCHAR(50) NOT NULL,
    nombre          VARCHAR(200) NOT NULL,
    tipo_calculo    VARCHAR(30) NOT NULL DEFAULT 'PORCENTAJE',
    valor           NUMERIC(10,4) NOT NULL,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,
    CONSTRAINT uq_recargo_tarifa_codigo UNIQUE (tarifa_id, codigo_recargo)
);

COMMENT ON TABLE recargos_tarifa IS
    'Recargos aplicables a cada tarifa base: combustible, peaje, seguro,
     manipulación, urgencia, refrigeración, sobredimensión. Hereda la
     vigencia de su tarifa padre (ADR-015).';
COMMENT ON COLUMN recargos_tarifa.tipo_calculo IS
    'PORCENTAJE (% sobre tarifa base), MONTO_FIJO (valor absoluto en moneda)';
COMMENT ON COLUMN recargos_tarifa.codigo_recargo IS
    'COMBUSTIBLE, PEAJE, SEGURO, MANIPULACION, URGENCIA,
     REFRIGERACION, SOBREDIMENSION';

CREATE INDEX idx_recargos_tarifa_id    ON recargos_tarifa(tarifa_id);
CREATE INDEX idx_recargos_empresa_id   ON recargos_tarifa(empresa_id);

ALTER TABLE recargos_tarifa ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_recargos" ON recargos_tarifa
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_recargos_fecha_modificacion
    BEFORE UPDATE ON recargos_tarifa
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();