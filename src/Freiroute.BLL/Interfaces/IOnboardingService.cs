using Freiroute.DTO.Onboarding;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del wizard de onboarding multi-paso (HU-012, ADR-010).
/// El progreso se persiste en tabla 'empresas' (onboarding_paso_actual, onboarding_completado).
/// </summary>
public interface IOnboardingService
{
    /// <summary>Obtiene el estado actual del onboarding y datos guardados para pre-llenar.</summary>
    Task<OnboardingEstadoResponseDto> GetEstadoAsync(Guid empresaId);

    /// <summary>Guarda el Paso 1: datos de la empresa.</summary>
    Task<bool> GuardarPaso1Async(OnboardingPaso1RequestDto dto, Guid empresaId);

    /// <summary>Guarda el Paso 2: identidad visual (colores + URL del logo ya subido).</summary>
    Task<bool> GuardarPaso2Async(OnboardingPaso2RequestDto dto, Guid empresaId);

    /// <summary>Guarda el Paso 3: configuración operativa (moneda, zona horaria, prefijos, modos).</summary>
    Task<bool> GuardarPaso3Async(OnboardingPaso3RequestDto dto, Guid empresaId);

    /// <summary>
    /// Guarda el Paso 4: datos del primer administrador.
    /// Si CambiarPassword = true → actualiza la contraseña del usuario en Supabase.
    /// </summary>
    Task<bool> GuardarPaso4Async(OnboardingPaso4RequestDto dto,
        Guid empresaId, Guid usuarioId);

    /// <summary>
    /// Guarda el Paso 5: envío de invitaciones al equipo (máx 5).
    /// Si la lista está vacía, se omite (skip).
    /// </summary>
    Task<bool> GuardarPaso5Async(OnboardingPaso5RequestDto dto,
        Guid empresaId, Guid invitadoPorId);

    /// <summary>
    /// Marca el onboarding como completado: onboarding_paso_actual = 5,
    /// onboarding_completado = true (HU-012 CA-08).
    /// </summary>
    Task<bool> CompletarAsync(Guid empresaId);

    /// <summary>
    /// Sube el logo del tenant a Supabase Storage y retorna la URL.
    /// Bucket privado 'logos-tenants', path {empresa_id}/logo.{ext} (HU-014).
    /// </summary>
    Task<string> GuardarLogoAsync(Guid empresaId, Stream logo, string extension);
}
