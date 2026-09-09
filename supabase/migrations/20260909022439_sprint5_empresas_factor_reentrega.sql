-- ============================================================
-- ALTER TABLE empresas: factor_reentrega (HU-030 — Rechazos/re-entregas)
-- Factor multiplicador sobre la tarifa original para calcular el
-- cargo de re-entrega. Configurable por tenant.
-- HU-030 CA-08: cargo = tarifa.precio_unitario × factor_reentrega.
-- Default: 1.5 (50% adicional sobre la tarifa original).
-- Decisión Fase 1: la configuración operativa del tenant se persiste
-- en la tabla 'empresas' (la tabla física 'configuracion' no existe).
-- ============================================================

ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS factor_reentrega NUMERIC(4,2) NOT NULL DEFAULT 1.5;

COMMENT ON COLUMN empresas.factor_reentrega IS
    'Factor multiplicador sobre la tarifa original para calcular el cargo de re-entrega.
     Default: 1.5 (50% adicional). Configurable por tenant.';
