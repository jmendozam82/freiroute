-- ============================================================
-- TABLA: historial_estados_reclamo (HU-032 — Claims Management)
-- Historial de transiciones de estado de un reclamo. Auditoría
-- inmutable — INSERT-only en operación normal (mismo patrón que
-- historial_estados_orden, ADR-019).
-- ============================================================

CREATE TABLE IF NOT EXISTS historial_estados_reclamo (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID        NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    reclamo_id      UUID        NOT NULL REFERENCES reclamos(id),
    estado_anterior VARCHAR(20),
    estado_nuevo    VARCHAR(20) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID        REFERENCES usuarios(id),
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE historial_estados_reclamo IS
    'Historial de transiciones de estado de reclamos. Auditoría inmutable
     (INSERT-only, sin soft delete). Mismo patrón que historial_estados_orden (ADR-019).';

COMMENT ON COLUMN historial_estados_reclamo.estado_anterior IS
    'Estado previo a la transición. NULL en el registro de creación del reclamo.';

COMMENT ON COLUMN historial_estados_reclamo.estado_nuevo IS
    'Estado resultante de la transición: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO.';

COMMENT ON COLUMN historial_estados_reclamo.motivo IS
    'Motivo obligatorio de la transición (HU-032 CA-04).';

ALTER TABLE historial_estados_reclamo ENABLE ROW LEVEL SECURITY;

-- Aislamiento multi-tenant (ADR-003)
CREATE POLICY historial_reclamo_tenant_isolation ON historial_estados_reclamo
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

-- Índice de consulta por reclamo dentro del tenant
CREATE INDEX IF NOT EXISTS idx_historial_reclamo_reclamo
    ON historial_estados_reclamo(empresa_id, reclamo_id);
