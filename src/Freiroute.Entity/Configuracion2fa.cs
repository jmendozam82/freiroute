namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa la configuración de autenticación de dos factores (2FA)
/// de un usuario. Corresponde a la tabla 'configuracion_2fa'.
/// El totp_secret se almacena cifrado con AES-256 — nunca en claro (ADR-004).
/// Los códigos de recuperación se almacenan como hashes SHA-256.
/// </summary>
public class Configuracion2fa
{
    // ── Identity / Lifecycle ──────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Relaciones ────────────────────────────────────────────────
    public Guid EmpresaId { get; set; }                 // FK empresas(id) ON DELETE CASCADE
    public Guid UsuarioId { get; set; }                 // FK usuarios(id) ON DELETE CASCADE — UNIQUE(usuario_id)

    // ── TOTP ──────────────────────────────────────────────────────
    public string? TotpSecret { get; set; }             // VARCHAR(500) — cifrado AES-256, nunca en claro
    public bool TotpHabilitado { get; set; } = false;   // BOOLEAN NOT NULL

    // ── Email 2FA ─────────────────────────────────────────────────
    public bool EmailHabilitado { get; set; } = false;  // BOOLEAN NOT NULL

    // ── Códigos de recuperación ───────────────────────────────────
    public string[] CodigosRecuperacion { get; set; } = [];  // TEXT[] — 8 hashes SHA-256 de un solo uso
}
