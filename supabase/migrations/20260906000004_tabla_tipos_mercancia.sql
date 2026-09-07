-- ============================================================
-- MIGRACIÓN 04 - TABLA tipos_mercancia
-- Freiroute TMS - Sprint 3 EP-03 (HU-017, ADR-003)
-- ============================================================
-- Catálogo de tipos de mercancía del tenant. Define cómo se maneja
-- y clasifica la carga para la planificación de embarques.
-- Incluye clasificación HAZMAT ONU (clase 1-9 + código ONU) y
-- flags de manejo (refrigeración, fragilidad, peligrosidad, etc.).
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS tipos_mercancia (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(200) NOT NULL,
    codigo                  VARCHAR(50),
    descripcion             TEXT,
    -- Clasificación
    categoria               VARCHAR(100),
    clase_peligrosidad      VARCHAR(10),
    codigo_onu              VARCHAR(10),
    codigo_hs               VARCHAR(20),
    -- Características físicas
    peso_maximo_kg          NUMERIC(10,3),
    volumen_maximo_m3       NUMERIC(10,3),
    temperatura_minima_c    NUMERIC(5,2),
    temperatura_maxima_c    NUMERIC(5,2),
    -- Flags de manejo
    requiere_refrigeracion  BOOLEAN NOT NULL DEFAULT false,
    es_fragil               BOOLEAN NOT NULL DEFAULT false,
    es_peligroso            BOOLEAN NOT NULL DEFAULT false,
    es_perecedero           BOOLEAN NOT NULL DEFAULT false,
    es_sobredimensionado    BOOLEAN NOT NULL DEFAULT false,
    requiere_fumigacion     BOOLEAN NOT NULL DEFAULT false,
    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    CONSTRAINT uq_mercancia_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE tipos_mercancia IS
    'Catálogo de tipos de mercancía del tenant. Define cómo se maneja
     y clasifica la carga para la planificación de embarques.';
COMMENT ON COLUMN tipos_mercancia.clase_peligrosidad IS
    'Clase HAZMAT según normativa ONU: 1 (Explosivos), 2 (Gases),
     3 (Líquidos inflamables), 4 (Sólidos inflamables),
     5 (Oxidantes), 6 (Tóxicos), 7 (Radioactivos),
     8 (Corrosivos), 9 (Varios). NULL si no es peligroso.';
COMMENT ON COLUMN tipos_mercancia.codigo_hs IS
    'Código del Sistema Armonizado (HS Code) para aduanas. Ej: 0401.10';

CREATE INDEX idx_mercancia_empresa_id     ON tipos_mercancia(empresa_id);
CREATE INDEX idx_mercancia_activo         ON tipos_mercancia(activo);
CREATE INDEX idx_mercancia_empresa_activo ON tipos_mercancia(empresa_id, activo);
CREATE INDEX idx_mercancia_peligroso      ON tipos_mercancia(es_peligroso)
    WHERE es_peligroso = true;

ALTER TABLE tipos_mercancia ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_mercancia" ON tipos_mercancia
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_mercancia_fecha_modificacion
    BEFORE UPDATE ON tipos_mercancia
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();