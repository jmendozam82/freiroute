-- ============================================================
-- MIGRACIÓN 02 - TABLA empresas
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Tabla raíz de tenants del SaaS Freiroute.
-- EXCEPCIÓN arquitectónica (ADR-003):
--   - NO tiene empresa_id propio (es la raíz del árbol de tenants)
--   - NO tiene RLS (la gestiona el SUPER_ADMIN sin filtro de tenant)
--   - SÍ tiene activo, fecha_creacion, fecha_modificacion
--   - SÍ tiene trigger de fecha_modificacion
-- ============================================================

CREATE TABLE IF NOT EXISTS empresas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre              VARCHAR(200) NOT NULL,
    ruc_nit             VARCHAR(50),
    email_admin         VARCHAR(200) NOT NULL UNIQUE,
    telefono            VARCHAR(50),
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    ciudad              VARCHAR(100),
    direccion           TEXT,
    logo_url            TEXT,
    -- Personalización white-label
    color_primario      VARCHAR(7)  DEFAULT '#1A73E8',
    color_secundario    VARCHAR(7)  DEFAULT '#0B2545',
    -- Suscripción y estado del tenant
    plan_suscripcion    VARCHAR(50) NOT NULL DEFAULT 'STARTER',
    estado              VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
    -- Configuración operativa
    moneda_principal    VARCHAR(10) NOT NULL DEFAULT 'USD',
    zona_horaria        VARCHAR(100) NOT NULL DEFAULT 'America/Managua',
    idioma              VARCHAR(10) NOT NULL DEFAULT 'es',
    formato_fecha       VARCHAR(20) NOT NULL DEFAULT 'DD/MM/YYYY',
    -- Numeración de documentos
    prefijo_embarque    VARCHAR(10) NOT NULL DEFAULT 'FR',
    consecutivo_embarque INTEGER NOT NULL DEFAULT 1,
    prefijo_orden       VARCHAR(10) NOT NULL DEFAULT 'ORD',
    consecutivo_orden   INTEGER NOT NULL DEFAULT 1,
    -- Control (soft delete universal)
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE empresas IS
'Tabla raíz de tenants del SaaS Freiroute. Cada registro es una empresa de transporte suscrita. No tiene empresa_id propio ni RLS: la gestiona el SUPER_ADMIN sin filtro de tenant.';
COMMENT ON COLUMN empresas.id IS 'Identificador único del tenant (UUID generado por la BD)';
COMMENT ON COLUMN empresas.nombre IS 'Nombre comercial de la empresa de transporte';
COMMENT ON COLUMN empresas.ruc_nit IS 'Documento tributario: RUC o NIT de la empresa';
COMMENT ON COLUMN empresas.email_admin IS 'Email del administrador del tenant. Único a nivel global (HU-001: unicidad en registro)';
COMMENT ON COLUMN empresas.telefono IS 'Teléfono de contacto principal del tenant';
COMMENT ON COLUMN empresas.pais IS 'País de operación de la empresa (default: Nicaragua)';
COMMENT ON COLUMN empresas.ciudad IS 'Ciudad principal de operación';
COMMENT ON COLUMN empresas.direccion IS 'Dirección física de la empresa';
COMMENT ON COLUMN empresas.logo_url IS 'URL del logo en Supabase Storage (bucket privado)';
COMMENT ON COLUMN empresas.color_primario IS 'Color hex para personalización white-label del tenant';
COMMENT ON COLUMN empresas.color_secundario IS 'Color hex secundario para personalización white-label';
COMMENT ON COLUMN empresas.plan_suscripcion IS 'Plan contratado: STARTER, PROFESSIONAL, ENTERPRISE';
COMMENT ON COLUMN empresas.estado IS 'Estado del tenant: ACTIVE, SUSPENDED, CANCELLED';
COMMENT ON COLUMN empresas.moneda_principal IS 'Moneda por defecto del tenant (default: USD)';
COMMENT ON COLUMN empresas.zona_horaria IS 'Zona horaria del tenant (default: America/Managua)';
COMMENT ON COLUMN empresas.idioma IS 'Idioma por defecto de la interfaz (default: es)';
COMMENT ON COLUMN empresas.formato_fecha IS 'Formato de fecha por defecto (default: DD/MM/YYYY)';
COMMENT ON COLUMN empresas.prefijo_embarque IS 'Prefijo para numeración de embarques (default FR, ej: FR-2026-00001)';
COMMENT ON COLUMN empresas.consecutivo_embarque IS 'Contador incremental para numeración de embarques';
COMMENT ON COLUMN empresas.prefijo_orden IS 'Prefijo para numeración de órdenes de transporte (default ORD)';
COMMENT ON COLUMN empresas.consecutivo_orden IS 'Contador incremental para numeración de órdenes';
COMMENT ON COLUMN empresas.activo IS 'Soft delete universal: false = tenant desactivado (nunca se borra físicamente)';
COMMENT ON COLUMN empresas.fecha_creacion IS 'Timestamp de creación del registro';
COMMENT ON COLUMN empresas.fecha_modificacion IS 'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices obligatorios + índices de consulta frecuente
CREATE INDEX idx_empresas_activo ON empresas(activo);
CREATE INDEX idx_empresas_estado ON empresas(estado);
CREATE INDEX idx_empresas_plan   ON empresas(plan_suscripcion);

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_empresas_fecha_modificacion
    BEFORE UPDATE ON empresas
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();