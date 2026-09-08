-- ============================================================
-- TABLA: lineas_orden
-- Descripción: Ítems de detalle de mercancía por orden.
--              ON DELETE CASCADE — se eliminan con la orden.
-- HU: HU-021
-- ============================================================

CREATE TABLE IF NOT EXISTS lineas_orden (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id       UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id         UUID NOT NULL REFERENCES ordenes(id) ON DELETE CASCADE,
    descripcion      VARCHAR(255) NOT NULL,
    cantidad         NUMERIC(12,3) NOT NULL,
    unidad_medida_id UUID REFERENCES unidades_medida(id),
    peso_kg          NUMERIC(12,3),
    volumen_m3       NUMERIC(12,3),
    valor_unitario   NUMERIC(15,2),
    numero_linea     SMALLINT NOT NULL DEFAULT 1,
    activo           BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion   TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE lineas_orden IS
    'Líneas de detalle de mercancía por orden de transporte.
     ON DELETE CASCADE permite limpiar las líneas si la orden se elimina
     físicamente (no ocurre en negocio — solo en tests de integración).';

COMMENT ON COLUMN lineas_orden.numero_linea IS
    'Número secuencial de la línea dentro de la orden (1, 2, 3...).
     Controla el orden de presentación en la UI.';

CREATE INDEX idx_lineas_orden_empresa  ON lineas_orden(empresa_id);
CREATE INDEX idx_lineas_orden_orden_id ON lineas_orden(orden_id);

ALTER TABLE lineas_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_lineas_orden" ON lineas_orden
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_lineas_orden_fecha_modificacion
    BEFORE UPDATE ON lineas_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();