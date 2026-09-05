-- ============================================================
-- Migración: Columnas de configuración operativa en empresas
-- Detectadas en re-smoke test como faltantes
-- Referenciadas en ConfiguracionRepository y OnboardingService
-- ============================================================

ALTER TABLE empresas
    ADD COLUMN IF NOT EXISTS industria
        VARCHAR(100),
    ADD COLUMN IF NOT EXISTS sitio_web
        VARCHAR(300),
    ADD COLUMN IF NOT EXISTS email_remitente
        VARCHAR(200),
    ADD COLUMN IF NOT EXISTS nombre_remitente
        VARCHAR(200),
    ADD COLUMN IF NOT EXISTS modos_transporte_activos
        TEXT[] NOT NULL DEFAULT '{}';

COMMENT ON COLUMN empresas.industria IS
    'Industria o sector de la empresa: Transporte de carga, Logística, etc.';
COMMENT ON COLUMN empresas.sitio_web IS
    'Sitio web corporativo de la empresa (opcional)';
COMMENT ON COLUMN empresas.email_remitente IS
    'Email desde el que se envían notificaciones del sistema al tenant';
COMMENT ON COLUMN empresas.nombre_remitente IS
    'Nombre del remitente para notificaciones (ej: "Freiroute - Trans Demo")';
COMMENT ON COLUMN empresas.modos_transporte_activos IS
    'Array de modos de transporte activos: FTL, LTL, AEREO, MARITIMO,
     FERROVIARIO, INTERMODAL. Configurado en el onboarding paso 3.';

-- Actualizar empresa raíz con valores por defecto
UPDATE empresas
SET
    industria                = 'Transporte de carga',
    modos_transporte_activos = ARRAY['FTL', 'LTL', 'AEREO',
                                     'MARITIMO', 'FERROVIARIO', 'INTERMODAL']
WHERE id = '00000000-0000-0000-0000-000000000001';