-- ============================================================
-- Numeración de reclamos (HU-032 + ADR-020)
-- Decisión: OPCIÓN B — tabla separada 'contadores_reclamo'.
-- Motivo: 'contadores_orden' (ADR-020) ya está en producción con
-- contadores de órdenes vivos y su PK (empresa_id, anio) alimenta el
-- upsert atómico de generar_numero_orden(). Alterar su PK para
-- incluir un discriminator 'tipo' sería arriesgado con datos
-- existentes y rompería la lógica de upsert actual.
-- Opción B: tabla espejo sin RLS (protegida por FK a empresas y por
-- la función) — mismo patrón de 'contadores_orden'.
-- ============================================================

CREATE TABLE IF NOT EXISTS contadores_reclamo (
    empresa_id         UUID     NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    anio               SMALLINT NOT NULL,
    ultimo_consecutivo INT      NOT NULL DEFAULT 0,
    PRIMARY KEY (empresa_id, anio)
);

COMMENT ON TABLE contadores_reclamo IS
    'Contador de consecutivo de reclamos por tenant y año.
     Mismo patrón que contadores_orden (ADR-020): la función
     generar_numero_reclamo() hace upsert atómico por tenant-año.
     No tiene RLS ni trigger propio — tabla técnica protegida por FK
     a empresas y por el acceso controlado de la función.';

COMMENT ON COLUMN contadores_reclamo.anio IS
    'Año del consecutivo (SMALLINT). El contador reinicia a 1
     automáticamente al comenzar un año nuevo para cada tenant.';

COMMENT ON COLUMN contadores_reclamo.ultimo_consecutivo IS
    'Último número asignado en ese año. La función lo incrementa
     en 1 antes de retornar el valor, garantizando atomicidad.';

-- ============================================================
-- FUNCIÓN: generar_numero_reclamo
-- Genera el número de reclamo legible para el tenant.
-- Formato: {PREFIJO}-{YYYY}-{NNNN}  (NNNN = 4 dígitos)
-- Ejemplo: REC-FRT-2026-0001
-- Patrón ADR-020, reutilizado para reclamos (HU-032).
-- ============================================================

CREATE OR REPLACE FUNCTION generar_numero_reclamo(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql AS $$
DECLARE
    v_consecutivo INT;
BEGIN
    -- Upsert atómico: incrementa el contador o lo inicializa en 1
    INSERT INTO contadores_reclamo (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = contadores_reclamo.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    RETURN UPPER(TRIM(p_prefijo))
        || '-' || p_anio::TEXT
        || '-' || LPAD(v_consecutivo::TEXT, 4, '0');
END;
$$;

COMMENT ON FUNCTION generar_numero_reclamo IS
    'Genera el número de reclamo legible para el tenant en el año indicado.
     Formato REC-{PREFIX}-{AÑO}-{NNNN}. Upsert atómico por tenant-año
     (mismo mecanismo que generar_numero_orden, ADR-020). Se llama al
     crear un reclamo (HU-032).';
