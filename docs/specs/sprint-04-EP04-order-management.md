# Spec: Sprint 4 — EP-04 Order Management

**Sprint:** 04
**Épica:** EP-04 — Order Management
**Historias:** HU-021 · HU-022 · HU-023 · HU-024 · HU-025 · HU-026 · HU-027
**Story Points:** 60 pts
**Objetivo del Sprint:** Implementar el módulo central de órdenes de transporte:
creación manual, importación masiva, recepción por API externa, máquina de estados,
consolidación, división y plantillas recurrentes. Es el prerrequisito directo de
EP-06 Shipment Planning (Sprint 7).
**ADRs aplicables:** ADR-019 · ADR-020 · ADR-003 · ADR-013 · ADR-017

---

## Dependencias

```
Sprint 1 → Auth, tenants, perfiles, permisos, auditoría
Sprint 2 → Configuración del tenant (prefijo_orden, moneda_principal)
Sprint 3 → clientes, ubicaciones, tipos_mercancia,
           unidades_medida, tipos_embalaje, tarifas_base
Sprint 4 (este) → BASE para Sprint 7 EP-06 Shipment Planning

Deuda técnica a resolver en este sprint:
  G-03 → @FrontendDev: sugerencia de descuento anual en Planes
  G-07 → @BackendDev:  enriquecer UbicacionResponseDto con campos faltantes
```

---

## Orden de Implementación por Capa

```
1. @Arquitecto     → Constantes Utility + Entities + DTOs + Interfaces
2. @IngenieroDatos → Migraciones SQL + Repositorios Dapper
3. @BackendDev     → BLL Services + API Controllers + Background Job
                     + Fix G-03 + Fix G-07
4. @QA             → Tests BLL ≥80% + API ≥60%
5. @FrontendDev    → Vistas Razor del módulo órdenes
```

---

## Tablas de Base de Datos del Sprint 4

### Diagrama de relaciones

```
empresas
  │
  ├── contadores_orden  (por empresa_id + año — ADR-020)
  │
  └── ordenes
        │── clientes          (Sprint 3)
        │── ubicaciones       (Sprint 3, origen + destino)
        │── tipos_mercancia   (Sprint 3)
        │── unidades_medida   (Sprint 3)
        │── tipos_embalaje    (Sprint 3)
        │── tarifas_base      (Sprint 3)
        │── shipments         (esqueleto mínimo — se expande en Sprint 7)
        │
        ├── lineas_orden
        ├── historial_estados_orden
        ├── documentos_orden  (esqueleto — se expande en Sprint 11)
        │
        ├── plantillas_orden
        ├── importaciones_orden
        └── api_keys_tenant
```

---

### Tabla: `contadores_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_contadores_orden.sql

-- ============================================================
-- TABLA: contadores_orden
-- Descripción: Contador atómico de consecutivo de órdenes por
--              tenant y año. Soporte del ADR-020.
-- HU relacionada: HU-021, HU-024
-- ============================================================

CREATE TABLE IF NOT EXISTS contadores_orden (
    empresa_id         UUID     NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    anio               SMALLINT NOT NULL,
    ultimo_consecutivo INT      NOT NULL DEFAULT 0,
    PRIMARY KEY (empresa_id, anio)
);

COMMENT ON TABLE contadores_orden IS
    'Contador de consecutivo de órdenes por tenant y año.
     El upsert atómico de generar_numero_orden() garantiza unicidad sin race conditions.';
COMMENT ON COLUMN contadores_orden.empresa_id IS
    'Tenant dueño del contador';
COMMENT ON COLUMN contadores_orden.anio IS
    'Año del consecutivo — reinicia a 1 al cambiar de año';
COMMENT ON COLUMN contadores_orden.ultimo_consecutivo IS
    'Último número asignado en ese año para ese tenant';
```

---

### Función: `generar_numero_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_funcion_generar_numero_orden.sql

-- ============================================================
-- FUNCIÓN: generar_numero_orden
-- Descripción: Genera el número de orden legible para el tenant.
--              Formato: {PREFIJO}-{YYYY}-{NNNNN}
-- ADR relacionado: ADR-020
-- ============================================================

CREATE OR REPLACE FUNCTION generar_numero_orden(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql AS $$
DECLARE
    v_consecutivo INT;
    v_numero      TEXT;
BEGIN
    INSERT INTO contadores_orden (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = contadores_orden.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    v_numero := UPPER(TRIM(p_prefijo))
             || '-' || p_anio::TEXT
             || '-' || LPAD(v_consecutivo::TEXT, 5, '0');

    RETURN v_numero;
END;
$$;

COMMENT ON FUNCTION generar_numero_orden IS
    'Genera el número de orden para el tenant. Llamar solo en DRAFT→CONFIRMED.';
```

---

### Tabla: `ordenes`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_ordenes.sql

-- ============================================================
-- TABLA: ordenes
-- Descripción: Órdenes de transporte — entidad central de EP-04.
--              Nace en DRAFT y cierra como CLOSED (ver ADR-019).
-- HU relacionadas: HU-021 · HU-022 · HU-023 · HU-024 · HU-025 · HU-026
-- ============================================================

CREATE TABLE IF NOT EXISTS ordenes (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id               UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,

    -- Número legible — NULL hasta la transición DRAFT→CONFIRMED (ADR-020)
    numero_orden             VARCHAR(30),

    -- Relaciones con maestros (Sprint 3)
    cliente_id               UUID NOT NULL REFERENCES clientes(id),
    origen_id                UUID NOT NULL REFERENCES ubicaciones(id),
    destino_id               UUID NOT NULL REFERENCES ubicaciones(id),
    tipo_mercancia_id        UUID NOT NULL REFERENCES tipos_mercancia(id),
    unidad_medida_id         UUID NOT NULL REFERENCES unidades_medida(id),
    tipo_embalaje_id         UUID REFERENCES tipos_embalaje(id),
    tarifa_id                UUID REFERENCES tarifas_base(id),

    -- Relación con shipment (se activa en HU-025 y se expande en Sprint 7)
    shipment_id              UUID REFERENCES shipments(id),

    -- Datos de carga
    cantidad                 NUMERIC(12,3) NOT NULL,
    peso_kg                  NUMERIC(12,3) NOT NULL,
    volumen_m3               NUMERIC(12,3),
    valor_declarado          NUMERIC(15,2),

    -- Datos de servicio
    modo_transporte          VARCHAR(20)  NOT NULL DEFAULT 'TERRESTRE',
    nivel_servicio           VARCHAR(20)  NOT NULL DEFAULT 'ESTANDAR',
    prioridad                VARCHAR(20)  NOT NULL DEFAULT 'NORMAL',

    -- Fechas operativas
    fecha_pickup_solicitada  DATE,
    fecha_entrega_requerida  DATE,
    fecha_confirmacion       TIMESTAMPTZ,

    -- Referencia y comunicación
    referencia_cliente       VARCHAR(100),
    instrucciones            TEXT,

    -- Estado FSM (ADR-019)
    estado                   VARCHAR(30) NOT NULL DEFAULT 'DRAFT',

    -- Split (HU-026)
    es_split                 BOOLEAN NOT NULL DEFAULT false,
    orden_origen_id          UUID REFERENCES ordenes(id),

    -- Canal de ingreso (HU-022 / HU-023 / HU-027)
    origen_creacion          VARCHAR(20) NOT NULL DEFAULT 'MANUAL',
    api_key_id               UUID REFERENCES api_keys_tenant(id),

    -- Auditoría estándar
    activo                   BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion           TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion       TIMESTAMPTZ NOT NULL DEFAULT now(),
    creado_por               UUID REFERENCES usuarios(id),
    modificado_por           UUID REFERENCES usuarios(id)
);

COMMENT ON TABLE ordenes IS
    'Órdenes de transporte — entidad central de EP-04 Order Management.
     Cada orden sigue la FSM definida en ADR-019.';
COMMENT ON COLUMN ordenes.numero_orden IS
    'Número legible generado al confirmar: {PREFIJO}-{YYYY}-{NNNNN}.
     NULL mientras la orden está en DRAFT (ver ADR-020).';
COMMENT ON COLUMN ordenes.estado IS
    'Estado FSM según ADR-019:
     DRAFT | CONFIRMED | ASSIGNED | PICKUP_SCHEDULED | IN_TRANSIT |
     DELIVERED | INVOICED | CLOSED | CANCELLED | ON_HOLD |
     FAILED_DELIVERY | PARTIALLY_SPLIT';
COMMENT ON COLUMN ordenes.modo_transporte IS
    'TERRESTRE | AEREO | MARITIMO | FERROVIARIO | INTERMODAL';
COMMENT ON COLUMN ordenes.nivel_servicio IS
    'ESTANDAR | EXPRESS | PROGRAMADO';
COMMENT ON COLUMN ordenes.prioridad IS
    'CRITICO | ALTO | NORMAL | BAJO';
COMMENT ON COLUMN ordenes.origen_creacion IS
    'Canal de ingreso: MANUAL (UI) | CSV (importación) | API (EDI/REST) | RECURRENTE (plantilla)';
COMMENT ON COLUMN ordenes.es_split IS
    'true si esta orden es resultado de dividir una orden padre (HU-026)';
COMMENT ON COLUMN ordenes.orden_origen_id IS
    'Referencia a la orden padre cuando es_split = true';

-- Índices
CREATE UNIQUE INDEX idx_ordenes_numero_empresa
    ON ordenes(empresa_id, numero_orden)
    WHERE numero_orden IS NOT NULL;
CREATE INDEX idx_ordenes_empresa_id     ON ordenes(empresa_id);
CREATE INDEX idx_ordenes_activo         ON ordenes(activo);
CREATE INDEX idx_ordenes_estado         ON ordenes(empresa_id, estado);
CREATE INDEX idx_ordenes_cliente        ON ordenes(empresa_id, cliente_id);
CREATE INDEX idx_ordenes_shipment       ON ordenes(shipment_id) WHERE shipment_id IS NOT NULL;
CREATE INDEX idx_ordenes_fecha_pickup   ON ordenes(empresa_id, fecha_pickup_solicitada);
CREATE INDEX idx_ordenes_fecha_entrega  ON ordenes(empresa_id, fecha_entrega_requerida);
CREATE INDEX idx_ordenes_prioridad      ON ordenes(empresa_id, prioridad, estado);

-- RLS
ALTER TABLE ordenes ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ordenes" ON ordenes
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- Trigger
CREATE TRIGGER trg_ordenes_fecha_modificacion
    BEFORE UPDATE ON ordenes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `lineas_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_lineas_orden.sql

-- ============================================================
-- TABLA: lineas_orden
-- Descripción: Ítems de detalle de mercancía por orden.
-- HU relacionada: HU-021
-- ============================================================

CREATE TABLE IF NOT EXISTS lineas_orden (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id       UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id         UUID NOT NULL REFERENCES ordenes(id) ON DELETE CASCADE,
    descripcion      VARCHAR(255) NOT NULL,
    cantidad         NUMERIC(12,3) NOT NULL,
    unidad_medida_id UUID REFERENCES unidades_medida(id),
    peso_kg          NUMERIC(12,3),
    volumen_m3       NUMERIC(12,3),
    valor_unitario   NUMERIC(15,2),
    numero_linea     SMALLINT NOT NULL DEFAULT 1,
    activo           BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion   TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE lineas_orden IS
    'Líneas de detalle de mercancía por orden de transporte.
     El ON DELETE CASCADE permite eliminar las líneas al desactivar la orden.';
COMMENT ON COLUMN lineas_orden.numero_linea IS
    'Número de línea dentro de la orden, para ordenamiento en pantalla';

CREATE INDEX idx_lineas_orden_empresa  ON lineas_orden(empresa_id);
CREATE INDEX idx_lineas_orden_orden_id ON lineas_orden(orden_id);

ALTER TABLE lineas_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_lineas_orden" ON lineas_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_lineas_orden_fecha_modificacion
    BEFORE UPDATE ON lineas_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `historial_estados_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_historial_estados_orden.sql

-- ============================================================
-- TABLA: historial_estados_orden
-- Descripción: Auditoría de todas las transiciones de estado
--              de una orden. Registro inmutable (ADR-019).
-- HU relacionada: HU-024
-- ============================================================

CREATE TABLE IF NOT EXISTS historial_estados_orden (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    orden_id        UUID NOT NULL REFERENCES ordenes(id),
    estado_anterior VARCHAR(30),
    estado_nuevo    VARCHAR(30) NOT NULL,
    motivo          TEXT,
    usuario_id      UUID REFERENCES usuarios(id),
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE historial_estados_orden IS
    'Auditoría inmutable de todas las transiciones de estado de una orden.
     Cada cambio queda registrado con usuario, timestamp y motivo opcional.';
COMMENT ON COLUMN historial_estados_orden.estado_anterior IS
    'NULL si es la inserción inicial del estado DRAFT';

CREATE INDEX idx_historial_orden_empresa  ON historial_estados_orden(empresa_id);
CREATE INDEX idx_historial_orden_orden_id ON historial_estados_orden(orden_id);

ALTER TABLE historial_estados_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_historial_orden" ON historial_estados_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_historial_estados_orden_fecha_mod
    BEFORE UPDATE ON historial_estados_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `shipments` (esqueleto mínimo)

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_shipments_base.sql

-- ============================================================
-- TABLA: shipments (esqueleto mínimo para FK desde ordenes)
-- Descripción: Tabla base de embarques. Se expande en Sprint 7
--              (EP-06 Shipment Planning). Aquí solo se crea
--              para que ordenes.shipment_id tenga FK válida.
-- HU relacionada: HU-025
-- ============================================================

CREATE TABLE IF NOT EXISTS shipments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    numero_shipment VARCHAR(30),
    estado          VARCHAR(30) NOT NULL DEFAULT 'PLANNED',
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE shipments IS
    'Embarques de transporte. Esqueleto mínimo creado en Sprint 4
     para soportar la FK ordenes.shipment_id. Módulo completo en Sprint 7.';

CREATE INDEX idx_shipments_empresa_id ON shipments(empresa_id);
CREATE INDEX idx_shipments_activo     ON shipments(activo);

ALTER TABLE shipments ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_shipments" ON shipments
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_shipments_fecha_modificacion
    BEFORE UPDATE ON shipments
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `plantillas_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_plantillas_orden.sql

-- ============================================================
-- TABLA: plantillas_orden
-- Descripción: Plantillas para creación rápida y órdenes
--              recurrentes programadas.
-- HU relacionada: HU-027
-- ============================================================

CREATE TABLE IF NOT EXISTS plantillas_orden (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre                  VARCHAR(100) NOT NULL,
    descripcion             TEXT,

    -- Snapshot JSON del OrdenRequestDto al guardar la plantilla
    datos_orden             JSONB NOT NULL DEFAULT '{}',

    -- Recurrencia
    es_recurrente           BOOLEAN NOT NULL DEFAULT false,
    frecuencia_recurrencia  VARCHAR(20),
    proxima_ejecucion       DATE,

    -- Control
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion      TIMESTAMPTZ NOT NULL DEFAULT now(),
    creado_por              UUID REFERENCES usuarios(id)
);

COMMENT ON TABLE plantillas_orden IS
    'Plantillas para crear órdenes rápidamente y para órdenes recurrentes.
     El background job RecurrenciaOrdenesJob las procesa cada día a las 00:05.';
COMMENT ON COLUMN plantillas_orden.datos_orden IS
    'Snapshot JSON de los campos de OrdenRequestDto al momento de guardar.
     Se usa para precagar el formulario o para crear órdenes automáticas.';
COMMENT ON COLUMN plantillas_orden.frecuencia_recurrencia IS
    'DIARIA | SEMANAL | QUINCENAL | MENSUAL. NULL si no es recurrente.';
COMMENT ON COLUMN plantillas_orden.proxima_ejecucion IS
    'Fecha de la próxima ejecución automática. El job la actualiza tras crear la orden.';

CREATE INDEX idx_plantillas_empresa        ON plantillas_orden(empresa_id);
CREATE INDEX idx_plantillas_empresa_activo ON plantillas_orden(empresa_id, activo);
CREATE INDEX idx_plantillas_recurrentes
    ON plantillas_orden(empresa_id, proxima_ejecucion)
    WHERE es_recurrente = true AND activo = true;

ALTER TABLE plantillas_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_plantillas_orden" ON plantillas_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_plantillas_orden_fecha_modificacion
    BEFORE UPDATE ON plantillas_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `importaciones_orden`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_importaciones_orden.sql

-- ============================================================
-- TABLA: importaciones_orden
-- Descripción: Historial de importaciones CSV masivas de órdenes.
--              Patrón fail-soft (ADR-017): filas válidas se crean,
--              filas inválidas se reportan sin detener el proceso.
-- HU relacionada: HU-022
-- ============================================================

CREATE TABLE IF NOT EXISTS importaciones_orden (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    usuario_id      UUID NOT NULL REFERENCES usuarios(id),
    nombre_archivo  VARCHAR(255) NOT NULL,
    total_filas     INT NOT NULL DEFAULT 0,
    filas_ok        INT NOT NULL DEFAULT 0,
    filas_error     INT NOT NULL DEFAULT 0,

    -- JSON: [{fila: N, campo: "...", error: "..."}]
    detalle_errores JSONB,

    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE importaciones_orden IS
    'Historial de importaciones CSV de órdenes. Fail-soft: las filas válidas
     se crean y las inválidas quedan en detalle_errores.';
COMMENT ON COLUMN importaciones_orden.detalle_errores IS
    'Array JSON con el detalle de errores por fila: [{fila: N, campo, error}]';

CREATE INDEX idx_importaciones_empresa    ON importaciones_orden(empresa_id);
CREATE INDEX idx_importaciones_usuario    ON importaciones_orden(usuario_id);
CREATE INDEX idx_importaciones_fecha      ON importaciones_orden(empresa_id, fecha_creacion DESC);

ALTER TABLE importaciones_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_importaciones_orden" ON importaciones_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_importaciones_orden_fecha_modificacion
    BEFORE UPDATE ON importaciones_orden
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

### Tabla: `api_keys_tenant`

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_api_keys_tenant.sql

-- ============================================================
-- TABLA: api_keys_tenant
-- Descripción: API Keys para integración REST externa por tenant.
--              La clave se almacena hasheada con bcrypt.
-- HU relacionada: HU-023
-- ============================================================

CREATE TABLE IF NOT EXISTS api_keys_tenant (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre          VARCHAR(100) NOT NULL,

    -- La clave nunca se guarda en texto plano — solo el hash bcrypt
    clave_hash      TEXT NOT NULL,

    activo          BOOLEAN NOT NULL DEFAULT true,
    ultimo_uso      TIMESTAMPTZ,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE api_keys_tenant IS
    'API Keys para integración REST externa por tenant (HU-023).
     El valor crudo es visible solo una vez al crear la key.
     Se almacena únicamente el hash bcrypt.';
COMMENT ON COLUMN api_keys_tenant.clave_hash IS
    'Hash bcrypt de la API key. Formato visible al cliente: frk_live_{uuid}';
COMMENT ON COLUMN api_keys_tenant.ultimo_uso IS
    'Timestamp del último request autenticado exitoso con esta key';

CREATE INDEX idx_api_keys_empresa    ON api_keys_tenant(empresa_id);
CREATE INDEX idx_api_keys_activo     ON api_keys_tenant(empresa_id, activo);

ALTER TABLE api_keys_tenant ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_api_keys" ON api_keys_tenant
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_api_keys_fecha_modificacion
    BEFORE UPDATE ON api_keys_tenant
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

---

## Historias de Usuario

---

### HU-021 · Creación manual de orden de transporte

**Como** operador de logística, **quiero** crear órdenes de transporte manualmente, **para** registrar solicitudes de envío de mis clientes con todos los datos operativos.

**Criterios de aceptación:**
- [ ] CA-01: `POST /api/ordenes` retorna 201 con `Location` header y el DTO de la orden creada
- [ ] CA-02: La orden creada tiene estado `DRAFT` y `numero_orden = NULL`
- [ ] CA-03: FluentValidation rechaza `cliente_id`, `origen_id`, `destino_id`, `tipo_mercancia_id`, `unidad_medida_id` vacíos → 422
- [ ] CA-04: `peso_kg ≤ 0` retorna 422 "El peso debe ser mayor a cero"
- [ ] CA-05: `cantidad ≤ 0` retorna 422 "La cantidad debe ser mayor a cero"
- [ ] CA-06: Si `fecha_entrega_requerida` < `fecha_pickup_solicitada` → 422 "La fecha de entrega debe ser posterior a la fecha de recogida"
- [ ] CA-07: `origen_id == destino_id` → 422 "El origen y el destino no pueden ser la misma ubicación"
- [ ] CA-08: `empresa_id` se extrae del JWT — no se acepta en el body
- [ ] CA-09: `modo_transporte` debe ser uno de: `TERRESTRE | AEREO | MARITIMO | FERROVIARIO | INTERMODAL` → 422 si no
- [ ] CA-10: `nivel_servicio` debe ser uno de: `ESTANDAR | EXPRESS | PROGRAMADO` → 422 si no
- [ ] CA-11: `prioridad` debe ser uno de: `CRITICO | ALTO | NORMAL | BAJO` → 422 si no
- [ ] CA-12: `PUT /api/ordenes/{id}` solo permitido en estado `DRAFT` o `CONFIRMED` → 422 si está en otro estado
- [ ] CA-13: Soft-deactivate solo en estado `DRAFT` → 422 si está en cualquier otro estado
- [ ] CA-14: `GET /api/ordenes` pagina 20 registros por página con `PagedResult<OrdenListDto>`
- [ ] CA-15: La orden solo es visible para su `empresa_id` (RLS verificado en test de integración)
- [ ] CA-16: Auditoría `auditoria_actividad` registra `CREATE` al crear y `UPDATE` al editar
- [ ] CA-17: La vista `/tenant/ordenes/create` muestra selects de cliente, origen, destino, tipo de mercancía

**Endpoints API:**
```
GET    /api/ordenes                    → PagedResult<OrdenListDto>
GET    /api/ordenes/{id}               → OrdenResponseDto
POST   /api/ordenes                    → 201 OrdenResponseDto
PUT    /api/ordenes/{id}               → 200 OrdenResponseDto
DELETE /api/ordenes/{id}/deactivate    → 200 (soft-delete — solo DRAFT)
```

**Módulo de permiso:** `ordenes`
**Estimación:** 13 pts | **Asignado a:** @Arquitecto → @IngenieroDatos → @BackendDev → @FrontendDev → @QA

---

### HU-022 · Importación de órdenes desde CSV

**Como** operador, **quiero** importar órdenes masivamente desde un archivo CSV, **para** procesar grandes volúmenes sin captura manual.

**Criterios de aceptación:**
- [ ] CA-01: `GET /api/ordenes/importar/plantilla` descarga un CSV con headers y 3 filas de ejemplo
- [ ] CA-02: Columnas obligatorias de la plantilla: `referencia_cliente`, `nombre_cliente`, `ciudad_origen`, `ciudad_destino`, `tipo_mercancia`, `cantidad`, `peso_kg`, `modo_transporte`, `nivel_servicio`, `fecha_pickup`, `fecha_entrega`
- [ ] CA-03: Las filas válidas se crean como órdenes en estado `DRAFT` con `origen_creacion = 'CSV'`
- [ ] CA-04: Las filas inválidas se reportan en el JSON de respuesta con número de fila y descripción del error
- [ ] CA-05: `POST /api/ordenes/importar` retorna 200 con `{total, ok, errores, detalle[]}`
- [ ] CA-06: Patrón fail-soft (ADR-017) — si una fila falla el proceso continúa con las siguientes
- [ ] CA-07: `GET /api/ordenes/importaciones` retorna historial de importaciones del tenant
- [ ] CA-08: Importación de 500 filas completa en < 10 segundos
- [ ] CA-09: Cliente no encontrado por nombre → error de fila descriptivo: "No se encontró un cliente con ese nombre"
- [ ] CA-10: Auditoría registra `IMPORTAR_ORDENES` con la cantidad de filas importadas correctamente

**Endpoints API:**
```
GET  /api/ordenes/importar/plantilla   → 200 text/csv
POST /api/ordenes/importar             → 200 ImportacionOrdenResultDto
GET  /api/ordenes/importaciones        → 200 IEnumerable<ImportacionOrdenResumenDto>
```

**Módulo de permiso:** `ordenes`
**Estimación:** 8 pts | **Asignado a:** @BackendDev → @FrontendDev → @QA

---

### HU-023 · Recepción de órdenes por API REST externa

**Como** cliente enterprise, **quiero** enviar órdenes directamente desde mi ERP vía API, **para** automatizar la creación sin intervención manual.

**Alcance MVP:** Solo REST JSON. EDI 204 nativo se difiere a Sprint 21 (EP-17 Integraciones).

**Criterios de aceptación:**
- [ ] CA-01: `POST /api/v1/orders` con API Key válida en header `X-Api-Key` → 201 con `numero_referencia`
- [ ] CA-02: API Key inválida o ausente → 401 Unauthorized
- [ ] CA-03: API Key de otro tenant → 401 (aislamiento de tenant garantizado)
- [ ] CA-04: Payload inválido → 422 con detalle de errores por campo
- [ ] CA-05: La orden creada tiene `origen_creacion = 'API'`
- [ ] CA-06: Rate limit: máximo 100 requests/minuto por API Key (middleware de throttling)
- [ ] CA-07: `POST /api/configuracion/api-keys` genera clave con prefijo `frk_live_` visible una sola vez
- [ ] CA-08: La clave se almacena como hash bcrypt — nunca en texto plano
- [ ] CA-09: Soft-deactivate de API Key la deshabilita inmediatamente para nuevos requests
- [ ] CA-10: `ultimo_uso` se actualiza en cada request autenticado exitoso

**Endpoints API:**
```
POST   /api/v1/orders                         → 201 (auth: X-Api-Key)
GET    /api/configuracion/api-keys            → 200 IEnumerable<ApiKeyResponseDto>
POST   /api/configuracion/api-keys            → 201 ApiKeyResponseDto (RawKey visible una vez)
DELETE /api/configuracion/api-keys/{id}/deactivate → 200 (soft-delete)
```

**Módulo de permiso:** `configuracion` (gestión de keys) | Auth: `X-Api-Key` (recepción externa)
**Estimación:** 8 pts | **Asignado a:** @Arquitecto → @BackendDev → @QA

---

### HU-024 · Flujo de estados de la orden

**Como** operador, **quiero** que la orden avance por estados controlados, **para** tener trazabilidad completa del ciclo de vida del transporte.

**Criterios de aceptación:**
- [ ] CA-01: `PATCH /api/ordenes/{id}/estado` con `DRAFT → CONFIRMED` genera `numero_orden` con formato `{PREFIJO}-{YYYY}-{NNNNN}`
- [ ] CA-02: Transición inválida retorna 422: "Transición de estado inválida: {EstadoOrigen} → {EstadoDestino}"
- [ ] CA-03: Un CONDUCTOR no puede ejecutar `DRAFT → CONFIRMED` → 403 Forbidden
- [ ] CA-04: `CONFIRMED → CANCELLED` registra motivo en `historial_estados_orden`
- [ ] CA-05: Los estados terminales (`CLOSED`, `CANCELLED`) no admiten ninguna transición → 422
- [ ] CA-06: `GET /api/ordenes/{id}/historial` retorna los estados ordenados por `fecha_creacion DESC`
- [ ] CA-07: Dos requests simultáneos de confirmación de la misma orden no generan `numero_orden` duplicado (test de concurrencia)
- [ ] CA-08: Auditoría registra `CAMBIO_ESTADO` con estado anterior y nuevo en cada transición
- [ ] CA-09: El `OrdenResponseDto` incluye el campo `transiciones_disponibles[]` con los estados a los que puede ir desde el estado actual
- [ ] CA-10: La UI muestra solo los botones de acción correspondientes a `transiciones_disponibles`

**Endpoints API:**
```
PATCH /api/ordenes/{id}/estado      → 200 OrdenResponseDto
GET   /api/ordenes/{id}/historial   → 200 IEnumerable<HistorialEstadoOrdenDto>
```

**Módulo de permiso:** `ordenes` · Update
**Estimación:** 13 pts | **Asignado a:** @Arquitecto → @BackendDev → @QA → @FrontendDev

---

### HU-025 · Consolidación de órdenes en shipment

**Como** planificador, **quiero** consolidar múltiples órdenes en un solo embarque, **para** optimizar el uso de capacidad y reducir costos.

**Criterios de aceptación:**
- [ ] CA-01: Solo órdenes en estado `CONFIRMED` pueden consolidarse → 422 si alguna está en otro estado
- [ ] CA-02: Se requieren mínimo 2 órdenes para consolidar
- [ ] CA-03: Las órdenes consolidadas pasan a estado `ASSIGNED` y quedan vinculadas al `shipment_id`
- [ ] CA-04: Si `shipment_id` es null en el request → se crea un nuevo shipment en estado `PLANNED`
- [ ] CA-05: Desconsolidar devuelve las órdenes al estado `CONFIRMED` y limpia `shipment_id`
- [ ] CA-06: Solo se puede desconsolidar si el shipment no tiene carrier asignado (estado `PLANNED`)
- [ ] CA-07: Órdenes de distintos modos de transporte → advertencia en response pero no bloqueo
- [ ] CA-08: `GET /api/shipments/{id}/ordenes` retorna todas las órdenes del shipment

**Endpoints API:**
```
POST   /api/ordenes/consolidar                → 200 ShipmentResumenDto
DELETE /api/ordenes/{id}/desconsolidar        → 200 OrdenResponseDto
GET    /api/shipments/{id}/ordenes            → 200 IEnumerable<OrdenListDto>
```

**Módulo de permiso:** `ordenes` · Update
**Estimación:** 8 pts | **Asignado a:** @BackendDev → @QA → @FrontendDev

---

### HU-026 · División de órdenes (Split)

**Como** planificador, **quiero** dividir una orden en múltiples partes, **para** gestionar entregas parciales o por diferentes rutas.

**Criterios de aceptación:**
- [ ] CA-01: Split solo permitido en estados `CONFIRMED` o `ASSIGNED` → 422 en cualquier otro estado
- [ ] CA-02: La suma de `cantidad` y `peso_kg` de todos los splits debe igualar la orden original → 422 si no cuadra
- [ ] CA-03: Mínimo 2 splits; máximo 10 splits por operación
- [ ] CA-04: Cada sub-orden generada tiene `es_split = true` y `orden_origen_id` apuntando a la orden padre
- [ ] CA-05: La orden original pasa a estado `PARTIALLY_SPLIT`
- [ ] CA-06: Cada sub-orden inicia en estado `CONFIRMED` con su propio ciclo FSM independiente
- [ ] CA-07: `GET /api/ordenes/{id}` de la orden original incluye lista de sub-órdenes con su estado actual
- [ ] CA-08: El historial de la orden original registra la transición a `PARTIALLY_SPLIT`

**Endpoints API:**
```
POST /api/ordenes/{id}/split   → 200 IEnumerable<OrdenResponseDto>
```

**Módulo de permiso:** `ordenes` · Create
**Estimación:** 5 pts | **Asignado a:** @BackendDev → @QA → @FrontendDev

---

### HU-027 · Órdenes recurrentes y plantillas

**Como** operador, **quiero** guardar plantillas de órdenes frecuentes, **para** crear nuevas órdenes rápidamente sin reintroducir todos los datos.

**Criterios de aceptación:**
- [ ] CA-01: `POST /api/ordenes/{id}/guardar-como-plantilla` guarda un snapshot JSON de la orden → 201
- [ ] CA-02: `POST /api/plantillas-orden/{id}/crear-orden` precarga todos los campos del snapshot y retorna una orden en estado `DRAFT`
- [ ] CA-03: La orden creada desde plantilla tiene `origen_creacion = 'RECURRENTE'`
- [ ] CA-04: Configurar recurrencia `DIARIA` → `proxima_ejecucion = mañana`
- [ ] CA-05: Configurar recurrencia `SEMANAL` → `proxima_ejecucion = en 7 días`
- [ ] CA-06: El job `RecurrenciaOrdenesJob` ejecuta cada día a las 00:05 (patrón ADR-013)
- [ ] CA-07: El job procesa todas las plantillas con `es_recurrente = true` y `proxima_ejecucion ≤ hoy`
- [ ] CA-08: Tras crear la orden, el job actualiza `proxima_ejecucion` según la frecuencia
- [ ] CA-09: Auditoría registra `CREAR_ORDEN_RECURRENTE` con el ID de la plantilla origen
- [ ] CA-10: Desactivar una plantilla (`activo = false`) detiene las recurrencias sin borrar el historial

**Endpoints API:**
```
POST   /api/ordenes/{id}/guardar-como-plantilla        → 201 PlantillaOrdenResponseDto
GET    /api/plantillas-orden                           → 200 IEnumerable<PlantillaOrdenResponseDto>
POST   /api/plantillas-orden/{id}/crear-orden          → 201 OrdenResponseDto
PUT    /api/plantillas-orden/{id}/recurrencia          → 200 PlantillaOrdenResponseDto
DELETE /api/plantillas-orden/{id}/deactivate           → 200 (soft-delete)
```

**Módulo de permiso:** `ordenes`
**Background Job:** `RecurrenciaOrdenesJob` — patrón `PeriodicTimer` (ADR-013)
**Estimación:** 5 pts | **Asignado a:** @BackendDev → @QA → @FrontendDev

---

## Deuda técnica a resolver en este sprint

### G-03 — Descuento anual automático en planes

**Agente:** @FrontendDev
**Esfuerzo estimado:** 30 minutos
**Archivos:** `Areas/Admin/Views/Planes/Create.cshtml` y `Edit.cshtml`

```javascript
// Al cambiar precio mensual → mostrar sugerencia de precio anual
document.getElementById('precioMensual').addEventListener('input', (e) => {
    const mensual  = parseFloat(e.target.value) || 0;
    const sugerido = (mensual * 10).toFixed(2); // 2 meses gratis
    document.getElementById('precioAnualSugerido').textContent =
        `Sugerido: $${sugerido} (2 meses gratis)`;
    // NO auto-rellenar el campo — solo mostrar la sugerencia
});
```

```html
<!-- Agregar debajo del input precioAnual -->
<span id="precioAnualSugerido" class="form-text text-muted">
    Ingresa el precio mensual para ver la sugerencia
</span>
```

### G-07 — UbicacionResponseDto con campos faltantes

**Agente:** @BackendDev
**Esfuerzo estimado:** 1 hora
**Archivos:** `UbicacionResponseDto.cs`, `UbicacionRepository.cs`

Agregar los siguientes campos al response DTO y al query SQL:

```csharp
public string?  CodigoPostal      { get; set; }
public TimeOnly? HorarioApertura  { get; set; }
public TimeOnly? HorarioCierre    { get; set; }
public string?  Instrucciones     { get; set; }
```

---

## Estructura de Archivos a Crear en el Sprint

### @Arquitecto entrega:

```
Freiroute.Utility/
├── Constants/OrdenConstants.cs
│     OrdenEstado · ModoTransporte · NivelServicio ·
│     OrdenPrioridad · OrigenCreacion · FrecuenciaRecurrencia
└── Orders/OrderStateMachine.cs

Freiroute.Entity/
├── Orden.cs
├── LineaOrden.cs
├── HistorialEstadoOrden.cs
├── PlantillaOrden.cs
├── ApiKeyTenant.cs
└── ImportacionOrden.cs

Freiroute.DTO/Orden/
├── OrdenRequestDto.cs
├── OrdenResponseDto.cs
├── OrdenListDto.cs
├── OrdenFiltroDto.cs
├── LineaOrdenRequestDto.cs
├── LineaOrdenResponseDto.cs
├── CambiarEstadoOrdenRequestDto.cs
├── ConsolidarOrdenesRequestDto.cs
├── SplitOrdenRequestDto.cs
├── PlantillaOrdenRequestDto.cs
├── ConfigurarRecurrenciaRequestDto.cs
├── PlantillaOrdenResponseDto.cs
├── ImportacionOrdenResultDto.cs
├── ApiKeyResponseDto.cs
└── HistorialEstadoOrdenDto.cs

Freiroute.DAL/Interfaces/
├── IOrdenRepository.cs
├── IPlantillaOrdenRepository.cs
├── IImportacionOrdenRepository.cs
└── IApiKeyTenantRepository.cs

Freiroute.BLL/Interfaces/
├── IOrdenService.cs
├── IOrdenImportService.cs
├── IOrdenApiExternaService.cs
└── IPlantillaOrdenService.cs
```

### @IngenieroDatos entrega:

```
supabase/migrations/
├── YYYYMMDDHHMMSS_tabla_contadores_orden.sql
├── YYYYMMDDHHMMSS_funcion_generar_numero_orden.sql
├── YYYYMMDDHHMMSS_tabla_api_keys_tenant.sql
├── YYYYMMDDHHMMSS_tabla_shipments_base.sql
├── YYYYMMDDHHMMSS_tabla_ordenes.sql
├── YYYYMMDDHHMMSS_tabla_lineas_orden.sql
├── YYYYMMDDHHMMSS_tabla_historial_estados_orden.sql
├── YYYYMMDDHHMMSS_tabla_plantillas_orden.sql
└── YYYYMMDDHHMMSS_tabla_importaciones_orden.sql

Freiroute.DAL/Repositories/
├── OrdenRepository.cs
├── PlantillaOrdenRepository.cs
├── ImportacionOrdenRepository.cs
└── ApiKeyTenantRepository.cs
```

### @BackendDev entrega:

```
Freiroute.BLL/
├── Services/
│   ├── OrdenService.cs
│   ├── OrdenImportService.cs
│   ├── OrdenApiExternaService.cs
│   └── PlantillaOrdenService.cs
├── Validators/
│   ├── OrdenValidator.cs
│   └── CambiarEstadoOrdenValidator.cs
└── Jobs/
    └── RecurrenciaOrdenesJob.cs

Freiroute.API/Controllers/
├── OrdenesController.cs
├── OrdenesImportController.cs      ← rutas /importar
├── OrdenesApiExternaController.cs  ← ruta /api/v1/orders
├── PlantillasOrdenController.cs
└── ShipmentsController.cs          ← solo GET /api/shipments/{id}/ordenes en Sprint 4

-- Fix G-03: UbicacionResponseDto.cs + UbicacionRepository.cs
-- Fix G-07: Planes/Create.cshtml + Edit.cshtml (JS)
```

### @QA entrega:

```
tests/Freiroute.BLL.Tests/
├── Orders/
│   ├── OrdenServiceTests.cs          ← CAs de HU-021, HU-024, HU-025, HU-026
│   ├── OrdenImportServiceTests.cs    ← CAs de HU-022
│   ├── PlantillaOrdenServiceTests.cs ← CAs de HU-027
│   └── OrderStateMachineTests.cs     ← todas las transiciones válidas e inválidas
└── Validators/
    ├── OrdenValidatorTests.cs
    └── CambiarEstadoOrdenValidatorTests.cs

tests/Freiroute.API.Tests/
├── Controllers/
│   ├── OrdenesControllerTests.cs
│   └── OrdenesApiExternaControllerTests.cs  ← CAs de HU-023 (auth con X-Api-Key)
└── Concurrency/
    └── NumeracionOrdenConcurrencyTests.cs   ← CA-07 de HU-024 (race condition)
```

### @FrontendDev entrega:

```
Freiroute.Aplicacion/Areas/Tenant/
├── Controllers/OrdenesController.cs
└── Views/Ordenes/
    ├── Index.cshtml       ← tabla paginada con badges de estado y filtros
    ├── Create.cshtml      ← formulario completo HU-021
    ├── Edit.cshtml        ← edición (solo DRAFT/CONFIRMED)
    └── Detail.cshtml      ← detalle + historial de estados + botones de acción

-- Fix G-03:
Areas/Admin/Views/Planes/Create.cshtml  ← JS sugerencia descuento anual
Areas/Admin/Views/Planes/Edit.cshtml    ← JS sugerencia descuento anual
```

---

## Definición de Done (DoD) — Sprint 4

Un Sprint 4 está completo cuando:

- [ ] `supabase db push` aplicado sin errores — 9 nuevas tablas/objetos en BD
- [ ] `supabase db diff` sin cambios pendientes
- [ ] `dotnet build` sin warnings
- [ ] `dotnet test` sin fallos
- [ ] Cobertura BLL ≥ 80% (Coverlet)
- [ ] Cobertura API ≥ 60% (Coverlet)
- [ ] Crear orden DRAFT funcional desde la UI con todos los selectores (cliente, origen, destino, mercancía)
- [ ] Transición `DRAFT → CONFIRMED` genera número de orden en formato correcto
- [ ] Importación CSV de 10+ filas funcional con reporte de errores
- [ ] API Key generada, usada en `POST /api/v1/orders` y autenticada correctamente
- [ ] `OrderStateMachine` — 100% de transiciones válidas e inválidas cubiertas por tests
- [ ] Concurrencia: test de doble confirmación simultánea sin duplicar `numero_orden`
- [ ] `RecurrenciaOrdenesJob` registrado en el contenedor DI y ejecutando sin errores
- [ ] Badges de estado en la lista de órdenes con color semántico correcto (Design System)
- [ ] G-03 cerrado: sugerencia de descuento anual visible en Planes Create y Edit
- [ ] G-07 cerrado: `UbicacionResponseDto` expone `codigo_postal`, `horarios`, `instrucciones`
- [ ] Swagger documentado en todos los endpoints del sprint
- [ ] PR revisado y aprobado

---

*Spec Sprint 4 — Freiroute TMS*
*Versión: 1.0 | Fecha: 2026*
*Próximo: Sprint 5 — EP-04 Órdenes Avanzadas (HU-028 a HU-032)*
