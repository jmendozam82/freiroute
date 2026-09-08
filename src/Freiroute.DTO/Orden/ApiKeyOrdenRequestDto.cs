using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Datos para gestionar API Keys de integración externa (HU-023).
/// La clave crudo (frk_live_{uuid}) se muestra solo una vez al crear.
/// Se almacena únicamente como hash bcrypt.
/// </summary>
[SwaggerSchema(Description = "Datos para crear una API Key de integración")]
public class ApiKeyOrdenRequestDto
{
    [SwaggerSchema(Description = "Nombre descriptivo de la API Key", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;
}
