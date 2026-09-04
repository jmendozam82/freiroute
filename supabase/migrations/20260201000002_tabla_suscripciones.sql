-- ============================================================
-- MIGRACIÓN 02 - TABLA suscripciones
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Suscripciones activas de cada empresa a un plan.
-- EXCEPCIONES al patrón estándar:
--   - SIN RLS (el Super Admin ve todas las suscripciones)
--   - SÍ tiene empresa_id como FK (no como discriminador de tenant)
--   - Constraint UNIQUE DEFERRABLE: una empresa tiene UNA suscripción activa
-- ============================================================

CREATE TABLE IF NOT EXISTS suscripciones (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Relaciones
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    plan_id                 UUID NOT NULL REFERENCES planes(id) ON DELETE RESTRICT,
    -- Ciclo de facturación
    tipo_ciclo              VARCHAR(20) NOT NULL DEFAULT 'MENSUAL',
    fecha_inicio            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_vencimiento       TIMESTAMPTZ NOT NULL,
    fecha_cancelacion       TIMESTAMPTZ,
    -- Estado
    estado                  VARCHAR(50) NOT NULL DEFAULT 'TRIAL',
    -- Precio pactado al contratar (puede diferir del plan actual)
    precio_pactado          NUMERIC(10,2) NOT NULL DEFAULT 0,
    moneda_pactada          VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Control (soft delete universal)
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    -- Auditoría
    creado_por_id           UUID REFERENCES usuarios(id),
    -- Una empresa tiene exactamente una suscripción activa
    CONSTRAINT uq_suscripcion_empresa_activa
        UNIQUE (empresa_id, activo) DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE suscripciones IS
    'Suscripciones de cada empresa a un plan. Una empresa tiene exactamente una suscripción activa a la vez.';
COMMENT ON COLUMN suscripciones.id IS
    'Identificador único de la suscripción (UUID generado por la BD)';
COMMENT ON COLUMN suscripciones.empresa_id IS
    'FK a la empresa (tenant) suscrita';
COMMENT ON COLUMN suscripciones.plan_id IS
    'FK al plan contratado. Puede cambiar si el tenant cambia de plan.';
COMMENT ON COLUMN suscripciones.tipo_ciclo IS
    'Ciclo de facturación: MENSUAL o ANUAL';
COMMENT ON COLUMN suscripciones.fecha_inicio IS
    'Fecha de inicio de la suscripción';
COMMENT ON COLUMN suscripciones.fecha_vencimiento IS
    'Fecha de vencimiento de la suscripción. El job de vencimientos verifica esta fecha.';
COMMENT ON COLUMN suscripciones.fecha_cancelacion IS
    'Fecha de cancelación. NULL si la suscripción sigue activa.';
COMMENT ON COLUMN suscripciones.estado IS
    'Estado de la suscripción: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED';
COMMENT ON COLUMN suscripciones.precio_pactado IS
    'Precio negociado al contratar — puede diferir del precio actual del plan';
COMMENT ON COLUMN suscripciones.moneda_pactada IS
    'Moneda del precio pactado: USD, EUR, etc.';
COMMENT ON COLUMN suscripciones.activo IS
    'Soft delete: false = suscripción inactiva (desactivada o cancelada)';
COMMENT ON COLUMN suscripciones.fecha_creacion IS
    'Timestamp de creación del registro';
COMMENT ON COLUMN suscripciones.fecha_modificacion IS
    'Timestamp de la última modificación (lo actualiza el trigger)';
COMMENT ON COLUMN suscripciones.creado_por_id IS
    'Super Admin que creó o modificó la suscripción';

-- Índices
CREATE INDEX idx_suscripciones_empresa_id        ON suscripciones(empresa_id);
CREATE INDEX idx_suscripciones_plan_id           ON suscripciones(plan_id);
CREATE INDEX idx_suscripciones_estado            ON suscripciones(estado);
CREATE INDEX idx_suscripciones_vencimiento       ON suscripciones(fecha_vencimiento);
CREATE INDEX idx_suscripciones_activo            ON suscripciones(activo);
-- Para el background job de vencimientos (ADR-013):
-- Busca suscripciones ACTIVE cuyo vencimiento está próximo
CREATE INDEX idx_suscripciones_vencimiento_estado
    ON suscripciones(fecha_vencimiento, estado)
    WHERE activo = true;

-- NO tiene RLS — el Super Admin gestiona todas las suscripciones
-- (ADR-004: la tabla es operada exclusivamente desde el panel de admin)

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_suscripciones_fecha_modificacion
    BEFORE UPDATE ON suscripciones
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
