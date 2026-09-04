-- ============================================================
-- MIGRACIÓN 05 - TABLA codigos_2fa_temporales (Append-only)
-- Freiroute TMS - Sprint 2 EP-02
-- ============================================================
-- Códigos 2FA de un solo uso enviados por email.
-- EXCEPCIONES al patrón estándar:
--   - SIN empresa_id (se resuelve por usuario_id — ADR-011)
--   - SIN campo activo (se purgan por fecha_expiracion)
--   - SIN trigger de fecha_modificacion (append-only)
--   - SIN RLS (acceso solo por usuario_id + codigo_hash)
-- El único DELETE físico autorizado fuera de soft-delete (ADR-013):
--   DELETE FROM codigos_2fa_temporales
--   WHERE fecha_expiracion < NOW() OR usado = true
-- ============================================================

CREATE TABLE IF NOT EXISTS codigos_2fa_temporales (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Relación
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    -- Datos del código
    codigo_hash         VARCHAR(500) NOT NULL,
    tipo                VARCHAR(20) NOT NULL DEFAULT 'EMAIL',
    usado               BOOLEAN NOT NULL DEFAULT false,
    -- Expiración
    fecha_expiracion    TIMESTAMPTZ NOT NULL,
    -- Timestamp
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- NOTA: No hay empresa_id, activo, ni fecha_modificacion
);

COMMENT ON TABLE codigos_2fa_temporales IS
    'Códigos 2FA de un solo uso enviados por email. Expiran en 10 minutos. Tabla append-only — se purga por el job de vencimientos.';
COMMENT ON COLUMN codigos_2fa_temporales.id IS
    'Identificador único del código temporal (UUID generado por la BD)';
COMMENT ON COLUMN codigos_2fa_temporales.usuario_id IS
    'FK al usuario destinatario del código. Se resuelve empresa_id a través de usuarios.empresa_id.';
COMMENT ON COLUMN codigos_2fa_temporales.codigo_hash IS
    'Hash SHA-256 del código de 6 dígitos. Nunca se almacena el código en claro.';
COMMENT ON COLUMN codigos_2fa_temporales.tipo IS
    'Tipo de código: EMAIL (código enviado por email) o TOTP (código de app autenticadora)';
COMMENT ON COLUMN codigos_2fa_temporales.usado IS
    'true si el código ya fue validado exitosamente. Un solo uso.';
COMMENT ON COLUMN codigos_2fa_temporales.fecha_expiracion IS
    'Fecha/hora de expiración del código (10 minutos después de la creación)';
COMMENT ON COLUMN codigos_2fa_temporales.fecha_creacion IS
    'Timestamp de cuándo se generó el código';

-- Índices
CREATE INDEX idx_2fa_temp_usuario   ON codigos_2fa_temporales(usuario_id);
CREATE INDEX idx_2fa_temp_expira    ON codigos_2fa_temporales(fecha_expiracion);
-- Para el background job de purga (ADR-013):
-- Busca códigos no usados cuya expiración ya pasó
CREATE INDEX idx_2fa_temporales_expiracion
    ON codigos_2fa_temporales(fecha_expiracion)
    WHERE usado = false;

-- NO tiene RLS — acceso filtrado por usuario_id + codigo_hash en BLL
-- NO tiene trigger de fecha_modificacion — es append-only
-- El DELETE físico de esta tabla está autorizado por ADR-013:
-- DELETE FROM codigos_2fa_temporales
-- WHERE fecha_expiracion < NOW() OR usado = true
