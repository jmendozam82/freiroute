-- ============================================================
-- MIGRACIÓN 11 - DATOS INICIALES Catálogos (empresa raíz)
-- Freiroute TMS - Sprint 3 EP-03 (HU-018, ADR-003)
-- ============================================================
-- Semillas del sistema en la empresa raíz (plantilla):
--   - 11 unidades de medida estándar (PESO: kg, g, ton, lb |
--     VOLUMEN: m3, L, ft3 | LONGITUD: m, cm, in, ft)
--   - 7 tipos de embalaje estándar (PLT, CAJA, TAM, CTN20,
--     CTN40, GRA, BOB)
--
-- Estos registros SON la plantilla que UnidadMedidaRepository.
-- CopiarUnidadesEstandarAsync y TipoEmbalajeRepository.
-- CopiarEmbalajesEstandarAsync copian a cada tenant nuevo
-- (HU-018 CA-02/CA-04, EmpresaService.CreateAsync).
--
-- La empresa raíz es '00000000-0000-0000-0000-000000000001'
-- (coincide con IdsSistema.EmpresaRaizId).
-- Todos los INSERT usan ON CONFLICT DO NOTHING: idempotentes y
-- seguros ante re-ejecución. NO se insertan ubicaciones ni clientes
-- (son datos de negocio del tenant, no plantillas del sistema).
-- ============================================================

-- ── Unidades de medida estándar ─────────────────────────────────

-- PESO (unidad base: kg)
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Kilogramo', 'kg', 'PESO', 1.0, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Gramo', 'g', 'PESO', 0.001, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Tonelada métrica', 'ton', 'PESO', 1000.0, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Libra', 'lb', 'PESO', 0.453592, 'kg')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- VOLUMEN (unidad base: m3)
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Metro cúbico', 'm3', 'VOLUMEN', 1.0, 'm3'),
('00000000-0000-0000-0000-000000000001', 'Litro', 'L', 'VOLUMEN', 0.001, 'm3'),
('00000000-0000-0000-0000-000000000001', 'Pie cúbico', 'ft3', 'VOLUMEN', 0.0283168, 'm3')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- LONGITUD (unidad base: m)
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Metro', 'm', 'LONGITUD', 1.0, 'm'),
('00000000-0000-0000-0000-000000000001', 'Centímetro', 'cm', 'LONGITUD', 0.01, 'm'),
('00000000-0000-0000-0000-000000000001', 'Pulgada', 'in', 'LONGITUD', 0.0254, 'm'),
('00000000-0000-0000-0000-000000000001', 'Pie', 'ft', 'LONGITUD', 0.3048, 'm')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- ── Tipos de embalaje estándar ──────────────────────────────────

INSERT INTO tipos_embalaje (empresa_id, nombre, codigo,
    capacidad_kg, capacidad_m3, apilable) VALUES
('00000000-0000-0000-0000-000000000001',
    'Pallet estándar', 'PLT', 1500, 1.5, true),
('00000000-0000-0000-0000-000000000001',
    'Caja de cartón', 'CAJA', 30, 0.05, true),
('00000000-0000-0000-0000-000000000001',
    'Tambor metálico', 'TAM', 250, 0.2, false),
('00000000-0000-0000-0000-000000000001',
    'Contenedor 20 pies', 'CTN20', 28000, 33.2, false),
('00000000-0000-0000-0000-000000000001',
    'Contenedor 40 pies', 'CTN40', 28000, 67.7, false),
('00000000-0000-0000-0000-000000000001',
    'A granel', 'GRA', NULL, NULL, false),
('00000000-0000-0000-0000-000000000001',
    'Bobina', 'BOB', 5000, 2.0, false)
ON CONFLICT (empresa_id, codigo) DO NOTHING;