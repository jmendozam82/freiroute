-- ============================================================
-- MIGRACIÓN 07 - TABLA clientes (shippers)
-- Freiroute TMS - Sprint 3 EP-03 (HU-019, ADR-003)
-- ============================================================
-- Shippers / clientes del tenant que contratan servicios de
-- transporte. Contiene la configuración comercial, de crédito y
-- SLA por cliente.
-- La unicidad de RUC/NIT se valida en la capa de aplicación
-- (ClienteRepository.ExisteRucAsync, HU-019 CA-08) — no hay
-- constraint UNIQUE a nivel BD porque el RUC es opcional y la
-- regla es por empresa.
-- ADR-003: RLS con empresa_isolation + filtro por empresa_id.
-- Soft delete universal (ADR-005): activo = false.
-- ============================================================

CREATE TABLE IF NOT EXISTS clientes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    -- Identificación
    nombre              VARCHAR(200) NOT NULL,
    nombre_comercial    VARCHAR(200),
    ruc_nit             VARCHAR(50),
    tipo_documento      VARCHAR(20) NOT NULL DEFAULT 'RUC',
    tipo_cliente        VARCHAR(50) NOT NULL DEFAULT 'REGULAR',
    industria           VARCHAR(100),
    -- Contacto principal
    email               VARCHAR(200),
    telefono            VARCHAR(50),
    sitio_web           VARCHAR(300),
    -- Dirección fiscal
    direccion_fiscal    TEXT,
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    departamento        VARCHAR(100),
    ciudad              VARCHAR(100),
    -- Ubicación de despacho por defecto
    ubicacion_defecto_id UUID REFERENCES ubicaciones(id) ON DELETE SET NULL,
    -- Configuración comercial
    credito_dias        INTEGER NOT NULL DEFAULT 0,
    limite_credito      NUMERIC(18,2) NOT NULL DEFAULT 0,
    moneda              VARCHAR(10) NOT NULL DEFAULT 'USD',
    estado_credito      VARCHAR(50) NOT NULL DEFAULT 'AL_DIA',
    -- SLA
    sla_dias_entrega    INTEGER,
    sla_ventana_inicio  TIME,
    sla_ventana_fin     TIME,
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE clientes IS
    'Shippers / clientes del tenant que contratan servicios de transporte.
     Contiene la configuración comercial, de crédito y SLA por cliente.';
COMMENT ON COLUMN clientes.tipo_cliente IS
    'REGULAR, VIP, OCASIONAL, CORPORATIVO, GOBIERNO';
COMMENT ON COLUMN clientes.estado_credito IS
    'AL_DIA, EN_MORA, BLOQUEADO, SIN_CREDITO';
COMMENT ON COLUMN clientes.credito_dias IS
    'Días de crédito para pago de facturas. 0 = pago al contado.';
COMMENT ON COLUMN clientes.sla_dias_entrega IS
    'Días máximos de entrega pactados. NULL si no hay SLA formal.';

CREATE INDEX idx_clientes_empresa_id     ON clientes(empresa_id);
CREATE INDEX idx_clientes_activo         ON clientes(activo);
CREATE INDEX idx_clientes_empresa_activo ON clientes(empresa_id, activo);
CREATE INDEX idx_clientes_tipo           ON clientes(tipo_cliente);
CREATE INDEX idx_clientes_estado_credito ON clientes(estado_credito);
CREATE INDEX idx_clientes_ruc            ON clientes(ruc_nit)
    WHERE ruc_nit IS NOT NULL;

ALTER TABLE clientes ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_clientes" ON clientes
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_clientes_fecha_modificacion
    BEFORE UPDATE ON clientes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();