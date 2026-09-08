-- ============================================================
-- TABLA: api_keys_tenant
-- Descripción: API Keys para integración REST externa (HU-023).
--              La clave nunca se almacena en texto plano.
-- ============================================================

CREATE TABLE IF NOT EXISTS api_keys_tenant (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    nombre          VARCHAR(100) NOT NULL,
    clave_hash      TEXT NOT NULL,
    activo          BOOLEAN NOT NULL DEFAULT true,
    ultimo_uso      TIMESTAMPTZ,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE api_keys_tenant IS
    'API Keys para integración REST externa por tenant (HU-023).
     El valor crudo es visible solo una vez al crear la key;
     se almacena únicamente el hash bcrypt.';

COMMENT ON COLUMN api_keys_tenant.clave_hash IS
    'Hash bcrypt de la API key. Formato visible al cliente: frk_live_{uuid-sin-guiones}.
     NUNCA almacenar el valor en texto plano.';

COMMENT ON COLUMN api_keys_tenant.ultimo_uso IS
    'Timestamp del último request autenticado exitoso con esta key.
     Actualizado en cada uso por UpdateUltimoUsoAsync.';

CREATE INDEX idx_api_keys_empresa    ON api_keys_tenant(empresa_id);
CREATE INDEX idx_api_keys_activo     ON api_keys_tenant(empresa_id, activo);

ALTER TABLE api_keys_tenant ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_api_keys" ON api_keys_tenant
    FOR ALL
    USING (empresa_id = (
        current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_api_keys_fecha_modificacion
    BEFORE UPDATE ON api_keys_tenant
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();