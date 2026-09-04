-- ============================================================
-- MIGRACIÓN 01 - TABLA planes (Catálogo global)
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Tabla catálogo global de planes de suscripción del SaaS.
-- EXCEPCIONES al patrón estándar (ADR-004):
--   - SIN empresa_id (catálogo global del SaaS)
--   - SIN RLS (solo el SUPER_ADMIN la gestiona)
--   - SÍ tiene activo, fecha_creacion, fecha_modificacion
--   - SÍ tiene trigger de fecha_modificacion
-- ============================================================

CREATE TABLE IF NOT EXISTS planes (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Datos del plan
    nombre                  VARCHAR(100) NOT NULL,
    codigo                  VARCHAR(50)  NOT NULL UNIQUE,
    descripcion             TEXT,
    -- Límites operativos
    limite_usuarios         INTEGER NOT NULL DEFAULT 5,
    limite_embarques_mes    INTEGER NOT NULL DEFAULT 500,
    limite_storage_gb       INTEGER NOT NULL DEFAULT 1,
    -- Precio
    precio_mensual          NUMERIC(10,2) NOT NULL DEFAULT 0,
    precio_anual            NUMERIC(10,2) NOT NULL DEFAULT 0,
    moneda                  VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Módulos disponibles (array de strings)
    modulos_disponibles     TEXT[] NOT NULL DEFAULT '{}',
    -- Control de visibilidad
    es_publico              BOOLEAN NOT NULL DEFAULT true,
    -- Control (soft delete universal)
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ
);

COMMENT ON TABLE planes IS
    'Planes de suscripción del SaaS Freiroute. Gestionados por Super Admin. Catálogo global sin empresa_id.';
COMMENT ON COLUMN planes.id IS
    'Identificador único del plan (UUID generado por la BD)';
COMMENT ON COLUMN planes.nombre IS
    'Nombre visible del plan: Starter, Professional, Enterprise';
COMMENT ON COLUMN planes.codigo IS
    'Código único del plan: STARTER, PROFESSIONAL, ENTERPRISE';
COMMENT ON COLUMN planes.descripcion IS
    'Descripción corta del plan para el portal de signup';
COMMENT ON COLUMN planes.limite_usuarios IS
    'Máximo de usuarios activos. -1 significa ilimitado.';
COMMENT ON COLUMN planes.limite_embarques_mes IS
    'Máximo de embarques por mes. -1 significa ilimitado.';
COMMENT ON COLUMN planes.limite_storage_gb IS
    'Almacenamiento máximo en GB para documentos del tenant';
COMMENT ON COLUMN planes.precio_mensual IS
    'Precio mensual de suscripción en la moneda del plan';
COMMENT ON COLUMN planes.precio_anual IS
    'Precio anual de suscripción (descuento vs mensual)';
COMMENT ON COLUMN planes.moneda IS
    'Moneda del precio: USD, EUR, etc.';
COMMENT ON COLUMN planes.modulos_disponibles IS
    'Array con los códigos de módulos disponibles para este plan (ej: ordenes, embarques, carriers)';
COMMENT ON COLUMN planes.es_publico IS
    'true = visible en el portal de signup. false = solo accesible por invitación del Super Admin';
COMMENT ON COLUMN planes.activo IS
    'Soft delete: false = plan desactivado (no se puede asignar a nuevos tenants)';
COMMENT ON COLUMN planes.fecha_creacion IS
    'Timestamp de creación del registro';
COMMENT ON COLUMN planes.fecha_modificacion IS
    'Timestamp de la última modificación (lo actualiza el trigger)';

-- Índices
CREATE INDEX idx_planes_codigo ON planes(codigo);
CREATE INDEX idx_planes_activo ON planes(activo);

-- NO tiene empresa_id — es catálogo global del SaaS (ADR-004)
-- NO tiene RLS — solo el Super Admin la gestiona

-- Trigger para actualizar fecha_modificacion
CREATE TRIGGER trg_planes_fecha_modificacion
    BEFORE UPDATE ON planes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

-- ============================================================
-- DATOS INICIALES — STARTER, PROFESSIONAL, ENTERPRISE
-- Los módulos en cada plan coinciden EXACTAMENTE con los valores
-- de ModuloPermiso.cs (src/Freiroute.Utility/Constants/)
-- ============================================================

INSERT INTO planes (nombre, codigo, descripcion,
    limite_usuarios, limite_embarques_mes, limite_storage_gb,
    precio_mensual, precio_anual,
    modulos_disponibles) VALUES
(
    'Starter', 'STARTER',
    'Ideal para empresas de transporte pequeñas',
    5, 500, 1, 99.00, 990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace','documentos']
),
(
    'Professional', 'PROFESSIONAL',
    'Para empresas en crecimiento con operaciones medianas',
    25, 5000, 10, 299.00, 2990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace',
          'documentos','analytics','facturacion','clientes','flota']
),
(
    'Enterprise', 'ENTERPRISE',
    'Para grandes operaciones de transporte sin límites',
    -1, -1, 100, 799.00, 7990.00,
    ARRAY['ordenes','embarques','carriers','rutas','track_trace',
          'documentos','analytics','facturacion','clientes',
          'flota','usuarios','configuracion']
)
ON CONFLICT (codigo) DO NOTHING;
