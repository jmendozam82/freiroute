-- ============================================================
-- MIGRACIÓN 01 - TABLA ubicaciones
-- Freiroute TMS - Sprint 3 EP-03 (HU-015, ADR-003, ADR-014)
-- ============================================================
-- Ubicaciones georreferenciadas del tenant: almacenes, clientes,
-- puertos, aeropuertos y puntos de entrega/recogida. Base para la
-- planificación de rutas y el cálculo de distancias.
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id en código.
-- ADR-014: latitud/longitud son derivadas (geocodificación Nominatim).
-- Soft delete universal (ADR-005): activo = false, nunca DELETE físico.
-- ============================================================

CREATE TABLE IF NOT EXISTS ubicaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    -- Identificación
    nombre              VARCHAR(200) NOT NULL,
    codigo              VARCHAR(50),
    tipo                VARCHAR(50) NOT NULL DEFAULT 'OTRO',
    -- Dirección
    direccion           TEXT,
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    departamento        VARCHAR(100),
    ciudad              VARCHAR(100),
    codigo_postal       VARCHAR(20),
    -- Geocodificación (ADR-014)
    latitud             DOUBLE PRECISION,
    longitud            DOUBLE PRECISION,
    georeferenciada     BOOLEAN NOT NULL DEFAULT false,
    direccion_normalizada TEXT,
    -- Datos operativos
    contacto_nombre     VARCHAR(200),
    contacto_telefono   VARCHAR(50),
    contacto_email      VARCHAR(200),
    horario_apertura    TIME,
    horario_cierre      TIME,
    tiempo_servicio_min INTEGER NOT NULL DEFAULT 30,
    instrucciones       TEXT,
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_ubicacion_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE ubicaciones IS
    'Ubicaciones georreferenciadas del tenant: almacenes, clientes,
     puertos, aeropuertos y puntos de cruce. Base para la planificación
     de rutas y el cálculo de distancias.';
COMMENT ON COLUMN ubicaciones.tipo IS
    'ALMACEN, CLIENTE, PUERTO, AEROPUERTO, TERMINAL, CRUCE_FRONTERA,
     PUNTO_RECARGA, OTRO';
COMMENT ON COLUMN ubicaciones.georeferenciada IS
    'true cuando latitud/longitud han sido validadas vía geocodificación';
COMMENT ON COLUMN ubicaciones.tiempo_servicio_min IS
    'Tiempo estimado de carga/descarga en minutos. Usado en optimización
     de rutas para calcular ETA con paradas.';

CREATE INDEX idx_ubicaciones_empresa_id     ON ubicaciones(empresa_id);
CREATE INDEX idx_ubicaciones_activo         ON ubicaciones(activo);
CREATE INDEX idx_ubicaciones_empresa_activo ON ubicaciones(empresa_id, activo);
CREATE INDEX idx_ubicaciones_tipo           ON ubicaciones(tipo);
CREATE INDEX idx_ubicaciones_geo ON ubicaciones(latitud, longitud)
    WHERE georeferenciada = true;

ALTER TABLE ubicaciones ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ubicaciones" ON ubicaciones
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_ubicaciones_fecha_modificacion
    BEFORE UPDATE ON ubicaciones
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();