namespace Freiroute.DTO.Auditoria;

/// <summary>
/// Datos de salida de un registro del log de auditoría (HU-008).
/// Exposición directa del log inmutable: no existe Update ni Deactivate
/// (HU-008 CA-06). Detalles es JSON serializado (string) — el frontend lo
/// parsea según el tipo de acción.
/// </summary>
public class AuditoriaActivityResponseDto
{
    public Guid Id { get; set; }
    public Guid? EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? EntidadTipo { get; set; }
    public Guid? EntidadId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Detalles { get; set; }
    public DateTime FechaCreacion { get; set; }
}