-- ============================================================
-- MIGRACIÓN 02 - TABLA zonas_entrega
-- Freiroute TMS - Sprint 3 EP-03 (HU-016, ADR-003, ADR-018)
-- ============================================================
-- Zonas geográficas de cobertura del tenant. Usadas en tarifas
-- (HU-020), asignación de carriers y planificación de rutas.
-- Definición geográfica multi-método: polígono GeoJSON, códigos
-- postales, ciudades, departamentos o países (ADR-018).
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS zonas_entrega (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(200) NOT NULL,
    codigo              VARCHAR(50) NOT NULL,
    descripcion         TEXT,
    color_hex           VARCHAR(7) NOT NULL DEFAULT '#1A73E8',
    -- Definición geográfica
    tipo_definicion     VARCHAR(20) NOT NULL DEFAULT 'POLIGONO',
    poligono_geojson    TEXT,
    codigos_postales    TEXT[],
    ciudades            TEXT[],
    departamentos       TEXT[],
    paises              TEXT[] NOT NULL DEFAULT '{Nicaragua}',
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_zona_codigo_empresa UNIQUE (empresa_id, codigo)
);

COMMENT ON TABLE zonas_entrega IS
    'Zonas geográficas de cobertura del tenant. Usadas en tarifas,
     asignación de carriers y planificación de rutas.';
COMMENT ON COLUMN zonas_entrega.tipo_definicion IS
    'POLIGONO (GeoJSON), CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES';
COMMENT ON COLUMN zonas_entrega.color_hex IS
    'Color en formato #RRGGBB para visualización en el mapa';
COMMENT ON COLUMN zonas_entrega.poligono_geojson IS
    'GeoJSON Polygon o MultiPolygon que define el área de la zona.
     Usado para verificar si una ubicación cae dentro de la zona.';

CREATE INDEX idx_zonas_empresa_id     ON zonas_entrega(empresa_id);
CREATE INDEX idx_zonas_activo         ON zonas_entrega(activo);
CREATE INDEX idx_zonas_empresa_activo ON zonas_entrega(empresa_id, activo);

ALTER TABLE zonas_entrega ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_zonas" ON zonas_entrega
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_zonas_fecha_modificacion
    BEFORE UPDATE ON zonas_entrega
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();