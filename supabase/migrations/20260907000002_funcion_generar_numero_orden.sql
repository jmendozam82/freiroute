-- ============================================================
-- FUNCIÓN: generar_numero_orden
-- Descripción: Genera el número de orden legible para el tenant.
--              Formato: {PREFIJO}-{YYYY}-{NNNNN}
--              Ejemplo: ORD-2026-00001
-- ADR: ADR-020
-- Llamar SOLO en la transición DRAFT→CONFIRMED.
-- ============================================================

CREATE OR REPLACE FUNCTION generar_numero_orden(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql AS $$
DECLARE
    v_consecutivo INT;
    v_numero      TEXT;
BEGIN
    -- Upsert atómico: incrementa el contador o lo inicializa en 1
    INSERT INTO contadores_orden (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = contadores_orden.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    -- Formato: PREFIJO-YYYY-NNNNN (NNNNN = 5 dígitos con ceros a la izquierda)
    v_numero := UPPER(TRIM(p_prefijo))
             || '-' || p_anio::TEXT
             || '-' || LPAD(v_consecutivo::TEXT, 5, '0');

    RETURN v_numero;
END;
$$;

COMMENT ON FUNCTION generar_numero_orden IS
    'Genera el número de orden legible para el tenant en el año indicado.
     Garantiza unicidad por tenant-año mediante upsert atómico.
     Se llama exclusivamente en la transición DRAFT→CONFIRMED (ADR-020).
     El índice UNIQUE en ordenes(empresa_id, numero_orden) actúa como
     red de seguridad contra race conditions extremas.';