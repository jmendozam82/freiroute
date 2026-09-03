-- ============================================================
-- MIGRACIÓN 08 - DATOS INICIALES
-- Freiroute TMS - Sprint 1 EP-01
-- ============================================================
-- Siembra inicial idempotente (ON CONFLICT DO NOTHING):
--   1. Empresa raíz del SaaS (Freiroute como tenant propio)
--   2. Perfil Super Admin del sistema (es_sistema = true)
--   3. Perfiles base plantilla: ADMIN, DISPATCHER, OPERADOR,
--      CONDUCTOR, CLIENTE (vinculados a la empresa raíz, es_sistema = true)
--   4. Permisos con flags booleanos según rol (ADR-009)
-- ============================================================

-- ── 1. Empresa raíz del SaaS ─────────────────────────────────────
INSERT INTO empresas (
    id, nombre, email_admin, pais, plan_suscripcion, estado,
    color_primario, color_secundario, moneda_principal
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Freiroute SaaS Admin',
    'admin@freiroute.com',
    'Nicaragua',
    'ENTERPRISE',
    'ACTIVE',
    '#1A73E8',
    '#0B2545',
    'USD'
) ON CONFLICT DO NOTHING;

-- ── 2. Perfil Super Admin del sistema ────────────────────────────
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000010',
    '00000000-0000-0000-0000-000000000001',
    'Super Administrador',
    'Super Admin global del SaaS Freiroute. Acceso total implícito — no usa la tabla permisos (HU-006 CA-06).',
    'SUPER_ADMIN',
    true
) ON CONFLICT DO NOTHING;

-- ── 3. Perfiles base plantilla (es_sistema = true) ───────────────

-- ADMIN: administra todo el tenant
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000011',
    '00000000-0000-0000-0000-000000000001',
    'Administrador de Empresa',
    'Administrador del tenant: acceso completo a todos los módulos de su empresa.',
    'ADMIN',
    true
) ON CONFLICT DO NOTHING;

-- DISPATCHER: planificación y asignación
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000012',
    '00000000-0000-0000-0000-000000000001',
    'Dispatcher',
    'Planificador/asignador de embarques: gestiona órdenes, embarques, carriers, rutas y track & trace.',
    'DISPATCHER',
    true
) ON CONFLICT DO NOTHING;

-- OPERADOR: operación diaria
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000013',
    '00000000-0000-0000-0000-000000000001',
    'Operador',
    'Operador de transporte: crea órdenes y embarques, consulta carriers.',
    'OPERADOR',
    true
) ON CONFLICT DO NOTHING;

-- CONDUCTOR: ejecución en campo
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000014',
    '00000000-0000-0000-0000-000000000001',
    'Conductor',
    'Conductor de vehículo: actualiza track & trace y registra POD (proof of delivery).',
    'CONDUCTOR',
    true
) ON CONFLICT DO NOTHING;

-- CLIENTE: portal del cliente
INSERT INTO perfiles (
    id, empresa_id, nombre, descripcion, tipo_perfil, es_sistema
) VALUES (
    '00000000-0000-0000-0000-000000000015',
    '00000000-0000-0000-0000-000000000001',
    'Cliente',
    'Cliente/shipper: consulta sus órdenes de transporte y documentos.',
    'CLIENTE',
    true
) ON CONFLICT DO NOTHING;

-- ── 4. Permisos base por perfil (flags booleanos, ADR-009) ───────
-- Módulos del TMS (HU-006 CA-04): ordenes, embarques, carriers, rutas,
-- track_trace, documentos, flota, analytics, facturacion, clientes,
-- usuarios, configuracion

-- ADMIN: TODOS los módulos con leer + crear + actualizar
INSERT INTO permisos (
    empresa_id, perfil_id, modulo, puede_leer, puede_crear, puede_actualizar
) VALUES
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'ordenes',       true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'embarques',     true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'carriers',      true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'rutas',         true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'track_trace',   true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'documentos',    true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'flota',         true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'analytics',     true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'facturacion',   true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'clientes',      true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'usuarios',      true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'configuracion', true, true, true)
ON CONFLICT ON CONSTRAINT uq_permiso_perfil_modulo DO NOTHING;

-- DISPATCHER: ordenes, embarques, carriers, rutas, track_trace → leer + crear + actualizar
INSERT INTO permisos (
    empresa_id, perfil_id, modulo, puede_leer, puede_crear, puede_actualizar
) VALUES
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'ordenes',     true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'embarques',   true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'carriers',    true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'rutas',       true, true, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'track_trace', true, true, true)
ON CONFLICT ON CONSTRAINT uq_permiso_perfil_modulo DO NOTHING;

-- OPERADOR: ordenes + embarques → leer + crear; carriers → solo leer
INSERT INTO permisos (
    empresa_id, perfil_id, modulo, puede_leer, puede_crear, puede_actualizar
) VALUES
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000013', 'ordenes',   true, true, false),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000013', 'embarques', true, true, false),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000013', 'carriers',  true, false, false)
ON CONFLICT ON CONSTRAINT uq_permiso_perfil_modulo DO NOTHING;

-- CONDUCTOR: track_trace → leer + actualizar (para registrar POD)
INSERT INTO permisos (
    empresa_id, perfil_id, modulo, puede_leer, puede_crear, puede_actualizar
) VALUES
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000014', 'track_trace', true, false, true),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000014', 'documentos',  true, false, true)
ON CONFLICT ON CONSTRAINT uq_permiso_perfil_modulo DO NOTHING;

-- CLIENTE: ordenes → solo leer; documentos → solo leer
INSERT INTO permisos (
    empresa_id, perfil_id, modulo, puede_leer, puede_crear, puede_actualizar
) VALUES
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000015', 'ordenes',    true, false, false),
    ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000015', 'documentos', true, false, false)
ON CONFLICT ON CONSTRAINT uq_permiso_perfil_modulo DO NOTHING;