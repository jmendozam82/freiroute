-- ============================================================
-- TABLA: reglas_prioridad (HU-029 — Priorización dinámica)
-- Reglas configurables por tenant para la elevación automática de
-- prioridad de órdenes (job PrioridadOrdenesJob, patrón ADR-013).
-- Condiciones: ENTREGA_PROXIMA | CLIENTE_VIP | SIN_AVANCE.
-- La semilla NO se inserta aquí — se crea en el onboarding (Sprint 2)
-- o en el servicio de ReglaPrioridadService usando las constantes
-- del Utility.
-- ============================================================

CREATE TABLE IF NOT EXISTS reglas_prioridad (
    id                 UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id         UUID         NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre             VARCHAR(100) NOT NULL,
    condicion          VARCHAR(50)  NOT NULL,
    horas_umbral       INTEGER      NOT NULL DEFAULT 24,
    nivel_destino      VARCHAR(20)  NOT NULL DEFAULT 'ALTO',
    activo             BOOLEAN      NOT NULL DEFAULT true,
    fecha_creacion     TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ  NOT NULL DEFAULT now()
);

COMMENT ON TABLE reglas_prioridad IS
    'Reglas configurables por tenant para elevación automática de prioridad de órdenes.
     Condiciones: ENTREGA_PROXIMA | CLIENTE_VIP | SIN_AVANCE.';

COMMENT ON COLUMN reglas_prioridad.condicion IS
    'Condición de la regla: ENTREGA_PROXIMA (entrega dentro del umbral) |
     CLIENTE_VIP (cliente VIP sin avance) | SIN_AVANCE (sin transición de estado).';

COMMENT ON COLUMN reglas_prioridad.horas_umbral IS
    'Umbral en horas de la condición (default 24).';

COMMENT ON COLUMN reglas_prioridad.nivel_destino IS
    'Prioridad destino de la elevación: CRITICO | ALTO (default ALTO).';

ALTER TABLE reglas_prioridad ENABLE ROW LEVEL SECURITY;

-- Aislamiento multi-tenant (ADR-003)
CREATE POLICY reglas_prioridad_tenant_isolation ON reglas_prioridad
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

-- Una sola regla activa por nombre dentro del tenant (índice parcial)
CREATE UNIQUE INDEX IF NOT EXISTS idx_reglas_prioridad_nombre_activo
    ON reglas_prioridad(empresa_id, nombre)
    WHERE activo = true;

-- Trigger estándar update_fecha_modificacion
CREATE TRIGGER trg_reglas_prioridad_fecha_modificacion
    BEFORE UPDATE ON reglas_prioridad
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
