# ADR-011: Cifrado del TOTP Secret (AES-256 in-app)

## Estado
Aceptado

## Fecha
2026 — Sprint 2

## Contexto
Para el 2FA (HU-005), el sistema almacena el secret TOTP de cada usuario
en la tabla `configuracion_2fa`. El secret TOTP es el material secreto que
permite validar los códigos de 6 dígitos generados por la app autenticadora.
Si se filtra la base de datos, un atacante con el secret podría reproducir
todos los códigos 2FA de los usuarios. Es crítico protegerlo en reposo.

## Decisión
El secret TOTP se cifra con **AES-256-GCM in-app** antes de persistir, con
una clave maestra almacenada en variable de entorno / secreto del entorno
(`TOTP_ENCRYPTION_KEY`). La clave nunca se hardcodea ni se commitea al
repositorio (regla AGENTS.md #33).

- Cifrado simétrico AES-256-GCM con IV aleatorio por registro (12 bytes)
  y tag de autenticación (16 bytes).
- El formato persistido en BD es: `base64(iv + tag + ciphertext)`.
- El descifrado ocurre en memoria únicamente al verificar un código 2FA.
- La clave maestra se rota con notación de versionado (`key:version`) para
  permitir migración sin downtime en el futuro.

## Alternativas Consideradas

1. **Vault externo (HashiCorp Vault / Azure Key Vault)** — Descartada en esta
   fase por complejidad de operación y falta de infraestructura de secrets en
   el entorno inicial. Se evalúa para Sprint 13 (cuando la facturación y
   credenciales de pago requieran gestión de secretos más robusta).

2. **Sin cifrado (secret en claro)** — Descartada: filtración de BD expondría
   el 2FA completo. Inaceptable para seguridad.

3. **Hash del secret (imposible descifrar)** — Descartada porque el TOTP
   requiere el secret original para validar códigos (no es un valor que se
   compare directamente, sino que se usa como semilla del algoritmo HMAC).
   Es imposible validar TOTP solo con el hash.

## Consecuencias

**Positivas:**
- El secret TOTP queda protegido en reposo — un dump de BD no basta para
  comprometer el 2FA.
- AES-256-GCM es un cifrado autenticado: detecta manipulación.
- La clave maestra se gestiona como secreto de entorno (12-factor).

**Negativas / Trade-offs:**
- Requiere gestionar y rotar la clave maestra.
- Un error en la rotación de clave podría impedir descifrar secrets existentes
  (mitigado con versionado de clave).

## Módulos Afectados
- Tabla `configuracion_2fa.totp_secret` (Sprint 2)
- `Configuracion2faService` / `AuthService` (HU-005)
- nuevo helper `Freiroute.Utility` de cifrado simétrico
- configuración de entorno: `TOTP_ENCRYPTION_KEY`
