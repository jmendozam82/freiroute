namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un código temporal de 2FA enviado por email.
/// Corresponde a la tabla 'codigos_2fa_temporales'.
/// Los códigos expiran en 10 minutos. Se purgan por el job de vencimientos.
/// NOTA: NO tiene empresa_id — se resuelve a través de usuario_id (ADR-005).
/// NO tiene campo 'activo' — se controla por fecha_expiracion y uso.
/// </summary>
public class Codigo2faTempora
{
    // ── Identity ──────────────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()

    // ── Relación ──────────────────────────────────────────────────
    public Guid UsuarioId { get; set; }                 // FK usuarios(id) ON DELETE CASCADE

    // ── Datos del código ──────────────────────────────────────────
    public string CodigoHash { get; set; } = string.Empty;  // VARCHAR(500) — hash del código
    public string Tipo { get; set; } = "EMAIL";             // EMAIL | TOTP
    public bool Usado { get; set; } = false;                // BOOLEAN — un solo uso

    // ── Expiración ────────────────────────────────────────────────
    public DateTime FechaExpiracion { get; set; }       // TIMESTAMPTZ — código válido por 10 minutos

    // ── Timestamp ─────────────────────────────────────────────────
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ NOT NULL
}
