namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un permiso granular por perfil y módulo.
/// Corresponde a la tabla 'permisos'. Solo existen 3 niveles: READ, CREATE, UPDATE.
/// Cada módulo se modela con 3 flags booleanos (puede_leer, puede_crear, puede_actualizar).
/// No existe DELETE — los permisos solo se desactivan (activo = false).
/// </summary>
public class Permiso
{
    // ── Campos base obligatorios ──────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK tenant — discriminador multi-tenant
    public bool Activo { get; set; } = true;            // Soft delete universal
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // ── Campos de negocio ────────────────────────────────────────
    public Guid PerfilId { get; set; }                  // FK perfiles(id) ON DELETE CASCADE
    public string Modulo { get; set; } = string.Empty;  // Módulo del TMS: ordenes, embarques, carriers, rutas, ...
    public bool PuedeLeer { get; set; } = false;        // Permiso READ: ver listados y detalles
    public bool PuedeCrear { get; set; } = false;       // Permiso CREATE: crear nuevos registros
    public bool PuedeActualizar { get; set; } = false;  // Permiso UPDATE: editar y desactivar registros
}