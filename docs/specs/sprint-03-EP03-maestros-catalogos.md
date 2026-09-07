# Spec: Sprint 3 — EP-03 Gestión de Maestros y Catálogos

**Sprint:** 03
**Épica:** EP-03 — Gestión de Maestros y Catálogos Base
**Historias:** HU-004 (OAuth diferido) · HU-015 · HU-016 · HU-017 · HU-018 · HU-019 · HU-020
**Story Points:** ~89 pts
**Objetivo del Sprint:** Construir los catálogos de datos maestros que
alimentan todos los módulos operativos del TMS. Sin ubicaciones,
zonas, tipos de mercancía y tarifas, no es posible crear órdenes
ni planificar embarques en los sprints siguientes.
**ADRs aplicables:** ADR-003 · ADR-014 · ADR-015

---

## Dependencias

```
Sprint 1 → Auth, tenants, perfiles, permisos
Sprint 2 → Planes, suscripciones, onboarding, configuración
Sprint 3 (este) → BASE para Sprint 4-5 (Órdenes) y Sprint 6 (Carriers)

Orden interno del sprint:
HU-015 (Ubicaciones) → HU-016 (Zonas) → HU-017 (Mercancías)
→ HU-018 (Unidades) → HU-019 (Clientes) → HU-020 (Tarifas)
Las tarifas referencian zonas — HU-016 debe existir antes de HU-020.
```

---

## Orden de Implementación

```
1. @Arquitecto    → Entities + DTOs + Interfaces
2. @IngenieroDatos → Migraciones + Repositorios
3. @BackendDev    → BLL Services + API Controllers
                    + HU-004 OAuth (completar stub)
4. @QA            → Tests BLL ≥80% + API ≥60%
5. @FrontendDev   → Vistas de catálogos
```

---

## Tablas de Base de Datos del Sprint 3

### Tabla: `ubicaciones`

```sql
CREATE TABLE IF NOT EXISTS ubicaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    -- Identificación
    nombre              VARCHAR(200) NOT NULL,
    codigo              VARCHAR(50),
    tipo                VARCHAR(50) NOT NULL DEFAULT 'OTRO',
    -- Dirección
    direccion           TEXT,
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    departamento        VARCHAR(100),
    ciudad              VARCHAR(100),
    codigo_postal       VARCHAR(20),
    -- Geocodificación (ADR-014)
    latitud             DOUBLE PRECISION,
    longitud            DOUBLE PRECISION,
    georeferenciada     BOOLEAN NOT NULL DEFAULT false,
    direccion_normalizada TEXT,
    -- Datos operativos
    contacto_nombre     VARCHAR(200),
    contacto_telefono   VARCHAR(50),
    contacto_email      VARCHAR(200),
    horario_apertura    TIME,
    horario_cierre      TIME,
    tiempo_servicio_min INTEGER NOT NULL DEFAULT 30,
    instrucciones       TEXT,
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_ubicacion_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE ubicaciones IS
    'Ubicaciones georreferenciadas del tenant: almacenes, clientes,
     puertos, aeropuertos y puntos de cruce. Base para la planificación
     de rutas y el cálculo de distancias.';
COMMENT ON COLUMN ubicaciones.tipo IS
    'ALMACEN, CLIENTE, PUERTO, AEROPUERTO, TERMINAL, CRUCE_FRONTERA,
     PUNTO_RECARGA, OTRO';
COMMENT ON COLUMN ubicaciones.georeferenciada IS
    'true cuando latitud/longitud han sido validadas vía geocodificación';
COMMENT ON COLUMN ubicaciones.tiempo_servicio_min IS
    'Tiempo estimado de carga/descarga en minutos. Usado en optimización
     de rutas para calcular ETA con paradas.';

CREATE INDEX idx_ubicaciones_empresa_id     ON ubicaciones(empresa_id);
CREATE INDEX idx_ubicaciones_activo         ON ubicaciones(activo);
CREATE INDEX idx_ubicaciones_empresa_activo ON ubicaciones(empresa_id, activo);
CREATE INDEX idx_ubicaciones_tipo           ON ubicaciones(tipo);
CREATE INDEX idx_ubicaciones_geo ON ubicaciones(latitud, longitud)
    WHERE georeferenciada = true;

ALTER TABLE ubicaciones ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ubicaciones" ON ubicaciones
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_ubicaciones_fecha_modificacion
    BEFORE UPDATE ON ubicaciones
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `zonas_entrega`

```sql
CREATE TABLE IF NOT EXISTS zonas_entrega (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(200) NOT NULL,
    codigo              VARCHAR(50) NOT NULL,
    descripcion         TEXT,
    color_hex           VARCHAR(7) NOT NULL DEFAULT '#1A73E8',
    -- Definición geográfica
    tipo_definicion     VARCHAR(20) NOT NULL DEFAULT 'POLIGONO',
    poligono_geojson    TEXT,
    codigos_postales    TEXT[],
    ciudades            TEXT[],
    departamentos       TEXT[],
    paises              TEXT[] NOT NULL DEFAULT '{Nicaragua}',
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_zona_codigo_empresa UNIQUE (empresa_id, codigo)
);

COMMENT ON TABLE zonas_entrega IS
    'Zonas geográficas de cobertura del tenant. Usadas en tarifas,
     asignación de carriers y planificación de rutas.';
COMMENT ON COLUMN zonas_entrega.tipo_definicion IS
    'POLIGONO (GeoJSON), CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES';
COMMENT ON COLUMN zonas_entrega.color_hex IS
    'Color en formato #RRGGBB para visualización en el mapa';
COMMENT ON COLUMN zonas_entrega.poligono_geojson IS
    'GeoJSON Polygon o MultiPolygon que define el área de la zona.
     Usado para verificar si una ubicación cae dentro de la zona.';

CREATE INDEX idx_zonas_empresa_id     ON zonas_entrega(empresa_id);
CREATE INDEX idx_zonas_activo         ON zonas_entrega(activo);
CREATE INDEX idx_zonas_empresa_activo ON zonas_entrega(empresa_id, activo);

ALTER TABLE zonas_entrega ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_zonas" ON zonas_entrega
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_zonas_fecha_modificacion
    BEFORE UPDATE ON zonas_entrega
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `ubicacion_zonas` (tabla de relación)

```sql
CREATE TABLE IF NOT EXISTS ubicacion_zonas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    ubicacion_id    UUID NOT NULL REFERENCES ubicaciones(id) ON DELETE CASCADE,
    zona_id         UUID NOT NULL REFERENCES zonas_entrega(id) ON DELETE CASCADE,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_ubicacion_zona UNIQUE (ubicacion_id, zona_id)
);

COMMENT ON TABLE ubicacion_zonas IS
    'Relación muchos-a-muchos: una ubicación puede pertenecer a
     múltiples zonas de entrega.';

CREATE INDEX idx_ubiz_empresa_id   ON ubicacion_zonas(empresa_id);
CREATE INDEX idx_ubiz_ubicacion_id ON ubicacion_zonas(ubicacion_id);
CREATE INDEX idx_ubiz_zona_id      ON ubicacion_zonas(zona_id);

ALTER TABLE ubicacion_zonas ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ubicacion_zonas" ON ubicacion_zonas
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);
```

---

### Tabla: `tipos_mercancia`

```sql
CREATE TABLE IF NOT EXISTS tipos_mercancia (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(200) NOT NULL,
    codigo                  VARCHAR(50),
    descripcion             TEXT,
    -- Clasificación
    categoria               VARCHAR(100),
    clase_peligrosidad      VARCHAR(10),
    codigo_onu              VARCHAR(10),
    codigo_hs               VARCHAR(20),
    -- Características físicas
    peso_maximo_kg          NUMERIC(10,3),
    volumen_maximo_m3       NUMERIC(10,3),
    temperatura_minima_c    NUMERIC(5,2),
    temperatura_maxima_c    NUMERIC(5,2),
    -- Flags de manejo
    requiere_refrigeracion  BOOLEAN NOT NULL DEFAULT false,
    es_fragil               BOOLEAN NOT NULL DEFAULT false,
    es_peligroso            BOOLEAN NOT NULL DEFAULT false,
    es_perecedero           BOOLEAN NOT NULL DEFAULT false,
    es_sobredimensionado    BOOLEAN NOT NULL DEFAULT false,
    requiere_fumigacion     BOOLEAN NOT NULL DEFAULT false,
    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    CONSTRAINT uq_mercancia_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE tipos_mercancia IS
    'Catálogo de tipos de mercancía del tenant. Define cómo se maneja
     y clasifica la carga para la planificación de embarques.';
COMMENT ON COLUMN tipos_mercancia.clase_peligrosidad IS
    'Clase HAZMAT según normativa ONU: 1 (Explosivos), 2 (Gases),
     3 (Líquidos inflamables), 4 (Sólidos inflamables),
     5 (Oxidantes), 6 (Tóxicos), 7 (Radioactivos),
     8 (Corrosivos), 9 (Varios). NULL si no es peligroso.';
COMMENT ON COLUMN tipos_mercancia.codigo_hs IS
    'Código del Sistema Armonizado (HS Code) para aduanas. Ej: 0401.10';

CREATE INDEX idx_mercancia_empresa_id     ON tipos_mercancia(empresa_id);
CREATE INDEX idx_mercancia_activo         ON tipos_mercancia(activo);
CREATE INDEX idx_mercancia_empresa_activo ON tipos_mercancia(empresa_id, activo);
CREATE INDEX idx_mercancia_peligroso      ON tipos_mercancia(es_peligroso)
    WHERE es_peligroso = true;

ALTER TABLE tipos_mercancia ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_mercancia" ON tipos_mercancia
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_mercancia_fecha_modificacion
    BEFORE UPDATE ON tipos_mercancia
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `unidades_medida`

```sql
CREATE TABLE IF NOT EXISTS unidades_medida (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    simbolo             VARCHAR(20) NOT NULL,
    tipo                VARCHAR(50) NOT NULL,
    factor_conversion   NUMERIC(18,8) NOT NULL DEFAULT 1,
    unidad_base         VARCHAR(20) NOT NULL,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_unidad_simbolo_empresa UNIQUE (empresa_id, simbolo)
);

COMMENT ON TABLE unidades_medida IS
    'Catálogo de unidades de medida del tenant para peso, volumen
     y dimensiones. Incluye el factor de conversión a la unidad base.';
COMMENT ON COLUMN unidades_medida.tipo IS
    'PESO, VOLUMEN, LONGITUD, TEMPERATURA';
COMMENT ON COLUMN unidades_medida.factor_conversion IS
    'Factor para convertir a la unidad base del tipo.
     Ej: 1 lb = 0.453592 kg → factor = 0.453592';
COMMENT ON COLUMN unidades_medida.unidad_base IS
    'Unidad base del sistema: kg (peso), m3 (volumen), m (longitud), C (temp)';

CREATE INDEX idx_unidades_empresa_id     ON unidades_medida(empresa_id);
CREATE INDEX idx_unidades_tipo           ON unidades_medida(tipo);
CREATE INDEX idx_unidades_empresa_activo ON unidades_medida(empresa_id, activo);

ALTER TABLE unidades_medida ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_unidades" ON unidades_medida
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_unidades_fecha_modificacion
    BEFORE UPDATE ON unidades_medida
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `tipos_embalaje`

```sql
CREATE TABLE IF NOT EXISTS tipos_embalaje (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    codigo              VARCHAR(20) NOT NULL,
    descripcion         TEXT,
    capacidad_kg        NUMERIC(10,3),
    capacidad_m3        NUMERIC(10,3),
    apilable            BOOLEAN NOT NULL DEFAULT true,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    CONSTRAINT uq_embalaje_codigo_empresa UNIQUE (empresa_id, codigo)
);

COMMENT ON TABLE tipos_embalaje IS
    'Tipos de embalaje del tenant: pallets, cajas, tambores, contenedores.
     Usados al registrar la carga en las órdenes de transporte.';
COMMENT ON COLUMN tipos_embalaje.codigo IS
    'Código corto: PLT (pallet), CAJA, TAM (tambor), CTN (contenedor),
     BOB (bobina), GRA (a granel)';

CREATE INDEX idx_embalaje_empresa_id     ON tipos_embalaje(empresa_id);
CREATE INDEX idx_embalaje_empresa_activo ON tipos_embalaje(empresa_id, activo);

ALTER TABLE tipos_embalaje ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_embalaje" ON tipos_embalaje
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_embalaje_fecha_modificacion
    BEFORE UPDATE ON tipos_embalaje
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `clientes`

```sql
CREATE TABLE IF NOT EXISTS clientes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    -- Identificación
    nombre              VARCHAR(200) NOT NULL,
    nombre_comercial    VARCHAR(200),
    ruc_nit             VARCHAR(50),
    tipo_documento      VARCHAR(20) NOT NULL DEFAULT 'RUC',
    tipo_cliente        VARCHAR(50) NOT NULL DEFAULT 'REGULAR',
    industria           VARCHAR(100),
    -- Contacto principal
    email               VARCHAR(200),
    telefono            VARCHAR(50),
    sitio_web           VARCHAR(300),
    -- Dirección fiscal
    direccion_fiscal    TEXT,
    pais                VARCHAR(100) NOT NULL DEFAULT 'Nicaragua',
    departamento        VARCHAR(100),
    ciudad              VARCHAR(100),
    -- Ubicación de despacho por defecto
    ubicacion_defecto_id UUID REFERENCES ubicaciones(id) ON DELETE SET NULL,
    -- Configuración comercial
    credito_dias        INTEGER NOT NULL DEFAULT 0,
    limite_credito      NUMERIC(18,2) NOT NULL DEFAULT 0,
    moneda              VARCHAR(10) NOT NULL DEFAULT 'USD',
    estado_credito      VARCHAR(50) NOT NULL DEFAULT 'AL_DIA',
    -- SLA
    sla_dias_entrega    INTEGER,
    sla_ventana_inicio  TIME,
    sla_ventana_fin     TIME,
    -- Control
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE clientes IS
    'Shippers / clientes del tenant que contratan servicios de transporte.
     Contiene la configuración comercial, de crédito y SLA por cliente.';
COMMENT ON COLUMN clientes.tipo_cliente IS
    'REGULAR, VIP, OCASIONAL, CORPORATIVO, GOBIERNO';
COMMENT ON COLUMN clientes.estado_credito IS
    'AL_DIA, EN_MORA, BLOQUEADO, SIN_CREDITO';
COMMENT ON COLUMN clientes.credito_dias IS
    'Días de crédito para pago de facturas. 0 = pago al contado.';
COMMENT ON COLUMN clientes.sla_dias_entrega IS
    'Días máximos de entrega pactados. NULL si no hay SLA formal.';

CREATE INDEX idx_clientes_empresa_id     ON clientes(empresa_id);
CREATE INDEX idx_clientes_activo         ON clientes(activo);
CREATE INDEX idx_clientes_empresa_activo ON clientes(empresa_id, activo);
CREATE INDEX idx_clientes_tipo           ON clientes(tipo_cliente);
CREATE INDEX idx_clientes_estado_credito ON clientes(estado_credito);
CREATE INDEX idx_clientes_ruc            ON clientes(ruc_nit)
    WHERE ruc_nit IS NOT NULL;

ALTER TABLE clientes ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_clientes" ON clientes
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_clientes_fecha_modificacion
    BEFORE UPDATE ON clientes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `contactos_cliente`

```sql
CREATE TABLE IF NOT EXISTS contactos_cliente (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    cliente_id      UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    nombre          VARCHAR(200) NOT NULL,
    cargo           VARCHAR(100),
    rol             VARCHAR(50) NOT NULL DEFAULT 'GENERAL',
    email           VARCHAR(200),
    telefono        VARCHAR(50),
    es_principal    BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE contactos_cliente IS
    'Múltiples contactos por cliente con rol diferenciado.
     El contacto principal recibe las notificaciones del sistema.';
COMMENT ON COLUMN contactos_cliente.rol IS
    'GENERAL, LOGISTICA, COMPRAS, FINANZAS, RECEPCION, GERENCIA';

CREATE INDEX idx_contactos_cliente_id ON contactos_cliente(cliente_id);
CREATE INDEX idx_contactos_empresa_id ON contactos_cliente(empresa_id);

ALTER TABLE contactos_cliente ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_contactos" ON contactos_cliente
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);
```

---

### Tabla: `tarifas_base`

```sql
CREATE TABLE IF NOT EXISTS tarifas_base (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(200) NOT NULL,
    codigo                  VARCHAR(50),
    -- Aplicación de la tarifa
    zona_origen_id          UUID REFERENCES zonas_entrega(id) ON DELETE RESTRICT,
    zona_destino_id         UUID REFERENCES zonas_entrega(id) ON DELETE RESTRICT,
    modo_transporte         VARCHAR(50) NOT NULL DEFAULT 'FTL',
    tipo_servicio           VARCHAR(50) NOT NULL DEFAULT 'ESTANDAR',
    -- Modelo de precio (ADR-015)
    tipo_tarifa             VARCHAR(30) NOT NULL DEFAULT 'FIJO_VIAJE',
    precio_unitario         NUMERIC(18,4) NOT NULL,
    precio_minimo           NUMERIC(18,4),
    moneda                  VARCHAR(10) NOT NULL DEFAULT 'USD',
    -- Vigencia
    fecha_vigencia_desde    DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_vigencia_hasta    DATE,
    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ,
    CONSTRAINT uq_tarifa_codigo_empresa
        UNIQUE (empresa_id, codigo)
        DEFERRABLE INITIALLY DEFERRED
);

COMMENT ON TABLE tarifas_base IS
    'Tarifas de flete por zona, modo y tipo de servicio del tenant.
     Historial completo preservado — no se eliminan registros vencidos.';
COMMENT ON COLUMN tarifas_base.tipo_tarifa IS
    'POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD (ADR-015)';
COMMENT ON COLUMN tarifas_base.modo_transporte IS
    'FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL';
COMMENT ON COLUMN tarifas_base.tipo_servicio IS
    'ESTANDAR, EXPRESS, PROGRAMADO, REFRIGERADO, PELIGROSO';
COMMENT ON COLUMN tarifas_base.precio_minimo IS
    'Precio mínimo aplicable aunque el cálculo resulte menor.
     NULL si no hay mínimo.';

CREATE INDEX idx_tarifas_empresa_id     ON tarifas_base(empresa_id);
CREATE INDEX idx_tarifas_activo         ON tarifas_base(activo);
CREATE INDEX idx_tarifas_empresa_activo ON tarifas_base(empresa_id, activo);
CREATE INDEX idx_tarifas_zona_origen    ON tarifas_base(zona_origen_id);
CREATE INDEX idx_tarifas_zona_destino   ON tarifas_base(zona_destino_id);
CREATE INDEX idx_tarifas_vigencia       ON tarifas_base(fecha_vigencia_desde,
    fecha_vigencia_hasta) WHERE activo = true;

ALTER TABLE tarifas_base ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_tarifas" ON tarifas_base
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_tarifas_fecha_modificacion
    BEFORE UPDATE ON tarifas_base
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `recargos_tarifa`

```sql
CREATE TABLE IF NOT EXISTS recargos_tarifa (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    tarifa_id       UUID NOT NULL REFERENCES tarifas_base(id) ON DELETE CASCADE,
    codigo_recargo  VARCHAR(50) NOT NULL,
    nombre          VARCHAR(200) NOT NULL,
    tipo_calculo    VARCHAR(30) NOT NULL DEFAULT 'PORCENTAJE',
    valor           NUMERIC(10,4) NOT NULL,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,
    CONSTRAINT uq_recargo_tarifa_codigo UNIQUE (tarifa_id, codigo_recargo)
);

COMMENT ON TABLE recargos_tarifa IS
    'Recargos aplicables a cada tarifa base: combustible, peaje, seguro,
     manipulación, urgencia, refrigeración, sobredimensión.';
COMMENT ON COLUMN recargos_tarifa.tipo_calculo IS
    'PORCENTAJE (% sobre tarifa base), MONTO_FIJO (valor absoluto en moneda)';
COMMENT ON COLUMN recargos_tarifa.codigo_recargo IS
    'COMBUSTIBLE, PEAJE, SEGURO, MANIPULACION, URGENCIA,
     REFRIGERACION, SOBREDIMENSION';

CREATE INDEX idx_recargos_tarifa_id    ON recargos_tarifa(tarifa_id);
CREATE INDEX idx_recargos_empresa_id   ON recargos_tarifa(empresa_id);

ALTER TABLE recargos_tarifa ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_recargos" ON recargos_tarifa
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_recargos_fecha_modificacion
    BEFORE UPDATE ON recargos_tarifa
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Datos iniciales (semillas del sistema)

```sql
-- Unidades de medida estándar para nuevos tenants
-- Se insertan vinculadas a la empresa raíz como plantillas

-- PESO
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Kilogramo', 'kg', 'PESO', 1.0, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Gramo', 'g', 'PESO', 0.001, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Tonelada métrica', 'ton', 'PESO', 1000.0, 'kg'),
('00000000-0000-0000-0000-000000000001', 'Libra', 'lb', 'PESO', 0.453592, 'kg')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- VOLUMEN
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Metro cúbico', 'm3', 'VOLUMEN', 1.0, 'm3'),
('00000000-0000-0000-0000-000000000001', 'Litro', 'L', 'VOLUMEN', 0.001, 'm3'),
('00000000-0000-0000-0000-000000000001', 'Pie cúbico', 'ft3', 'VOLUMEN', 0.0283168, 'm3')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- LONGITUD
INSERT INTO unidades_medida (empresa_id, nombre, simbolo, tipo,
    factor_conversion, unidad_base) VALUES
('00000000-0000-0000-0000-000000000001', 'Metro', 'm', 'LONGITUD', 1.0, 'm'),
('00000000-0000-0000-0000-000000000001', 'Centímetro', 'cm', 'LONGITUD', 0.01, 'm'),
('00000000-0000-0000-0000-000000000001', 'Pulgada', 'in', 'LONGITUD', 0.0254, 'm'),
('00000000-0000-0000-0000-000000000001', 'Pie', 'ft', 'LONGITUD', 0.3048, 'm')
ON CONFLICT (empresa_id, simbolo) DO NOTHING;

-- Tipos de embalaje estándar
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
```

---

## Historias de Usuario del Sprint

---

### HU-004 · OAuth 2.0 — Completar implementación (diferida Sprint 2)

**Como** usuario, **quiero** iniciar sesión con Google o Microsoft,
**para** acceder sin gestionar otra contraseña.

**Criterios de aceptación:**
- [ ] CA-01: POST /api/auth/oauth/callback recibe el token de Supabase y retorna JWT interno
- [ ] CA-02: Si el email ya existe en `usuarios` → vincular `supabase_user_id` y hacer login
- [ ] CA-03: Si el email es nuevo → verificar que la empresa existe por dominio o código de invitación
- [ ] CA-04: JWT retornado tiene los mismos claims que el login normal
- [ ] CA-05: Login OAuth registrado en `auditoria_actividad` con acción `LOGIN_OAUTH`
- [ ] CA-06: Botones Google/Microsoft en Login.cshtml funcionales (no solo deshabilitados)

**Implementación:**
```
Reemplazar el stub NotImplementedException en AuthService.OAuthCallbackAsync:
1. GET https://{supabase_ref}.supabase.co/auth/v1/user
   Authorization: Bearer {dto.SupabaseToken}
   → retorna email + provider + metadatos del usuario OAuth

2. IUsuarioRepository.GetByEmailGlobalAsync(email)
   → Si existe y ACTIVE → cargar permisos y emitir JWT interno
   → Si existe pero SUSPENDED/LOCKED → BusinessException con mensaje

3. Si NO existe → buscar empresa por dominio del email (opcional)
   → Si no se puede resolver la empresa → BusinessException:
     "No se encontró una empresa asociada a este email.
      Solicita una invitación al administrador."

4. Auditoría LOGIN_OAUTH con campo detalles: { provider: "google" }
```

**Estimación:** 5 pts

---

### HU-015 · Gestión de Ubicaciones y Geocodificación

**Como** operador de logística, **quiero** registrar y geocodificar
ubicaciones de clientes, almacenes y puntos de entrega, **para**
usarlas como origen y destino en los embarques.

**Criterios de aceptación:**
- [ ] CA-01: CRUD completo de ubicaciones con campos: nombre, tipo, dirección, país,
  ciudad, departamento, coordenadas, datos de contacto, horario, tiempo de servicio
- [ ] CA-02: Al guardar una ubicación con dirección → geocodificar automáticamente
  usando Nominatim (ADR-014) → guardar lat/lng + dirección normalizada
- [ ] CA-03: Si Nominatim no puede geocodificar → guardar la ubicación sin coordenadas
  y marcar `georeferenciada = false` con advertencia al usuario (no bloquear)
- [ ] CA-04: Visualización en mapa interactivo (Leaflet + CartoDB) de todas las
  ubicaciones del tenant con markers por tipo
- [ ] CA-05: El usuario puede ajustar manualmente el pin en el mapa si la
  geocodificación automática es inexacta
- [ ] CA-06: Importación masiva desde CSV con plantilla descargable
- [ ] CA-07: Búsqueda por nombre, código, tipo o ciudad
- [ ] CA-08: Tipos disponibles: ALMACEN, CLIENTE, PUERTO, AEROPUERTO, TERMINAL,
  CRUCE_FRONTERA, PUNTO_RECARGA, OTRO

**Endpoints API:**
```
GET    /api/ubicaciones                    → lista paginada con filtros
GET    /api/ubicaciones/{id}               → detalle
POST   /api/ubicaciones                    → crear + geocodificar
PUT    /api/ubicaciones/{id}               → actualizar + re-geocodificar si cambia dirección
DELETE /api/ubicaciones/{id}/deactivate    → desactivar
PUT    /api/ubicaciones/{id}/coordenadas   → actualizar coordenadas manualmente
POST   /api/ubicaciones/importar           → importación CSV masiva
GET    /api/ubicaciones/exportar           → exportar a CSV
GET    /api/ubicaciones/mapa               → todas las ubicaciones con lat/lng para el mapa
```

**Estimación:** 13 pts

---

### HU-016 · Gestión de Zonas de Entrega

**Como** planificador, **quiero** definir zonas geográficas de cobertura,
**para** asignar carriers y tarifas por zona de manera eficiente.

**Criterios de aceptación:**
- [ ] CA-01: CRUD de zonas con nombre, código y color identificador (hex)
- [ ] CA-02: Definición de zona por al menos uno de estos métodos:
  polígono GeoJSON en mapa, lista de códigos postales, lista de ciudades
  o lista de departamentos
- [ ] CA-03: Visualización de todas las zonas en mapa con su color y polígono
- [ ] CA-04: Asignar/desasignar ubicaciones a zonas manualmente
- [ ] CA-05: Verificar si una ubicación con coordenadas cae dentro de una
  zona (point-in-polygon para zonas tipo polígono)
- [ ] CA-06: Una zona no puede eliminarse si tiene tarifas activas asociadas
- [ ] CA-07: Exportar/importar zonas en GeoJSON

**Endpoints API:**
```
GET    /api/zonas
GET    /api/zonas/{id}
POST   /api/zonas
PUT    /api/zonas/{id}
DELETE /api/zonas/{id}/deactivate
GET    /api/zonas/{id}/ubicaciones     → ubicaciones en esta zona
POST   /api/zonas/{id}/ubicaciones     → asignar ubicaciones a la zona
DELETE /api/zonas/{id}/ubicaciones/{ubicacionId} → desasignar
POST   /api/zonas/verificar-pertenencia
       Body: { lat, lng } → retorna lista de zonas donde cae el punto
```

**Estimación:** 8 pts

---

### HU-017 · Catálogo de Tipos de Mercancía

**Como** operador, **quiero** registrar tipos de mercancía con sus
atributos de manejo, **para** clasificar correctamente la carga en
los embarques y garantizar el cumplimiento de restricciones.

**Criterios de aceptación:**
- [ ] CA-01: CRUD de tipos de mercancía con atributos completos
- [ ] CA-02: Clasificación HAZMAT: clase de peligrosidad (1-9) + código ONU
  cuando aplique. El sistema valida que la clase sea un valor válido de la
  normativa ONU (1-9, incluidas subclases como 1.1, 2.1, etc.)
- [ ] CA-03: Flags de manejo: frágil, perecedero, requiere refrigeración,
  sobredimensionado, requiere fumigación
- [ ] CA-04: Rango de temperatura para mercancías refrigeradas
  (temp_minima_c y temp_maxima_c — ambos requeridos si requiere_refrigeracion=true)
- [ ] CA-05: Clasificación arancelaria (HS Code) para operaciones de
  comercio exterior (opcional)
- [ ] CA-06: Importación masiva desde CSV
- [ ] CA-07: Al buscar mercancías peligrosas se resalta visualmente el tipo
  con badge danger y la clase HAZMAT

**Endpoints API:**
```
GET    /api/tipos-mercancia
GET    /api/tipos-mercancia/{id}
POST   /api/tipos-mercancia
PUT    /api/tipos-mercancia/{id}
DELETE /api/tipos-mercancia/{id}/deactivate
POST   /api/tipos-mercancia/importar
```

**Estimación:** 5 pts

---

### HU-018 · Catálogo de Unidades de Medida y Embalajes

**Como** operador, **quiero** gestionar unidades de medida y tipos de
embalaje, **para** estandarizar el registro de cargas en todo el sistema.

**Criterios de aceptación:**
- [ ] CA-01: CRUD de unidades de medida por tipo (PESO, VOLUMEN, LONGITUD)
  con factor de conversión a la unidad base
- [ ] CA-02: Al crear un tenant → copiar automáticamente las unidades
  estándar del sistema (kg, ton, m3, L, m, cm, lb, ft3) con posibilidad
  de agregar unidades personalizadas
- [ ] CA-03: CRUD de tipos de embalaje con capacidad máxima y flag apilable
- [ ] CA-04: Al crear un tenant → copiar automáticamente los embalajes
  estándar (PLT, CAJA, TAM, CTN20, CTN40, GRA, BOB)
- [ ] CA-05: El simulador de conversión: dado un valor en unidad A,
  muestra el equivalente en todas las demás unidades del mismo tipo
- [ ] CA-06: Una unidad no puede desactivarse si tiene tipos de mercancía
  que la referencian

**Endpoints API:**
```
GET    /api/unidades-medida
GET    /api/unidades-medida/{id}
POST   /api/unidades-medida
PUT    /api/unidades-medida/{id}
DELETE /api/unidades-medida/{id}/deactivate
GET    /api/unidades-medida/convertir?valor=100&desde=kg&hacia=lb

GET    /api/tipos-embalaje
GET    /api/tipos-embalaje/{id}
POST   /api/tipos-embalaje
PUT    /api/tipos-embalaje/{id}
DELETE /api/tipos-embalaje/{id}/deactivate
```

**Estimación:** 5 pts

---

### HU-019 · Catálogo de Clientes (Shippers)

**Como** ejecutivo de cuenta, **quiero** registrar clientes con su
información comercial, crediticia y de SLA, **para** asociarlos a
órdenes de transporte y monitorear el cumplimiento de compromisos.

**Criterios de aceptación:**
- [ ] CA-01: CRUD de clientes con: nombre, RUC/NIT, tipo, industria,
  dirección fiscal, crédito (días + límite), estado de crédito y SLA
- [ ] CA-02: Múltiples contactos por cliente con rol diferenciado:
  LOGISTICA, COMPRAS, FINANZAS, RECEPCION, GERENCIA
- [ ] CA-03: Asignación de ubicación de despacho por defecto (de las
  ubicaciones registradas en HU-015)
- [ ] CA-04: Tipos de cliente: REGULAR, VIP, OCASIONAL, CORPORATIVO, GOBIERNO
- [ ] CA-05: Estado de crédito: AL_DIA, EN_MORA, BLOQUEADO, SIN_CREDITO.
  Los clientes con estado BLOQUEADO muestran alerta visual en toda la UI
- [ ] CA-06: Adjuntar documentos al cliente (contrato, RUC, carta de compromiso)
  almacenados en Supabase Storage
- [ ] CA-07: Historial de órdenes del cliente (placeholder — se llena en Sprint 4)
- [ ] CA-08: Importación masiva desde CSV con validación de RUC único por empresa
- [ ] CA-09: Exportar directorio de clientes a Excel

**Endpoints API:**
```
GET    /api/clientes                    → lista paginada con filtros
GET    /api/clientes/{id}               → detalle con contactos
POST   /api/clientes                    → crear + contactos
PUT    /api/clientes/{id}               → actualizar
DELETE /api/clientes/{id}/deactivate    → desactivar
PUT    /api/clientes/{id}/estado-credito → cambiar estado
GET    /api/clientes/{id}/contactos     → listar contactos
POST   /api/clientes/{id}/contactos     → agregar contacto
PUT    /api/clientes/{id}/contactos/{cid} → actualizar contacto
DELETE /api/clientes/{id}/contactos/{cid} → desactivar contacto
POST   /api/clientes/importar           → importar CSV
GET    /api/clientes/exportar           → exportar Excel
```

**Estimación:** 13 pts

---

### HU-020 · Catálogo de Tarifas Base

**Como** gerente de operaciones, **quiero** definir tarifas de transporte
por zona, modo y tipo de servicio, **para** calcular costos automáticamente
al planificar embarques.

**Criterios de aceptación:**
- [ ] CA-01: CRUD de tarifas con: zonas origen/destino, modo de transporte,
  tipo de servicio, tipo de tarifa (POR_KG, POR_M3, POR_KM, FIJO_VIAJE, POR_UNIDAD),
  precio unitario y moneda
- [ ] CA-02: Recargos configurables por tarifa: COMBUSTIBLE (%), PEAJE (fijo),
  SEGURO (%), MANIPULACION (fijo), URGENCIA (%), REFRIGERACION (fijo),
  SOBREDIMENSION (%)
- [ ] CA-03: Vigencia de tarifas: fecha de inicio requerida, fecha de fin opcional.
  Al actualizar una tarifa → crear nueva versión con nueva vigencia
  (el registro anterior queda histórico con su fecha_vigencia_hasta)
- [ ] CA-04: Simulador de costo: dado origen, destino, modo, peso y volumen →
  calcular el costo estimado aplicando la tarifa vigente y todos sus recargos
- [ ] CA-05: Alerta cuando no existe tarifa vigente para una combinación
  origen-destino-modo (necesaria al crear órdenes en Sprint 4)
- [ ] CA-06: Una tarifa con fecha_vigencia_hasta en el pasado se muestra
  como "Vencida" con badge neutral (no se puede editar, solo ver)
- [ ] CA-07: Historial de cambios de tarifa por zona (auditoría de precios)

**Endpoints API:**
```
GET    /api/tarifas                     → lista paginada con filtros
GET    /api/tarifas/{id}                → detalle con recargos
GET    /api/tarifas/vigente             → tarifa vigente por zona/modo/fecha
       QueryParams: zonaOrigenId, zonaDestinoId, modo, fecha
POST   /api/tarifas                     → crear
PUT    /api/tarifas/{id}                → actualizar (crea nueva versión)
DELETE /api/tarifas/{id}/deactivate     → desactivar
GET    /api/tarifas/{id}/recargos       → listar recargos
POST   /api/tarifas/{id}/recargos       → agregar recargo
PUT    /api/tarifas/{id}/recargos/{rid} → actualizar recargo
DELETE /api/tarifas/{id}/recargos/{rid}/deactivate
POST   /api/tarifas/simular             → simular costo
       Body: { zonaOrigenId, zonaDestinoId, modo, tipoServicio,
               pesoKg, volumenM3, distanciaKm, fecha }
       Response: { tarifaAplicada, costoBase, recargos[], costoTotal }
```

**Estimación:** 13 pts

---

## Estructura de Archivos a Crear en el Sprint 3

### @Arquitecto entrega:

```
Freiroute.Entity/
├── Ubicacion.cs
├── ZonaEntrega.cs
├── UbicacionZona.cs
├── TipoMercancia.cs
├── UnidadMedida.cs
├── TipoEmbalaje.cs
├── Cliente.cs
├── ContactoCliente.cs
├── TarifaBase.cs
└── RecargoTarifa.cs

Freiroute.DTO/
├── Geo/
│   └── GeocodingResultDto.cs
├── Ubicacion/
│   ├── UbicacionRequestDto.cs
│   ├── UbicacionResponseDto.cs
│   ├── UbicacionMapaDto.cs         ← solo id, nombre, tipo, lat, lng
│   └── ActualizarCoordenadasDto.cs
├── Zona/
│   ├── ZonaRequestDto.cs
│   └── ZonaResponseDto.cs
├── Mercancia/
│   ├── TipoMercanciaRequestDto.cs
│   └── TipoMercanciaResponseDto.cs
├── Unidad/
│   ├── UnidadMedidaRequestDto.cs
│   ├── UnidadMedidaResponseDto.cs
│   ├── TipoEmbalajeRequestDto.cs
│   └── TipoEmbalajeResponseDto.cs
├── Cliente/
│   ├── ClienteRequestDto.cs
│   ├── ClienteResponseDto.cs
│   ├── ContactoClienteRequestDto.cs
│   └── ContactoClienteResponseDto.cs
└── Tarifa/
    ├── TarifaBaseRequestDto.cs
    ├── TarifaBaseResponseDto.cs
    ├── RecargoTarifaRequestDto.cs
    ├── RecargoTarifaResponseDto.cs
    └── SimularCostoRequestDto.cs
    └── SimularCostoResponseDto.cs

Freiroute.DAL/Interfaces/
├── IUbicacionRepository.cs
├── IZonaEntregaRepository.cs
├── ITipoMercanciaRepository.cs
├── IUnidadMedidaRepository.cs
├── ITipoEmbalajeRepository.cs
├── IClienteRepository.cs
└── ITarifaBaseRepository.cs

Freiroute.BLL/Interfaces/
├── IUbicacionService.cs
├── IZonaEntregaService.cs
├── ITipoMercanciaService.cs
├── IUnidadMedidaService.cs
├── ITipoEmbalajeService.cs
├── IClienteService.cs
├── ITarifaBaseService.cs
└── IGeocodingService.cs
```

### @IngenieroDatos entrega:

```
supabase/migrations/
├── 20260301000001_tabla_ubicaciones.sql
├── 20260301000002_tabla_zonas_entrega.sql
├── 20260301000003_tabla_ubicacion_zonas.sql
├── 20260301000004_tabla_tipos_mercancia.sql
├── 20260301000005_tabla_unidades_medida.sql
├── 20260301000006_tabla_tipos_embalaje.sql
├── 20260301000007_tabla_clientes.sql
├── 20260301000008_tabla_contactos_cliente.sql
├── 20260301000009_tabla_tarifas_base.sql
├── 20260301000010_tabla_recargos_tarifa.sql
└── 20260301000011_datos_iniciales_catalogo.sql

Freiroute.DAL/Repositories/
├── UbicacionRepository.cs
├── ZonaEntregaRepository.cs
├── TipoMercanciaRepository.cs
├── UnidadMedidaRepository.cs
├── TipoEmbalajeRepository.cs
├── ClienteRepository.cs
└── TarifaBaseRepository.cs

Freiroute.Utility/Security/
└── (AesGcmEncryptor.cs ya existe)

Freiroute.Utility/Constants/
├── TipoUbicacion.cs     ← ALMACEN, CLIENTE, PUERTO, AEROPUERTO...
├── TipoCliente.cs       ← REGULAR, VIP, OCASIONAL...
├── EstadoCredito.cs     ← AL_DIA, EN_MORA, BLOQUEADO...
├── TipoTarifa.cs        ← POR_KG, POR_M3, POR_KM, FIJO_VIAJE...
├── CodigoRecargo.cs     ← COMBUSTIBLE, PEAJE, SEGURO...
└── ModoTransporte.cs    ← FTL, LTL, AEREO, MARITIMO...
```

### @BackendDev entrega:

```
Freiroute.BLL/Services/
├── UbicacionService.cs      ← incluye geocodificación automática
├── ZonaEntregaService.cs
├── TipoMercanciaService.cs
├── UnidadMedidaService.cs
├── TipoEmbalajeService.cs
├── ClienteService.cs
├── TarifaBaseService.cs
└── NominatimGeocodingService.cs  ← implementa IGeocodingService

Freiroute.BLL/Validators/
├── UbicacionValidator.cs
├── ZonaEntregaValidator.cs
├── TipoMercanciaValidator.cs
├── UnidadMedidaValidator.cs
├── ClienteValidator.cs
└── TarifaBaseValidator.cs

Freiroute.API/Controllers/
├── UbicacionesController.cs
├── ZonasController.cs
├── TiposMercanciaController.cs
├── UnidadesMedidaController.cs
├── TiposEmbalajeController.cs
├── ClientesController.cs
└── TarifasController.cs

Actualizar AuthService.cs:
└── OAuthCallbackAsync → implementación real (HU-004)
```

### @FrontendDev entrega:

```
Freiroute.Aplicacion/Areas/Tenant/Views/
├── Ubicaciones/
│   ├── Index.cshtml     ← tabla + mapa Leaflet
│   └── Create.cshtml
│   └── Edit.cshtml
├── Zonas/
│   ├── Index.cshtml     ← mapa con polígonos coloreados
│   └── Create.cshtml
├── Mercancia/
│   ├── Index.cshtml
│   └── Create.cshtml
├── Unidades/
│   └── Index.cshtml     ← tabs: Unidades de medida | Embalajes
├── Clientes/
│   ├── Index.cshtml
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   └── Detalle.cshtml   ← con contactos y ubicación
└── Tarifas/
    ├── Index.cshtml
    ├── Create.cshtml
    └── Simulador.cshtml ← form de simulación de costo

Actualizar _Layout.cshtml:
└── Agregar sección "Catálogos" en el sidebar del tenant:
    Ubicaciones · Zonas · Mercancías · Clientes · Tarifas
```

---

## Definición de Done (DoD) — Sprint 3

- [ ] `supabase db push` — 11 migraciones aplicadas
- [ ] `supabase db diff` — vacío
- [ ] 10 tablas nuevas con RLS, índices y triggers
- [ ] OAuth funcional (HU-004) — botones activados en Login.cshtml
- [ ] Geocodificación automática al registrar ubicaciones (Nominatim)
- [ ] Mapa Leaflet con markers de ubicaciones por tipo
- [ ] Zonas visibles en mapa con colores diferenciados
- [ ] Importación CSV de ubicaciones y clientes funcional
- [ ] Simulador de tarifas retorna costo desglosado
- [ ] Un cliente puede tener múltiples contactos con rol diferenciado
- [ ] Unidades y embalajes estándar copiados al crear un tenant nuevo
- [ ] `dotnet build` sin warnings
- [ ] `dotnet test` sin fallos
- [ ] Cobertura BLL ≥ 80%
- [ ] Cobertura API ≥ 60%
- [ ] Swagger documentado en todos los endpoints nuevos
- [ ] UI con Design System Freiroute — sección Catálogos en sidebar
- [ ] PR revisado y aprobado por @PM

---

## Convenciones específicas del Sprint 3

### Geocodificación (NominatimGeocodingService)

```csharp
// Rate limit: 1 request/segundo (ADR-014)
// User-Agent: "Freiroute-TMS/1.0 (api@freiroute.com)"
// Si falla → georeferenciada = false, NO lanzar excepción
// Si tiene éxito → actualizar lat, lng, direccion_normalizada,
//                  georeferenciada = true

// UbicacionService.CreateAsync:
var entidad = MapToEntity(dto, empresaId);
var id = await _ubicacionRepository.CreateAsync(entidad);

// Geocodificar en background (no bloquear la respuesta)
_ = Task.Run(async () => {
    try {
        var geo = await _geocodingService.GeocodeAsync(
            dto.Direccion, dto.Ciudad, dto.Pais);
        if (geo != null) {
            await _ubicacionRepository.ActualizarCoordenadasAsync(
                id, empresaId, geo.Latitud, geo.Longitud,
                geo.DireccionNormalizada);
        }
    } catch (Exception ex) {
        _logger.LogWarning(ex,
            "Geocodificación fallida para ubicación {Id}", id);
    }
});
```

### Versionado de tarifas

```csharp
// TarifaBaseService.UpdateAsync:
// NO actualizar la tarifa existente — crear nueva versión:

// 1. Cerrar la tarifa actual:
await _tarifaRepository.CerrarVigenciaAsync(
    id, empresaId, DateTime.Today.AddDays(-1));
// fecha_vigencia_hasta = ayer

// 2. Crear nueva versión con los datos actualizados:
var nuevaTarifa = new TarifaBase
{
    // ... datos del dto
    FechaVigenciaDesde = DateTime.Today,
    FechaVigenciaHasta = null  // vigente hasta nuevo aviso
};
var nuevaId = await _tarifaRepository.CreateAsync(nuevaTarifa);

// 3. Copiar los recargos de la tarifa anterior a la nueva:
var recargos = await _tarifaRepository.GetRecargosAsync(id, empresaId);
foreach (var recargo in recargos)
{
    recargo.TarifaId = nuevaId;
    await _tarifaRepository.CreateRecargoAsync(recargo);
}
```

### Copiar catálogos estándar al crear tenant

Agregar a `EmpresaService.CreateAsync` (después de crear la suscripción):

```csharp
// Copiar unidades de medida estándar de la empresa raíz al nuevo tenant
await CopiarUnidadesEstandarAsync(nuevaEmpresaId);
await CopiarEmbalajesEstandarAsync(nuevaEmpresaId);

// CopiarUnidadesEstandarAsync:
// SELECT * FROM unidades_medida WHERE empresa_id = '00000000-...-0001'
// INSERT INTO unidades_medida (empresa_id = nuevaEmpresaId, resto de campos)
```

---

*Spec Sprint 3 — Freiroute TMS*
*Versión: 1.0 | Fecha: 2026*
*Próximo: Sprint 4-5 — EP-04 Order Management*
