namespace Freiroute.Entity;

/// <summary>
/// API Key para integración REST externa por tenant (HU-023).
/// La clave crudo se almacena únicamente como hash bcrypt — nunca en
/// texto plano. El valor visible al cliente (frk_live_{uuid}) se muestra
/// solo una vez al crear la key.
/// Corresponde a la tabla 'api_keys_tenant'.
/// </summary>
public class ApiKeyTenant
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Identificación ────────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL

    // ── Seguridad ────────────────────────────────────────────
    public string ClaveHash { get; set; } = string.Empty; // TEXT NOT NULL — hash bcrypt

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime? UltimoUso { get; set; }            // TIMESTAMPTZ — último request autenticado exitoso
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}
