namespace Freiroute.Entity;

/// <summary>
/// Contacto de un cliente con rol diferenciado. El contacto principal
/// recibe las notificaciones del sistema.
/// Corresponde a la tabla 'contactos_cliente' (HU-019).
/// </summary>
public class ContactoCliente
{
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid ClienteId { get; set; }                 // FK clientes(id) ON DELETE CASCADE
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(200) NOT NULL
    public string? Cargo { get; set; }                  // VARCHAR(100)
    public string Rol { get; set; } = "GENERAL";        // VARCHAR(50) DEFAULT 'GENERAL'
    public string? Email { get; set; }                  // VARCHAR(200)
    public string? Telefono { get; set; }               // VARCHAR(50)
    public bool EsPrincipal { get; set; } = false;      // BOOLEAN DEFAULT false
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime FechaModificacion { get; set; }     // TIMESTAMPTZ DEFAULT NOW()
}