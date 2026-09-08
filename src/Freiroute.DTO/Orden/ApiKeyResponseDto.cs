using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Orden;

/// <summary>
/// Respuesta de una API Key de integración (HU-023).
/// El campo RawKey solo tiene valor en la respuesta de creación —
/// nunca se almacena ni se retorna en consultas posteriores.
/// </summary>
[SwaggerSchema(Description = "Respuesta de una API Key de integración")]
public class ApiKeyResponseDto
{
    [SwaggerSchema(Description = "ID único de la API Key")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Nombre descriptivo de la API Key")]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Valor crudo de la clave (frk_live_{uuid}) — solo visible una vez al crear")]
    public string? RawKey { get; set; }

    [SwaggerSchema(Description = "Prefijo enmascarado para identificación (frk_live_****)")]
    public string? ClaveEnmascarada { get; set; }

    [SwaggerSchema(Description = "Si la API Key está activa")]
    public bool Activo { get; set; }

    [SwaggerSchema(Description = "Fecha del último uso exitoso")]
    public DateTime? UltimoUso { get; set; }

    [SwaggerSchema(Description = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}
