namespace Freiroute.Entity;

/// <summary>
/// Registro de historial de importaciones CSV masivas de órdenes (HU-022).
/// Patrón fail-soft (ADR-017): filas válidas se crean como órdenes DRAFT,
/// filas inválidas se reportan en detalle_errores sin detener el proceso.
/// Corresponde a la tabla 'importaciones_orden'.
/// </summary>
public class ImportacionOrden
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid UsuarioId { get; set; }                 // FK usuarios(id) NOT NULL — quién ejecutó la importación

    // ── Archivo ───────────────────────────────────────────────
    public string NombreArchivo { get; set; } = string.Empty; // VARCHAR(255) NOT NULL

    // ── Contadores ────────────────────────────────────────────
    public int TotalFilas { get; set; } = 0;            // INT NOT NULL DEFAULT 0
    public int FilasOk { get; set; } = 0;               // INT NOT NULL DEFAULT 0
    public int FilasError { get; set; } = 0;            // INT NOT NULL DEFAULT 0

    // ── Detalle de errores ────────────────────────────────────
    public string? DetalleErrores { get; set; }         // JSONB — [{fila: N, campo: "...", error: "..."}]

    // ── Control ───────────────────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime? FechaModificacion { get; set; }    // TIMESTAMPTZ — trigger update_fecha_modificacion()
}
