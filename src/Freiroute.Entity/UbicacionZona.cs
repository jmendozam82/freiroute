namespace Freiroute.Entity;

/// <summary>
/// Relación muchos-a-muchos entre ubicaciones y zonas de entrega:
/// una ubicación puede pertenecer a múltiples zonas.
/// Corresponde a la tabla 'ubicacion_zonas' (HU-016).
/// Tabla de relación pura — SIN Activo ni FechaModificacion.
/// </summary>
public class UbicacionZona
{
    public Guid Id { get; set; }                        // PK, gen_random_uuid()
    public Guid EmpresaId { get; set; }                 // FK empresas(id) — discriminador tenant (ADR-003)
    public Guid UbicacionId { get; set; }               // FK ubicaciones(id) ON DELETE CASCADE
    public Guid ZonaId { get; set; }                    // FK zonas_entrega(id) ON DELETE CASCADE
    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ DEFAULT NOW()
}