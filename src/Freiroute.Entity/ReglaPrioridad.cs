namespace Freiroute.Entity;

/// <summary>
/// Regla configurable por tenant para elevación automática de la
/// prioridad de órdenes (HU-029). Corresponde a la tabla
/// 'reglas_prioridad'. Las condiciones válidas son:
/// ENTREGA_PROXIMA | CLIENTE_VIP | SIN_AVANCE.
/// </summary>
public class ReglaPrioridad
{
    // ── Campos base ───────────────────────────────────────────
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)

    // ── Configuración de la regla ─────────────────────────────
    public string Nombre { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL — único por empresa (índice parcial)
    public string Condicion { get; set; } = string.Empty; // VARCHAR(50) NOT NULL — ENTREGA_PROXIMA|CLIENTE_VIP|SIN_AVANCE
    public int HorasUmbral { get; set; }                // INTEGER NOT NULL DEFAULT 24 — umbral en horas de la condición
    public string NivelDestino { get; set; } = string.Empty; // VARCHAR(20) NOT NULL DEFAULT 'ALTO' — CRITICO|ALTO

    // ── Auditoría estándar ───────────────────────────────────
    public bool Activo { get; set; } = true;            // Soft delete universal (ADR-005)
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
    public DateTime FechaModificacion { get; set; }     // TIMESTAMPTZ — trigger update_fecha_modificacion()
}