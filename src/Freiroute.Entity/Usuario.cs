namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa a un usuario del sistema por empresa.
/// Corresponde a la tabla 'usuarios'. La autenticación la gestiona Supabase Auth;
/// este registro guarda el perfil de negocio y el vínculo con auth.users
/// (supabase_user_id).
/// </summary>
public class Usuario
{
    // ── Campos base obligatorios ──────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK tenant — discriminador multi-tenant
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Identificación ────────────────────────────────────────────
    public Guid PerfilId { get; set; }                  // FK perfiles(id) ON DELETE RESTRICT
    public string TipoIdentidad { get; set; } = "CEDULA";   // CEDULA | PASAPORTE | RUC | DNI
    public string? NumeroIdentidad { get; set; }             // VARCHAR(50)
    public string NombreCompleto { get; set; } = string.Empty; // VARCHAR(200) NOT NULL
    public string Email { get; set; } = string.Empty;        // VARCHAR(200) NOT NULL — UNIQUE(email, empresa_id)
    public string? Telefono { get; set; }                    // VARCHAR(50)
    public string? FotoUrl { get; set; }                     // TEXT (Supabase Storage)

    // ── Auth (Supabase Auth) ──────────────────────────────────────
    public Guid? SupabaseUserId { get; set; }            // FK hacia auth.users de Supabase — UNIQUE

    // ── Estado y seguridad de cuenta ──────────────────────────────
    public string TipoUsuario { get; set; } = "OPERADOR";   // SUPER_ADMIN|ADMIN|DISPATCHER|OPERADOR|CONDUCTOR|CLIENTE
    public string Estado { get; set; } = "PENDING";         // PENDING|ACTIVE|SUSPENDED|LOCKED
    public DateTime? UltimoAcceso { get; set; }             // TIMESTAMPTZ
    public int IntentosFallidos { get; set; } = 0;          // INTEGER NOT NULL DEFAULT 0
    public DateTime? BloqueadoHasta { get; set; }           // TIMESTAMPTZ — bloqueo tras 5 intentos fallidos
}