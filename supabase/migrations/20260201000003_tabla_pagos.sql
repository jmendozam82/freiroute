-- ============================================================
-- MIGRACIÓN 03 - TABLA pagos (Append-only / Inmutable)
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Registro de pagos de suscripción. Es INMUTABLE (ADR-004):
--   - SIN campo activo (los pagos nunca se desactivan)
--   - SIN campo fecha_modificacion (los pagos no se editan)
--   - SIN trigger de fecha_modificacion
--   - SIN RLS (el Super Admin gestiona todos los pagos)
-- Solo se ejecutan INSERT — nunca UPDATE ni DELETE.
-- ============================================================

CREATE TABLE IF NOT EXISTS pagos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Relaciones
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    suscripcion_id      UUID NOT NULL REFERENCES suscripciones(id) ON DELETE RESTRICT,
    -- Datos del pago
    monto               NUMERIC(10,2) NOT NULL,
    moneda              VARCHAR(10) NOT NULL DEFAULT 'USD',
    metodo_pago         VARCHAR(50) NOT NULL DEFAULT 'MANUAL',
    referencia          VARCHAR(200),
    notas               TEXT,
    -- Estado
    estado              VARCHAR(50) NOT NULL DEFAULT 'COMPLETED',
    -- Período cubierto
    periodo_desde       TIMESTAMPTZ NOT NULL,
    periodo_hasta       TIMESTAMPTZ NOT NULL,
    -- Auditoría de creación
    registrado_por_id   UUID REFERENCES usuarios(id),
    -- Timestamp inmutable — SIN fecha_modificacion
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- NOTA: No hay campo 'activo' ni 'fecha_modificacion'
    -- Los pagos son append-only: solo INSERT, nunca UPDATE ni DELETE
);

COMMENT ON TABLE pagos IS
    'Registro de pagos de suscripción. Tabla inmutable — no se editan ni eliminan registros. Solo INSERT.';
COMMENT ON COLUMN pagos.id IS
    'Identificador único del pago (UUID generado por la BD)';
COMMENT ON COLUMN pagos.empresa_id IS
    'FK a la empresa (tenant) que realizó el pago';
COMMENT ON COLUMN pagos.suscripcion_id IS
    'FK a la suscripción a la que corresponde el pago';
COMMENT ON COLUMN pagos.monto IS
    'Monto total del pago en la moneda especificada';
COMMENT ON COLUMN pagos.moneda IS
    'Moneda del pago: USD, EUR, etc.';
COMMENT ON COLUMN pagos.metodo_pago IS
    'Método de pago: MANUAL, STRIPE, PAYPAL, TRANSFERENCIA, EFECTIVO';
COMMENT ON COLUMN pagos.referencia IS
    'Referencia externa del pago (número de transferencia, ID de transacción)';
COMMENT ON COLUMN pagos.notas IS
    'Notas o comentarios del pago registrado manualmente';
COMMENT ON COLUMN pagos.estado IS
    'Estado del pago: COMPLETED, PENDING, FAILED, REFUNDED';
COMMENT ON COLUMN pagos.periodo_desde IS
    'Inicio del período que cubre este pago';
COMMENT ON COLUMN pagos.periodo_hasta IS
    'Fin del período que cubre este pago';
COMMENT ON COLUMN pagos.registrado_por_id IS
    'Super Admin que registró el pago manualmente';
COMMENT ON COLUMN pagos.fecha_creacion IS
    'Timestamp de cuándo se registró el pago (inmutable)';

-- Índices
CREATE INDEX idx_pagos_empresa_id      ON pagos(empresa_id);
CREATE INDEX idx_pagos_suscripcion_id  ON pagos(suscripcion_id);
CREATE INDEX idx_pagos_fecha           ON pagos(fecha_creacion DESC);
-- Para el dashboard financiero (HU-011 CA-09):
CREATE INDEX idx_pagos_estado          ON pagos(estado);

-- NO tiene RLS — el Super Admin gestiona todos los pagos
-- NO tiene trigger de fecha_modificacion — los pagos son inmutables
-- NO tiene campo activo — los pagos nunca se desactivan
