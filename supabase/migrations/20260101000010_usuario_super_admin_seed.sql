-- ═══════════════════════════════════════════════════════════════
-- Seed: Usuario Super Admin para desarrollo local
-- Crea el primer usuario del sistema vinculado a la empresa raíz
-- y al perfil SUPER_ADMIN. Solo para entorno de desarrollo.
-- ═══════════════════════════════════════════════════════════════

-- Usuario Super Admin del SaaS
-- email: admin@freiroute.com
-- password: Admin123! (el stub de auth en Sprint 1 acepta cualquier
--           password excepto "wrong-password")
INSERT INTO usuarios (
    id,
    empresa_id,
    perfil_id,
    tipo_identidad,
    numero_identidad,
    nombre_completo,
    email,
    telefono,
    tipo_usuario,
    estado,
    intentos_fallidos,
    bloqueado_hasta,
    activo,
    fecha_creacion
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',   -- Empresa raíz Freiroute SaaS Admin
    '00000000-0000-0000-0000-000000000010',   -- Perfil Super Administrador
    'CEDULA',
    '001-010193-0001A',
    'Administrador Freiroute',
    'admin@freiroute.com',
    '+505 8888-0000',
    'SUPER_ADMIN',
    'ACTIVE',
    0,
    NULL,
    true,
    NOW()
) ON CONFLICT (email, empresa_id) DO NOTHING;

COMMENT ON TABLE usuarios IS 'Usuarios del sistema por empresa. La autenticación la gestiona Supabase Auth; este registro guarda el perfil de negocio.';
