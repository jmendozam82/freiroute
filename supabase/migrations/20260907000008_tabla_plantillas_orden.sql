-- ============================================================
-- TABLA: plantillas_orden
-- Descripción: Plantillas para creación rápida y órdenes
--              recurrentes programadas (HU-027).
--              El campo datos_orden es un snapshot JSON del
--              OrdenRequestDto al momento de guardar.
-- ============================================================

CREATE TABLE IF NOT EXISTS plantillas_orden (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(100) NOT NULL,
    descripcion             TEXT,
    datos_orden             JSONB NOT NULL DEFAULT '{}',
    es_recurrente           BOOLEAN NOT NULL DEFAULT false,
    frecuencia_recurrencia  VARCHAR(20),
    proxima_ejecucion       DATE,
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion      TIMESTAMPTZ NOT NULL DEFAULT now(),
    creado_por              UUID REFERENCES usuarios(id)
);

COMMENT ON TABLE plantillas_orden IS
    'Plantillas de órdenes para creación rápida y recurrencia programada.
     El background job RecurrenciaOrdenesJob las procesa cada día a las 00:05
     usando el patrón PeriodicTimer (ADR-013).';

COMMENT ON COLUMN plantillas_orden.datos_orden IS
    'Snapshot JSON de los campos de OrdenRequestDto al momento de guardar.
     Se usa para precargar el formulario de nueva orden o para crear
     órdenes automáticas en la recurrencia.';

COMMENT ON COLUMN plantillas_orden.frecuencia_recurrencia IS
    'DIARIA | SEMANAL | QUINCENAL | MENSUAL. NULL si es_recurrente = false.';

COMMENT ON COLUMN plantillas_orden.proxima_ejecucion IS
    'Fecha de la próxima ejecución automática. El job la actualiza tras
     crear cada orden recurrente usando FrecuenciaRecurrencia.CalcularProximaEjecucion().';

CREATE INDEX idx_plantillas_empresa        ON plantillas_orden(empresa_id);
CREATE INDEX idx_plantillas_empresa_activo ON plantillas_orden(empresa_id, activo);
-- Índice parcial crítico para el background job (consulta cross-tenant)
CREATE INDEX idx_plantillas_recurrentes
    ON plantillas_orden(proxima_ejecucion)
    WHERE es_recurrente = true AND activo = true;

ALTER TABLE plantillas_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_plantillas_orden" ON plantillas_orden
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_plantillas_orden_fecha_modificacion
    BEFORE UPDATE ON plantillas_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();