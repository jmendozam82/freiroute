using Swashbuckle.AspNetCore.Annotations;
using Freiroute.DTO.Usuario;

namespace Freiroute.DTO.Onboarding;

/// <summary>
/// Datos de entrada del Paso 5 del onboarding: invitar equipo (HU-012 CA-06).
/// Máximo 5 invitaciones; la lista puede estar vacía (skip).
/// </summary>
[SwaggerSchema(Description = "DTO del Paso 5 del onboarding — invitación de equipo")]
public class OnboardingPaso5RequestDto
{
    [SwaggerSchema(Description = "Lista de invitaciones por email (máx 5). Puede estar vacía para saltar.")]
    public List<InvitacionRequestDto> Invitaciones { get; set; } = [];
}
