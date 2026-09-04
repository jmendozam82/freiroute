using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada del callback OAuth (HU-004).
/// El frontend redirige al flujo de Supabase Auth y recibe el access_token.
/// </summary>
[SwaggerSchema(Description = "DTO del callback OAuth — recibe el token de Supabase para resolver el usuario")]
public class OAuthCallbackRequestDto
{
    [SwaggerSchema(Description = "Proveedor OAuth: 'google' o 'microsoft'", Nullable = false)]
    public string Provider { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Access token devuelto por Supabase Auth para el proveedor", Nullable = false)]
    public string SupabaseToken { get; set; } = string.Empty;
}
