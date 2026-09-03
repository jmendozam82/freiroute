namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa una invitación de usuario por email.
/// Corresponde a la tabla 'invitaciones'. El token expira en 48 horas.
/// NOTA: la tabla del spec NO incluye activo ni fecha_modificacion —
/// su ciclo de vida se controla con el campo Estado (PENDING|ACCEPTED|EXPIRED|CANCELLED).
/// </summary>
public class Invitacion
{
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK tenant — ON DELETE CASCADE

    public string Email { get; set; } = string.Empty;   // VARCHAR(200) NOT NULL
    public Guid PerfilId { get; set; }                  // FK perfiles(id) — perfil asignado al aceptar
    public string Token { get; set; } = string.Empty;   // VARCHAR(200) NOT NULL UNIQUE
    public string Estado { get; set; } = "PENDING";     // PENDING | ACCEPTED | EXPIRED | CANCELLED
    public DateTime FechaExpiracion { get; set; }       // TIMESTAMPTZ NOT NULL
    public DateTime? FechaAceptacion { get; set; }      // TIMESTAMPTZ
    public Guid? CreadoPorId { get; set; }              // FK usuarios(id) — quién invitó
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ NOT NULL
}