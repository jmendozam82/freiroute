-- ============================================================
-- ALTER TABLE ordenes: fecha_entrega_real (HU-031 — SLA Management)
-- Fecha/hora real de entrega de la orden. Se puebla al mover la
-- orden a DELIVERED vía FSM (ADR-019). Alimenta el cálculo de
-- cumplimiento SLA (HU-031 CA-03) comparando contra
-- fecha_entrega_requerida.
-- ============================================================

ALTER TABLE ordenes
    ADD COLUMN IF NOT EXISTS fecha_entrega_real TIMESTAMPTZ;

COMMENT ON COLUMN ordenes.fecha_entrega_real IS
    'Fecha y hora real de entrega. Poblada al mover la orden a DELIVERED.
     Usada para calcular cumplimiento SLA vs fecha_entrega_requerida.';
