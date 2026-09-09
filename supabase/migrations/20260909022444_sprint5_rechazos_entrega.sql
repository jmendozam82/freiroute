-- ============================================================
-- TABLA: rechazos_entrega (HU-030 — Gestión de rechazos)
-- Registra rechazos de entrega de órdenes en campo. Relación N:1
-- con ordenes. El registro del rechazo mueve la orden a
-- FAILED_DELIVERY vía FSM (ADR-019).
-- Motivos: CLIENTE_AUSENTE | DIRECCION_INCORRECTA |
--          MERCANCIA_DANADA | RECHAZO_CLIENTE | OTRO
-- ============================================================

CREATE TABLE IF NOT EXISTS rechazos_entrega (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id     UUID        NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id       UUID        NOT NULL REFERENCES ordenes(id),
    motivo         VARCHAR(40) NOT NULL,
    descripcion    TEXT,
    usuario_id     UUID        REFERENCES usuarios(id),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    activo         BOOLEAN     NOT NULL DEFAULT true
);

COMMENT ON TABLE rechazos_entrega IS
    'Registra rechazos de entrega de órdenes. Relación N:1 con ordenes.
     Motivos: CLIENTE_AUSENTE | DIRECCION_INCORRECTA | MERCANCIA_DANADA |
     RECHAZO_CLIENTE | OTRO. El registro del rechazo mueve la orden a
     FAILED_DELIVERY vía FSM (ADR-019, HU-030 CA-03).';

COMMENT ON COLUMN rechazos_entrega.motivo IS
    'Motivo del rechazo: CLIENTE_AUSENTE | DIRECCION_INCORRECTA |
     MERCANCIA_DANADA | RECHAZO_CLIENTE | OTRO.';

COMMENT ON COLUMN rechazos_entrega.usuario_id IS
    'Usuario que registra el rechazo en campo.';

ALTER TABLE rechazos_entrega ENABLE ROW LEVEL SECURITY;

-- Aislamiento multi-tenant (ADR-003): solo ve registros de su empresa.
CREATE POLICY rechazos_entrega_tenant_isolation ON rechazos_entrega
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

-- Índices operativos
CREATE INDEX IF NOT EXISTS idx_rechazos_entrega_orden
    ON rechazos_entrega(empresa_id, orden_id);

CREATE INDEX IF NOT EXISTS idx_rechazos_entrega_empresa
    ON rechazos_entrega(empresa_id, activo);
