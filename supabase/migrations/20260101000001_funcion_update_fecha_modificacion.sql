-- ============================================================
-- MIGRACIÓN 01 - FUNCIÓN update_fecha_modificacion()
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Función global reutilizable que actualiza fecha_modificacion
-- automáticamente en cada UPDATE. Debe existir ANTES de cualquier
-- trigger que la use (orden de ejecución por timestamp).
-- ============================================================

CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_fecha_modificacion() IS
'Actualiza fecha_modificacion automáticamente en cada UPDATE. Usada por todos los triggers del sistema.';