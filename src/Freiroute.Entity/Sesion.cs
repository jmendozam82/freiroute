namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa una sesión con refresh token por usuario.
/// Corresponde a la tabla 'sesiones' (ver diagrama del spec Sprint 1:
/// usuarios ──── sesiones).
/// </summary>
public class Sesion
{
    public Guid Id { get; set; }                            // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                     // FK tenant — discriminador multi-tenant
    public Guid UsuarioId { get; set; }                     // FK usuarios(id)
    public string RefreshTokenHash { get; set; } = string.Empty; // SHA-256 del refresh token (opaco, UUID aleatorio)
    public DateTime FechaExpiracion { get; set; }           // TIMESTAMPTZ — 30 días por defecto (HU-003 CA-02)
    public bool Activa { get; set; } = true;                // Soft revoke del refresh token
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}