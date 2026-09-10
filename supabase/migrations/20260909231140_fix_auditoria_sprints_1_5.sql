-- ====================================================================
-- MIGRACIÓN: Fix Auditoría y RLS (Sprints 1-5)
-- Objetivo: Subsanar deuda técnica crítica arquitectónica detectada.
-- ====================================================================

-- ====================================================================
-- 1. RECHAZOS ENTREGA (HU-030)
-- Faltaba RLS, fecha_modificacion y su trigger.
-- ====================================================================
ALTER TABLE rechazos_entrega ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_rechazos_entrega" ON rechazos_entrega
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

ALTER TABLE rechazos_entrega 
ADD COLUMN fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW();

CREATE TRIGGER trg_rechazos_entrega_fecha_modificacion
BEFORE UPDATE ON rechazos_entrega
FOR EACH ROW
EXECUTE FUNCTION update_fecha_modificacion();

-- ====================================================================
-- 2. CONTACTOS CLIENTE
-- Faltaba la columna fecha_modificacion (pero el trigger estaba o faltaba).
-- ====================================================================
ALTER TABLE contactos_cliente 
ADD COLUMN fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW();

-- Nota: Si el trigger ya existe, no es necesario crearlo, pero usaremos CREATE OR REPLACE 
-- por si acaso, o DROP TRIGGER IF EXISTS.
DROP TRIGGER IF EXISTS trg_contactos_cliente_fecha_modificacion ON contactos_cliente;
CREATE TRIGGER trg_contactos_cliente_fecha_modificacion
BEFORE UPDATE ON contactos_cliente
FOR EACH ROW
EXECUTE FUNCTION update_fecha_modificacion();

-- ====================================================================
-- 3. HISTORIAL ESTADOS RECLAMO
-- Faltaba RLS.
-- ====================================================================
ALTER TABLE historial_estados_reclamo ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_historial_estados_reclamo" ON historial_estados_reclamo
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- ====================================================================
-- 4. INVITACIONES, PAGOS, CODIGOS 2FA
-- Aplicando ADR-005: Soft delete universal.
-- ====================================================================

-- 4.1 Invitaciones
ALTER TABLE invitaciones 
ADD COLUMN activo BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW();

CREATE TRIGGER trg_invitaciones_fecha_modificacion
BEFORE UPDATE ON invitaciones
FOR EACH ROW
EXECUTE FUNCTION update_fecha_modificacion();

-- 4.2 Pagos
ALTER TABLE pagos 
ADD COLUMN activo BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW();

CREATE TRIGGER trg_pagos_fecha_modificacion
BEFORE UPDATE ON pagos
FOR EACH ROW
EXECUTE FUNCTION update_fecha_modificacion();

-- 4.3 Códigos 2FA Temporales
ALTER TABLE codigos_2fa_temporales 
ADD COLUMN activo BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW();

CREATE TRIGGER trg_codigos_2fa_temporales_fecha_modificacion
BEFORE UPDATE ON codigos_2fa_temporales
FOR EACH ROW
EXECUTE FUNCTION update_fecha_modificacion();
