-- ============================================================
-- ALTER TABLE ordenes: campos PO/SO (HU-028 — PO Integration)
-- Agrega numero_po (Purchase Order) y numero_so (Sales Order)
-- como referencias opcionales de trazabilidad ERP/WMS.
-- HU-028 CA-01 / CA-02: VARCHAR(100) opcionales.
-- Relación 1:N: una PO puede vincularse a múltiples órdenes.
-- ============================================================

ALTER TABLE ordenes
    ADD COLUMN IF NOT EXISTS numero_po VARCHAR(100),
    ADD COLUMN IF NOT EXISTS numero_so VARCHAR(100);

COMMENT ON COLUMN ordenes.numero_po IS
    'Número de Purchase Order del cliente vinculada a esta orden de transporte. Opcional.';
COMMENT ON COLUMN ordenes.numero_so IS
    'Número de Sales Order del cliente vinculada a esta orden de transporte. Opcional.';

-- Búsqueda exacta por PO/SO dentro del tenant (índices parciales excluyen NULLs)
CREATE INDEX IF NOT EXISTS idx_ordenes_numero_po
    ON ordenes(empresa_id, numero_po)
    WHERE numero_po IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_ordenes_numero_so
    ON ordenes(empresa_id, numero_so)
    WHERE numero_so IS NOT NULL;
