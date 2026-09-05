using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Configuracion;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de configuración general del tenant (HU-014).
/// Lee/escribe sobre la tabla 'empresas' (no tiene tabla propia).
/// El logo se almacena en Supabase Storage (bucket privado) con signed URLs.
/// </summary>
public class ConfiguracionService : IConfiguracionService
{
    private static readonly string BucketLogos = "logos-tenants";

    private readonly IConfiguracionRepository _configRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IStorageService _storageService;
    private readonly IValidator<ConfiguracionRequestDto> _configValidator;
    private readonly IValidator<NumeracionRequestDto> _numeracionValidator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<ConfiguracionService> _logger;

    public ConfiguracionService(
        IConfiguracionRepository configRepository,
        IEmpresaRepository empresaRepository,
        IStorageService storageService,
        IValidator<ConfiguracionRequestDto> configValidator,
        IValidator<NumeracionRequestDto> numeracionValidator,
        IAuditoriaService auditoria,
        ILogger<ConfiguracionService> logger)
    {
        _configRepository = configRepository;
        _empresaRepository = empresaRepository;
        _storageService = storageService;
        _configValidator = configValidator;
        _numeracionValidator = numeracionValidator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene la configuración general del tenant.</summary>
    public async Task<ConfiguracionResponseDto> GetAsync(Guid empresaId)
    {
        var empresa = await _configRepository.GetConfiguracionAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        return MapToResponse(empresa);
    }

    /// <summary>Actualiza la configuración general del tenant (HU-014 CA-04).</summary>
    public async Task<ConfiguracionResponseDto> UpdateAsync(
        ConfiguracionRequestDto dto, Guid empresaId)
    {
        var validation = await _configValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var ok = await _configRepository.UpdateConfiguracionAsync(
            empresaId,
            dto.Nombre, dto.RucNit, dto.Direccion, dto.Telefono,
            dto.Industria, dto.SitioWeb,
            dto.ColorPrimario, dto.ColorSecundario,
            dto.Moneda, dto.ZonaHoraria, dto.FormatoFecha,
            dto.EmailRemitente, dto.NombreRemitente);

        if (!ok)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        await _auditoria.RegistrarAsync(
            "configuracion", AccionAuditoria.UPDATE, empresaId, null,
            "configuracion", empresaId,
            new { nombre = dto.Nombre, moneda = dto.Moneda });

        return await GetAsync(empresaId);
    }

    /// <summary>
    /// Sube el logo del tenant a Supabase Storage y retorna la signed URL de 24 h.
    /// Bucket privado 'logos-tenants', path {empresa_id}/logo.{ext} (HU-014).
    /// </summary>
    public async Task<string> UpdateLogoAsync(Guid empresaId, Stream logo, string contentType)
    {
        if (logo is null || logo.Length == 0)
        {
            throw new BusinessException("El archivo del logo está vacío.");
        }

        var extension = ObtenerExtension(contentType);

        var objectPath = await _storageService.UploadAsync(
            BucketLogos, empresaId.ToString(), $"logo{extension}", logo, contentType);

        var signedUrl = await _storageService.GetSignedUrlAsync(
            BucketLogos, objectPath, 24 * 3600);

        if (string.IsNullOrEmpty(signedUrl))
        {
            throw new BusinessException("No se pudo generar la URL firmada del logo.");
        }

        await _configRepository.UpdateLogoUrlAsync(empresaId, signedUrl);

        await _auditoria.RegistrarAsync(
            "configuracion", AccionAuditoria.UPDATE, empresaId, null,
            "logo", empresaId, new { path = objectPath });

        return signedUrl;
    }

    /// <summary>Elimina el logo actual del tenant (soft — solo borra la referencia y el objeto).</summary>
    public async Task<bool> DeleteLogoAsync(Guid empresaId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null || string.IsNullOrEmpty(empresa.LogoUrl))
        {
            return true; // No hay logo que eliminar.
        }

        // Extraer el path del objeto desde la signed URL (parte después del bucket).
        var storedMatch = System.Text.RegularExpressions.Regex.Match(
            empresa.LogoUrl, $"{BucketLogos}/([^?]+)");
        if (storedMatch.Success)
        {
            await _storageService.DeleteAsync(BucketLogos, storedMatch.Groups[1].Value);
        }

        var ok = await _configRepository.UpdateLogoUrlAsync(empresaId, null);
        await _auditoria.RegistrarAsync(
            "configuracion", AccionAuditoria.UPDATE, empresaId, null,
            "logo", empresaId, new { accion = "eliminar_logo" });

        return ok;
    }

    /// <summary>Obtiene los prefijos y consecutivos de numeración del tenant.</summary>
    public async Task<NumeracionResponseDto> GetNumeracionAsync(Guid empresaId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        return MapToNumeracion(empresa);
    }

    /// <summary>
    /// Actualiza los prefijos de numeración (embarques, órdenes, carta de porte).
    /// Los consecutivos no se editan — son autoincrementales (HU-014 CA-05).
    /// </summary>
    public async Task<NumeracionResponseDto> UpdateNumeracionAsync(
        NumeracionRequestDto dto, Guid empresaId)
    {
        var validation = await _numeracionValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var ok = await _configRepository.UpdateNumeracionAsync(
            empresaId, dto.PrefijoEmbarque, dto.PrefijoOrden, dto.PrefijoCartaPorte);

        if (!ok)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        await _auditoria.RegistrarAsync(
            "configuracion", AccionAuditoria.UPDATE, empresaId, null,
            "numeracion", empresaId, new { dto });

        return await GetNumeracionAsync(empresaId);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static string ObtenerExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/svg+xml" => ".svg",
        "image/webp" => ".webp",
        "image/jpeg" or "image/jpg" => ".jpg",
        _ => ".png"
    };

    private static ConfiguracionResponseDto MapToResponse(Entity.Empresa e) => new()
    {
        EmpresaId = e.Id,
        Nombre = e.Nombre,
        RucNit = e.RucNit,
        Direccion = e.Direccion,
        Telefono = e.Telefono,
        Industria = e.Industria,
        SitioWeb = e.SitioWeb,
        LogoUrl = e.LogoUrl,
        ColorPrimario = e.ColorPrimario,
        ColorSecundario = e.ColorSecundario,
        Moneda = e.MonedaPrincipal,
        ZonaHoraria = e.ZonaHoraria,
        FormatoFecha = e.FormatoFecha,
        EmailRemitente = e.EmailRemitente,
        NombreRemitente = e.NombreRemitente,
        ModosTransporteActivos = e.ModosTransporteActivos?.ToList() ?? [],
        OnboardingCompletado = e.OnboardingCompletado
    };

    private static NumeracionResponseDto MapToNumeracion(Entity.Empresa e) => new()
    {
        PrefijoEmbarque = e.PrefijoEmbarque,
        ConsecutivoEmbarque = e.ConsecutivoEmbarque,
        PrefijoOrden = e.PrefijoOrden,
        ConsecutivoOrden = e.ConsecutivoOrden,
        PrefijoCartaPorte = e.PrefijoCartaPorte,
        ConsecutivoCartaPorte = e.ConsecutivoCartaPorte
    };
}
