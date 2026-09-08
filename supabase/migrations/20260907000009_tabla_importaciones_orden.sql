-- ============================================================
-- TABLA: importaciones_orden
-- Descripción: Historial de importaciones CSV masivas de órdenes.
--              Patrón fail-soft (ADR-017): las filas válidas se
--              crean, las inválidas quedan en detalle_errores.
-- HU: HU-022
-- ============================================================

CREATE TABLE IF NOT EXISTS importaciones_orden (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    usuario_id      UUID NOT NULL REFERENCES usuarios(id),
    nombre_archivo  VARCHAR(255) NOT NULL,
    total_filas     INT NOT NULL DEFAULT 0,
    filas_ok        INT NOT NULL DEFAULT 0,
    filas_error     INT NOT NULL DEFAULT 0,
    detalle_errores JSONB,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE importaciones_orden IS
    'Historial de importaciones CSV masivas de órdenes (HU-022).
     Patrón fail-soft (ADR-017): las filas válidas se crean y las
     inválidas se registran en detalle_errores sin abortar el proceso.';

COMMENT ON COLUMN importaciones_orden.detalle_errores IS
    'Array JSON con el detalle de errores por fila:
     [{fila: N, campo: "nombre_campo", error: "descripcion del error"}]
     NULL si todas las filas fueron válidas.';

CREATE INDEX idx_importaciones_empresa ON importaciones_orden(empresa_id);
CREATE INDEX idx_importaciones_usuario ON importaciones_orden(usuario_id);
CREATE INDEX idx_importaciones_fecha
    ON importaciones_orden(empresa_id, fecha_creacion DESC);

ALTER TABLE importaciones_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_importaciones_orden" ON importaciones_orden
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_importaciones_orden_fecha_modificacion
    BEFORE UPDATE ON importaciones_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();