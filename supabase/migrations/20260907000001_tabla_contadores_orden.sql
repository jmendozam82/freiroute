-- ============================================================
-- TABLA: contadores_orden
-- Descripción: Contador atómico de consecutivo de órdenes por
--              tenant y año. Implementa el mecanismo de
--              numeración automática del ADR-020.
-- Excepciones al patrón:
--   · SIN id propio — PK compuesta (empresa_id, anio)
--   · SIN activo — no es tabla de negocio
--   · SIN RLS — protegida por FK a empresas y por la función
--     generar_numero_orden() que controla el acceso
--   · SIN trigger de fecha_modificacion
--   · SIN COMMENT ON COLUMN individualmente — tabla técnica
-- ============================================================

CREATE TABLE IF NOT EXISTS contadores_orden (
    empresa_id         UUID     NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    anio               SMALLINT NOT NULL,
    ultimo_consecutivo INT      NOT NULL DEFAULT 0,
    PRIMARY KEY (empresa_id, anio)
);

COMMENT ON TABLE contadores_orden IS
    'Contador de consecutivo de órdenes por tenant y año.
     El upsert atómico en generar_numero_orden() garantiza unicidad
     sin race conditions (ADR-020). No tiene RLS ni trigger propio.';

COMMENT ON COLUMN contadores_orden.anio IS
    'Año del consecutivo (SMALLINT). El contador reinicia a 1
     automáticamente al comenzar un año nuevo para cada tenant.';

COMMENT ON COLUMN contadores_orden.ultimo_consecutivo IS
    'Último número asignado en ese año. La función lo incrementa
     en 1 antes de retornar el valor, garantizando atomicidad.';