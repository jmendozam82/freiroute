namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un perfil (rol) de usuario por empresa.
/// Corresponde a la tabla 'perfiles'. Cada tenant define sus propios perfiles;
/// los perfiles base del sistema (ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE)
/// se crean automáticamente al registrar un tenant con es_sistema = true.
/// </summary>
public class Perfil
{
    // ── Campos base obligatorios ──────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK tenant — discriminador multi-tenant
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos de negocio ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;                  // VARCHAR(100) NOT NULL
    public string? Descripcion { get; set; }                            // TEXT
    public string TipoPerfil { get; set; } = "CUSTOM";                  // SUPER_ADMIN|ADMIN|DISPATCHER|OPERADOR|CONDUCTOR|CLIENTE|CUSTOM
    public bool EsSistema { get; set; } = false;       // true = perfil creado por el sistema (no se puede desactivar)
}