-- ============================================================
-- TABLA: ordenes
-- Descripción: Órdenes de transporte — entidad central de
--              EP-04 Order Management. Sigue la FSM de ADR-019.
-- HU: HU-021 · HU-022 · HU-023 · HU-024 · HU-025 · HU-026
-- ============================================================

CREATE TABLE IF NOT EXISTS ordenes (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id               UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    numero_orden             VARCHAR(30),
    cliente_id               UUID NOT NULL REFERENCES clientes(id),
    origen_id                UUID NOT NULL REFERENCES ubicaciones(id),
    destino_id               UUID NOT NULL REFERENCES ubicaciones(id),
    tipo_mercancia_id        UUID NOT NULL REFERENCES tipos_mercancia(id),
    unidad_medida_id         UUID NOT NULL REFERENCES unidades_medida(id),
    tipo_embalaje_id         UUID REFERENCES tipos_embalaje(id),
    tarifa_id                UUID REFERENCES tarifas_base(id),
    shipment_id              UUID REFERENCES shipments(id),
    api_key_id               UUID REFERENCES api_keys_tenant(id),
    cantidad                 NUMERIC(12,3) NOT NULL,
    peso_kg                  NUMERIC(12,3) NOT NULL,
    volumen_m3               NUMERIC(12,3),
    valor_declarado          NUMERIC(15,2),
    modo_transporte          VARCHAR(20) NOT NULL DEFAULT 'TERRESTRE',
    nivel_servicio           VARCHAR(20) NOT NULL DEFAULT 'ESTANDAR',
    prioridad                VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    fecha_pickup_solicitada  DATE,
    fecha_entrega_requerida  DATE,
    fecha_confirmacion       TIMESTAMPTZ,
    referencia_cliente       VARCHAR(100),
    instrucciones            TEXT,
    estado                   VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    es_split                 BOOLEAN NOT NULL DEFAULT false,
    orden_origen_id          UUID REFERENCES ordenes(id),
    origen_creacion          VARCHAR(20) NOT NULL DEFAULT 'MANUAL',
    activo                   BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion           TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion       TIMESTAMPTZ NOT NULL DEFAULT now(),
    creado_por               UUID REFERENCES usuarios(id),
    modificado_por           UUID REFERENCES usuarios(id)
);

COMMENT ON TABLE ordenes IS
    'Órdenes de transporte — entidad central de EP-04 Order Management.
     Cada orden sigue la máquina de estados finitos definida en ADR-019.
     El número de orden se genera al confirmar (ADR-020).';

COMMENT ON COLUMN ordenes.numero_orden IS
    'Número legible generado al confirmar: {PREFIJO}-{YYYY}-{NNNNN}.
     NULL mientras la orden está en DRAFT. Ver ADR-020.';

COMMENT ON COLUMN ordenes.estado IS
    'Estado FSM según ADR-019:
     DRAFT | CONFIRMED | ASSIGNED | PICKUP_SCHEDULED | IN_TRANSIT |
     DELIVERED | INVOICED | CLOSED | CANCELLED | ON_HOLD |
     FAILED_DELIVERY | PARTIALLY_SPLIT';

COMMENT ON COLUMN ordenes.modo_transporte IS
    'Modo de transporte: TERRESTRE | AEREO | MARITIMO | FERROVIARIO | INTERMODAL.
     Nota: las tarifas_base de Sprint 3 usan FTL/LTL — dominio distinto.';

COMMENT ON COLUMN ordenes.nivel_servicio IS
    'Nivel: ESTANDAR | EXPRESS | PROGRAMADO';

COMMENT ON COLUMN ordenes.prioridad IS
    'Prioridad de la orden: CRITICO | ALTO | NORMAL | BAJO';

COMMENT ON COLUMN ordenes.origen_creacion IS
    'Canal de ingreso: MANUAL (UI) | CSV (importación masiva) |
     API (integración externa EDI/REST) | RECURRENTE (plantilla)';

COMMENT ON COLUMN ordenes.es_split IS
    'true si esta orden es resultado de dividir una orden padre (HU-026).
     La orden padre pasa a estado PARTIALLY_SPLIT.';

COMMENT ON COLUMN ordenes.orden_origen_id IS
    'Referencia a la orden padre cuando es_split = true.
     La orden padre puede tener múltiples sub-órdenes activas.';

-- Índice UNIQUE para numero_orden por tenant (parcial — excluye NULLs de DRAFT)
CREATE UNIQUE INDEX idx_ordenes_numero_empresa
    ON ordenes(empresa_id, numero_orden)
    WHERE numero_orden IS NOT NULL;

-- Índices operativos
CREATE INDEX idx_ordenes_empresa_id     ON ordenes(empresa_id);
CREATE INDEX idx_ordenes_activo         ON ordenes(activo);
CREATE INDEX idx_ordenes_estado         ON ordenes(empresa_id, estado);
CREATE INDEX idx_ordenes_cliente        ON ordenes(empresa_id, cliente_id);
CREATE INDEX idx_ordenes_shipment       ON ordenes(shipment_id)
    WHERE shipment_id IS NOT NULL;
CREATE INDEX idx_ordenes_fecha_pickup   ON ordenes(empresa_id, fecha_pickup_solicitada);
CREATE INDEX idx_ordenes_fecha_entrega  ON ordenes(empresa_id, fecha_entrega_requerida);
-- Índice de prioridad + estado para la vista del dispatcher
CREATE INDEX idx_ordenes_prioridad_estado ON ordenes(empresa_id, prioridad, estado)
    WHERE activo = true;

ALTER TABLE ordenes ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_ordenes" ON ordenes
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_ordenes_fecha_modificacion
    BEFORE UPDATE ON ordenes
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();