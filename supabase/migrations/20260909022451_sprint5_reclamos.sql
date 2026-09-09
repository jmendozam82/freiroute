-- ============================================================
-- TABLA: reclamos (HU-032 — Claims Management)
-- Reclamos formales de clientes por incidencias de entrega.
-- FSM de estados: ABIERTO → EN_REVISION → APROBADO/RECHAZADO → CERRADO.
-- Tipos: DANO | PERDIDA | RETRASO | OTRO.
-- El número legible se genera al crear: REC-{PREFIX}-{AÑO}-{NNNN}.
-- referencias_evidencia: TEXT[] — URLs a Supabase Storage (Sprint 11).
-- ============================================================

CREATE TABLE IF NOT EXISTS reclamos (
    id                    UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id            UUID         NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id              UUID         NOT NULL REFERENCES ordenes(id),
    numero_reclamo        VARCHAR(30),
    tipo                  VARCHAR(20)  NOT NULL,
    descripcion           TEXT         NOT NULL,
    monto_reclamado       NUMERIC(15,2),
    estado                VARCHAR(20)  NOT NULL DEFAULT 'ABIERTO',
    referencias_evidencia TEXT[],
    activo                BOOLEAN      NOT NULL DEFAULT true,
    fecha_creacion        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fecha_modificacion    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    creado_por            UUID         REFERENCES usuarios(id),
    modificado_por        UUID         REFERENCES usuarios(id)
);

COMMENT ON TABLE reclamos IS
    'Reclamos formales de clientes por incidencias (DANO|PERDIDA|RETRASO|OTRO).
     FSM: ABIERTO→EN_REVISION→APROBADO/RECHAZADO→CERRADO.';

COMMENT ON COLUMN reclamos.numero_reclamo IS
    'Número legible generado al crear: REC-{PREFIX}-{AÑO}-{NNNN}.
     Patrón ADR-020 reutilizado para reclamos (HU-032).';

COMMENT ON COLUMN reclamos.tipo IS
    'Tipo de reclamo: DANO | PERDIDA | RETRASO | OTRO.';

COMMENT ON COLUMN reclamos.estado IS
    'Estado FSM del reclamo: ABIERTO | EN_REVISION | APROBADO | RECHAZADO | CERRADO.';

COMMENT ON COLUMN reclamos.referencias_evidencia IS
    'Referencias de evidencia: URLs a documentos (Supabase Storage, Sprint 11).';

ALTER TABLE reclamos ENABLE ROW LEVEL SECURITY;

-- Aislamiento multi-tenant (ADR-003)
CREATE POLICY reclamos_tenant_isolation ON reclamos
    USING (empresa_id::TEXT = current_setting('app.current_empresa_id', true));

-- Índices operativos
CREATE INDEX IF NOT EXISTS idx_reclamos_orden
    ON reclamos(empresa_id, orden_id);

CREATE INDEX IF NOT EXISTS idx_reclamos_estado
    ON reclamos(empresa_id, estado)
    WHERE activo = true;

CREATE INDEX IF NOT EXISTS idx_reclamos_numero
    ON reclamos(empresa_id, numero_reclamo)
    WHERE numero_reclamo IS NOT NULL;

-- Unicidad del número legible por tenant (índice parcial, red de seguridad
-- contra race conditions — mismo patrón que numero_orden)
CREATE UNIQUE INDEX IF NOT EXISTS idx_reclamos_numero_unique
    ON reclamos(empresa_id, numero_reclamo)
    WHERE numero_reclamo IS NOT NULL;

-- Trigger estándar update_fecha_modificacion
CREATE TRIGGER trg_reclamos_fecha_modificacion
    BEFORE UPDATE ON reclamos
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
