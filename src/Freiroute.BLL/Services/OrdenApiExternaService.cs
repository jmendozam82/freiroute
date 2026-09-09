using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;

namespace Freiroute.BLL.Services;

public class OrdenApiExternaService : IOrdenApiExternaService
{
    private readonly IApiKeyTenantRepository _apiKeyRepository;
    private readonly IOrdenService _ordenService;
    private readonly IAuditoriaRepository _auditoriaRepository;

    public OrdenApiExternaService(
        IApiKeyTenantRepository apiKeyRepository,
        IOrdenService ordenService,
        IAuditoriaRepository auditoriaRepository)
    {
        _apiKeyRepository = apiKeyRepository;
        _ordenService = ordenService;
        _auditoriaRepository = auditoriaRepository;
    }

    public async Task<Guid?> ValidarApiKeyAsync(string apiKey)
    {
        // Actually the middleware provides the raw API key
        // As per ADR and specifications, we need to lookup all active API keys or the one matching if it's cached.
        // Wait, GetByClaveHashAsync is mentioned in the prompt: "ApiKeyTenantRepository.GetByClaveHashAsync fetches all active keys and verifies against the provided raw key."
        // Wait! If GetByClaveHashAsync fetches all and verifies against the raw key, let's use it.
        var entity = await _apiKeyRepository.GetByClaveHashAsync(apiKey);
        if (entity != null)
        {
            await _apiKeyRepository.ActualizarUltimoUsoAsync(entity.Id, entity.EmpresaId);
            return entity.EmpresaId;
        }
        return null;
    }

    public async Task<OrdenResponseDto> CrearOrdenDesdeApiAsync(OrdenRequestDto dto, Guid empresaId)
    {
        // HU-023 CA-05: La orden se crea con origen_creacion = 'API'
        // Using an empty/system Guid for user
        var systemUserId = Guid.Empty; // Ideally this should be a system user or the API key user id, but we don't have user id here
        
        var result = await _ordenService.CreateAsync(dto, empresaId, systemUserId, OrigenCreacion.Api);
        
        // (G-10) El origen 'API' se pasa al servicio — ver XML docs de IOrdenService.CreateAsync.
        
        return result;
    }

    public async Task<ApiKeyResponseDto> GenerarApiKeyAsync(ApiKeyOrdenRequestDto dto, Guid empresaId)
    {
        var rawKey = $"frk_live_{Guid.NewGuid():N}";
        var hash = BCrypt.Net.BCrypt.HashPassword(rawKey);

        var entity = new ApiKeyTenant
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            ClaveHash = hash,
            Activo = true
        };

        var id = await _apiKeyRepository.CreateAsync(entity);

        await _auditoriaRepository.RegistrarAsync(new AuditoriaActividad
        {
            EmpresaId = empresaId,
            UsuarioId = Guid.Empty, // We should get the user ID, but it's not in the method signature (wait, no user id parameter in GenerarApiKeyAsync)
            Modulo = "configuracion",
            Accion = "CREATE",
            EntidadId = id,
            Detalles = JsonSerializer.Serialize(new { nombre = dto.Nombre })
        });

        return new ApiKeyResponseDto
        {
            Id = id,
            Nombre = dto.Nombre,
            RawKey = rawKey, // Only visible once
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<ApiKeyResponseDto>> GetApiKeysAsync(Guid empresaId)
    {
        var keys = await _apiKeyRepository.GetAllAsync(empresaId);
        return keys.Select(k => new ApiKeyResponseDto
        {
            Id = k.Id,
            Nombre = k.Nombre,
            Activo = k.Activo,
            UltimoUso = k.UltimoUso,
            FechaCreacion = k.FechaCreacion
        });
    }

    public async Task<bool> DesactivarApiKeyAsync(Guid apiKeyId, Guid empresaId)
    {
        var result = await _apiKeyRepository.DeactivateAsync(apiKeyId, empresaId);
        if (result)
        {
            await _auditoriaRepository.RegistrarAsync(new AuditoriaActividad
            {
                EmpresaId = empresaId,
                UsuarioId = Guid.Empty,
                Modulo = "configuracion",
                Accion = "DEACTIVATE",
                EntidadId = apiKeyId,
                Detalles = JsonSerializer.Serialize(new { apiKeyId })
            });
        }
        return result;
    }
}
