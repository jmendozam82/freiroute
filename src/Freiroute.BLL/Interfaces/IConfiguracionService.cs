using Freiroute.DTO.Configuracion;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de configuración general del tenant (HU-014).
/// Se lee/escribe contra la tabla 'empresas' — no tiene tabla propia.
/// </summary>
public interface IConfiguracionService
{
    /// <summary>Obtiene la configuración general del tenant.</summary>
    Task<ConfiguracionResponseDto> GetAsync(Guid empresaId);

    /// <summary>Actualiza la configuración general del tenant. Registra auditoría.</summary>
    Task<ConfiguracionResponseDto> UpdateAsync(ConfiguracionRequestDto dto, Guid empresaId);

    /// <summary>
    /// Sube el logo a Supabase Storage y retorna la signed URL.
    /// Bucket privado 'logos-tenants', path {empresa_id}/logo.{ext}, signed URL 24h (HU-014).
    /// </summary>
    Task<string> UpdateLogoAsync(Guid empresaId, Stream logo, string contentType);

    /// <summary>Elimina el logo actual del tenant.</summary>
    Task<bool> DeleteLogoAsync(Guid empresaId);

    /// <summary>Obtiene los prefijos y consecutivos de numeración actuales.</summary>
    Task<NumeracionResponseDto> GetNumeracionAsync(Guid empresaId);

    /// <summary>
    /// Actualiza los prefijos de numeración (embarques, órdenes, carta de porte).
    /// Los consecutivos no se editan — son autoincrementales (HU-014 CA-05).
    /// </summary>
    Task<NumeracionResponseDto> UpdateNumeracionAsync(NumeracionRequestDto dto, Guid empresaId);
}
