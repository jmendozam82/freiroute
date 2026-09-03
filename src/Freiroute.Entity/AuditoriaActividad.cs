namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa un registro inmutable de auditoría de actividad.
/// Corresponde a la tabla 'auditoria_actividad'. Es de solo escritura —
/// nadie puede editar ni eliminar registros. Retención mínima: 12 meses.
/// NOTA: la tabla NO incluye activo ni fecha_modificacion (log inmutable).
/// </summary>
public class AuditoriaActividad
{
    public Guid Id { get; set; }                        // PK, gen_random_uuid()

    /// <summary>FK empresas(id) ON DELETE SET NULL — nullable porque el Super Admin opera sin tenant.</summary>
    public Guid? EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }                // FK usuarios(id)

    public string Modulo { get; set; } = string.Empty;  // VARCHAR(100) NOT NULL — módulo del TMS
    public string Accion { get; set; } = string.Empty;  // LOGIN|LOGOUT|LOGIN_FAILED|CREATE|UPDATE|DEACTIVATE|EXPORT|CAMBIO_ESTADO
    public string? EntidadTipo { get; set; }            // Nombre de la entidad afectada
    public Guid? EntidadId { get; set; }                // ID del registro afectado
    public string? IpAddress { get; set; }              // INET — se lee con cast ::text en SQL
    public string? UserAgent { get; set; }              // TEXT
    public string? Detalles { get; set; }               // JSONB — JSON serializado (valores anteriores/nuevos, contexto)

    public DateTime FechaCreacion { get; set; }         // TIMESTAMPTZ NOT NULL
}