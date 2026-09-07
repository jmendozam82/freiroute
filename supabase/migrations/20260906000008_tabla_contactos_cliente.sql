-- ============================================================
-- MIGRACIÓN 08 - TABLA contactos_cliente
-- Freiroute TMS - Sprint 3 EP-03 (HU-019, ADR-003)
-- ============================================================
-- Múltiples contactos por cliente con rol diferenciado. El contacto
-- principal (es_principal = true) recibe las notificaciones del sistema.
--
-- EXCEPCIONES arquitectónicas documentadas:
--   - SIN fecha_modificacion: los contactos son datos simples
--     (solo se crean, actualizan o desactivan).
--   - SIN trigger update_fecha_modificacion: no existe la columna
--     que el trigger actualizaría (update_fecha_modificacion()
--     referencia NEW.fecha_modificacion).
--   - SÍ tiene activo (soft delete, ADR-005).
--   - SÍ tiene RLS propio (ADR-003).
-- ============================================================

CREATE TABLE IF NOT EXISTS contactos_cliente (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    cliente_id      UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    nombre          VARCHAR(200) NOT NULL,
    cargo           VARCHAR(100),
    rol             VARCHAR(50) NOT NULL DEFAULT 'GENERAL',
    email           VARCHAR(200),
    telefono        VARCHAR(50),
    es_principal    BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE contactos_cliente IS
    'Múltiples contactos por cliente con rol diferenciado.
     El contacto principal recibe las notificaciones del sistema.';
COMMENT ON COLUMN contactos_cliente.rol IS
    'GENERAL, LOGISTICA, COMPRAS, FINANZAS, RECEPCION, GERENCIA';

CREATE INDEX idx_contactos_cliente_id ON contactos_cliente(cliente_id);
CREATE INDEX idx_contactos_empresa_id ON contactos_cliente(empresa_id);

ALTER TABLE contactos_cliente ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_contactos" ON contactos_cliente
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);