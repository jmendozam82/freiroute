-- ============================================================
-- TABLA: shipments (esqueleto mínimo)
-- Descripción: Embarques de transporte. Esqueleto creado en
--              Sprint 4 para soportar la FK ordenes.shipment_id.
--              Módulo completo implementado en Sprint 7 (EP-06).
-- ⚠️ NO AGREGAR COLUMNAS ADICIONALES — Sprint 7 las define.
-- ============================================================

CREATE TABLE IF NOT EXISTS shipments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    numero_shipment VARCHAR(30),
    estado          VARCHAR(30) NOT NULL DEFAULT 'PLANNED',
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE shipments IS
    'Embarques de transporte. Esqueleto mínimo creado en Sprint 4
     para soportar ordenes.shipment_id. Módulo completo en Sprint 7.
     ⚠ No modificar columnas — Sprint 7 agrega las operativas.';

COMMENT ON COLUMN shipments.estado IS
    'Estado del shipment: PLANNED | IN_PROGRESS | COMPLETED | CANCELLED.
     Solo PLANNED se usa en Sprint 4. El resto en Sprint 7.';

CREATE INDEX idx_shipments_empresa_id ON shipments(empresa_id);
CREATE INDEX idx_shipments_activo     ON shipments(activo);

ALTER TABLE shipments ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_shipments" ON shipments
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_shipments_fecha_modificacion
    BEFORE UPDATE ON shipments
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();