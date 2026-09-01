namespace Freiroute.DTO.Empresa;

/// <summary>
/// Datos de entrada para crear un nuevo tenant.
/// El slug se deriva automáticamente si no se proporciona.
/// </summary>
public class EmpresaRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = "";
    public string Plan { get; set; } = "starter";
}
