-- ============================================================
-- MIGRACIÓN 03 - TABLA ubicacion_zonas (relación N:N)
-- Freiroute TMS - Sprint 3 EP-03 (HU-016, ADR-003, ADR-018)
-- ============================================================
-- Relación muchos-a-muchos: una ubicación puede pertenecer a
-- múltiples zonas de entrega y una zona agrupa muchas ubicaciones.
--
-- EXCEPCIONES arquitectónicas documentadas:
--   - SIN activo: tabla de relación pura, no es dato de negocio.
--   - SIN fecha_modificacion: append-only; la desasignación usa
--     DELETE físico (única excepción al soft delete del ADR-005,
--     justificada por ADR-018: la relación no es un registro histórico).
--   - SIN trigger update_fecha_modificacion (no hay columna).
--   - SÍ tiene RLS propio por consistencia (ADR-003).
-- ============================================================

CREATE TABLE IF NOT EXISTS ubicacion_zonas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    ubicacion_id    UUID NOT NULL REFERENCES ubicaciones(id) ON DELETE CASCADE,
    zona_id         UUID NOT NULL REFERENCES zonas_entrega(id) ON DELETE CASCADE,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_ubicacion_zona UNIQUE (ubicacion_id, zona_id)
);

COMMENT ON TABLE ubicacion_zonas IS
    'Relación muchos-a-muchos: una ubicación puede pertenecer a
     múltiples zonas de entrega. Tabla de relación pura — la
     desasignación usa DELETE físico (excepción ADR-005, ADR-018).';

CREATE INDEX idx_ubiz_empresa_id   ON ubicacion_zonas(empresa_id);
CREATE INDEX idx_ubiz_ubicacion_id ON ubicacion_zonas(ubicacion_id);
CREATE INDEX idx_ubiz_zona_id      ON ubicacion_zonas(zona_id);

ALTER TABLE ubicacion_zonas ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ubicacion_zonas" ON ubicacion_zonas
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);