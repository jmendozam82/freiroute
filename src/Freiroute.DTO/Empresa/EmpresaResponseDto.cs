namespace Freiroute.DTO.Empresa;

/// <summary>
/// Datos de salida retornados al cliente tras operar con un tenant.
/// Nunca expone campos internos como CreadoPor.
/// </summary>
public class EmpresaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
