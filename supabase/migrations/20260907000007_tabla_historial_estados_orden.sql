-- ============================================================
-- TABLA: historial_estados_orden
-- Descripción: Auditoría inmutable de transiciones de estado (ADR-019).
--              Registros: INSERT-only. El trigger existe por
--              consistencia pero no debe dispararse en operación normal.
-- HU: HU-024
-- ============================================================

CREATE TABLE IF NOT EXISTS historial_estados_orden (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id        UUID NOT NULL REFERENCES ordenes(id),
    estado_anterior VARCHAR(30),
    estado_nuevo    VARCHAR(30) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID REFERENCES usuarios(id),
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE historial_estados_orden IS
    'Auditoría inmutable de todas las transiciones de estado de una orden.
     Cada cambio queda registrado con usuario, timestamp y motivo opcional.
     Los registros son INSERT-only; el UPDATE no ocurre en operación normal.
     El trigger de fecha_modificacion existe por consistencia del esquema.';

COMMENT ON COLUMN historial_estados_orden.estado_anterior IS
    'NULL en el primer registro del historial (inserción del DRAFT inicial).
     Contiene el estado previo en todas las demás transiciones.';

COMMENT ON COLUMN historial_estados_orden.motivo IS
    'Motivo del cambio de estado. Opcional en general; requerido en
     CANCELLED y ON_HOLD por política del negocio (validado en BLL).';

CREATE INDEX idx_historial_orden_empresa  ON historial_estados_orden(empresa_id);
CREATE INDEX idx_historial_orden_orden_id ON historial_estados_orden(orden_id);
-- Índice de fecha DESC para la vista del historial (orden cronológico inverso)
CREATE INDEX idx_historial_orden_fecha    ON historial_estados_orden(orden_id, fecha_creacion DESC);

ALTER TABLE historial_estados_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_historial_orden" ON historial_estados_orden
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_historial_estados_orden_fecha_mod
    BEFORE UPDATE ON historial_estados_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();