-- ============================================================
-- MIGRACIÓN 06 - ALTER TABLE empresas (Sprint 2)
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Agrega columnas para:
--   - Vínculo con tabla planes (ADR-004)
--   - Onboarding wizard multi-paso (ADR-010)
--   - Estados ampliados del tenant (ADR-004)
--   - Numeración de carta de porte (consistencia con IConfiguracionRepository)
-- Incluye datos iniciales para la empresa raíz.
-- ============================================================

-- Agregar columnas nuevas
ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS plan_id
        UUID REFERENCES planes(id) ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS onboarding_paso_actual
        INTEGER NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS onboarding_completado
        BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS estado
        VARCHAR(50) NOT NULL DEFAULT 'TRIAL',
    ADD COLUMN IF NOT EXISTS prefijo_carta_porte
        VARCHAR(10) NOT NULL DEFAULT 'CP',
    ADD COLUMN IF NOT EXISTS consecutivo_carta_porte
        INTEGER NOT NULL DEFAULT 1;

-- Comentarios en español (obligatorio — AGENTS.md regla #14)
COMMENT ON COLUMN empresas.plan_id IS
    'Plan de suscripción activo. NULL si aún no se ha asignado.';
COMMENT ON COLUMN empresas.onboarding_paso_actual IS
    'Paso actual del wizard de configuración inicial (1-5). 1 = no iniciado.';
COMMENT ON COLUMN empresas.onboarding_completado IS
    'true cuando el Admin completó el wizard de configuración inicial';
COMMENT ON COLUMN empresas.estado IS
    'Estado del tenant: TRIAL, ACTIVE, PAST_DUE, SUSPENDED, CANCELLED';
COMMENT ON COLUMN empresas.prefijo_carta_porte IS
    'Prefijo para numeración de cartas de porte (default CP)';
COMMENT ON COLUMN empresas.consecutivo_carta_porte IS
    'Contador incremental para numeración de cartas de porte';

-- Índices para consultas frecuentes
CREATE INDEX idx_empresas_plan_id ON empresas(plan_id);
-- NOTA: idx_empresas_estado ya existe desde la migración 0002

-- ============================================================
-- DATOS INICIALES — Empresa raíz
-- Asignar plan ENTERPRISE, estado ACTIVE, onboarding completado
-- ============================================================

UPDATE empresas
SET plan_id = (SELECT id FROM planes WHERE codigo = 'ENTERPRISE'),
    estado = 'ACTIVE',
    onboarding_completado = true,
    onboarding_paso_actual = 5
WHERE id = '00000000-0000-0000-0000-000000000001';

-- ============================================================
-- SUSCRIPCIÓN INICIAL — Empresa raíz con plan ENTERPRISE
-- Ciclo anual, 10 años de gracia, precio 0 (interno)
-- NOTA: No se usa ON CONFLICT DO NOTHING porque el constraint
-- uq_suscripcion_empresa_activa es DEFERRABLE — PostgreSQL no permite
-- usar constraints deferrables como árbitro en ON CONFLICT.
-- ============================================================

INSERT INTO suscripciones (
    empresa_id, plan_id, tipo_ciclo,
    fecha_inicio, fecha_vencimiento,
    estado, precio_pactado, moneda_pactada,
    activo
)
SELECT
    '00000000-0000-0000-0000-000000000001',
    id,
    'ANUAL',
    NOW(),
    NOW() + INTERVAL '10 years',
    'ACTIVE',
    0.00, 'USD',
    true
FROM planes
WHERE codigo = 'ENTERPRISE'
  AND NOT EXISTS (
      SELECT 1 FROM suscripciones
      WHERE empresa_id = '00000000-0000-0000-0000-000000000001'
        AND activo = true
  );
