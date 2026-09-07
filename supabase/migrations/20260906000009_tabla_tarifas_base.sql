-- ============================================================
-- MIGRACIÓN 09 - TABLA tarifas_base
-- Freiroute TMS - Sprint 3 EP-03 (HU-020, ADR-003, ADR-015)
-- ============================================================
-- Tarifas de flete por zona, modo y tipo de servicio del tenant.
-- Historial completo preservado — no se eliminan registros vencidos
-- (ADR-015).
--
-- VERSIONADO DE TARIFAS (ADR-015):
--   El UPDATE de esta tabla SOLO se usa para CerrarVigenciaAsync
--   (fijar fecha_vigencia_hasta = ayer al crear una nueva versión).
--   NO se usa para modificar datos de negocio: una tarifa modificada
--   se versiona cerrando la vigencia de la anterior y creando un
--   registro nuevo como sucesor vigente.
--
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false (conserva historial).
-- ============================================================

CREATE TABLE IF NOT EXISTS tarifas_base (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(200) NOT NULL,
    codigo                  VARCHAR(50),
    -- Aplicación de la tarifa
    zona_origen_id          UUID REFERENCES zonas_entrega(id) ON DELETE RESTRICT,
    zona_destino_id         UUID REFERENCES zonas_entrega(id) ON DELETE RESTRICT,
    modo_transporte         VARCHAR(50) NOT NULL DEFAULT 'FTL',
    tipo_servicio           VARCHAR(50) NOT NULL DEFAULT 'ESTANDAR',
    -- Modelo de precio (ADR-015)
    tipo_tarifa             VARCHAR(30) NOT NULL DEFAULT 'FIJO_VIAJE',
    precio_unitario         NUMERIC(18,4) NOT NULL,
    precio_minimo           NUMERIC(18,4),
    moneda                  VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Vigencia
    fecha_vigencia_desde    DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_vigencia_hasta    DATE,
    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    CONSTRAINT uq_tarifa_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE tarifas_base IS
    'Tarifas de flete por zona, modo y tipo de servicio del tenant.
     Historial completo preservado — no se eliminan registros vencidos.
     El UPDATE solo se usa para cerrar vigencia (CerrarVigenciaAsync,
     ADR-015): las tarifas se versionan creando registros nuevos.';
COMMENT ON COLUMN tarifas_base.tipo_tarifa IS
    'POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD (ADR-015)';
COMMENT ON COLUMN tarifas_base.modo_transporte IS
    'FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL';
COMMENT ON COLUMN tarifas_base.tipo_servicio IS
    'ESTANDAR, EXPRESS, PROGRAMADO, REFRIGERADO, PELIGROSO';
COMMENT ON COLUMN tarifas_base.precio_minimo IS
    'Precio mínimo aplicable aunque el cálculo resulte menor.
     NULL si no hay mínimo.';

CREATE INDEX idx_tarifas_empresa_id     ON tarifas_base(empresa_id);
CREATE INDEX idx_tarifas_activo         ON tarifas_base(activo);
CREATE INDEX idx_tarifas_empresa_activo ON tarifas_base(empresa_id, activo);
CREATE INDEX idx_tarifas_zona_origen    ON tarifas_base(zona_origen_id);
CREATE INDEX idx_tarifas_zona_destino   ON tarifas_base(zona_destino_id);
CREATE INDEX idx_tarifas_vigencia       ON tarifas_base(fecha_vigencia_desde,
    fecha_vigencia_hasta) WHERE activo = true;

-- Índice especial de vigencia: optimiza la búsqueda de la tarifa
-- vigente por empresa + zona origen/destino + modo + rango de fechas
-- (GetVigenteAsync — CRÍTICO para el cálculo de costos del Sprint 4).
CREATE INDEX idx_tarifas_vigencia_activa ON tarifas_base(
    empresa_id, zona_origen_id, zona_destino_id, modo_transporte,
    fecha_vigencia_desde, fecha_vigencia_hasta
) WHERE activo = true;

ALTER TABLE tarifas_base ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_tarifas" ON tarifas_base
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_tarifas_fecha_modificacion
    BEFORE UPDATE ON tarifas_base
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();