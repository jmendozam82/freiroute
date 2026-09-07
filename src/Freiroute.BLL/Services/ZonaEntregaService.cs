using System.Text.Json;
using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Zona;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de zonas de entrega (HU-016, ADR-018).
/// La verificación point-in-polygon para zonas tipo POLIGONO se
/// implementa aquí (ray casting); para los demás métodos se compara
/// la dirección reverso-geocodificada contra las listas de la zona.
/// </summary>
public class ZonaEntregaService : IZonaEntregaService
{
    private readonly IZonaEntregaRepository _zonaRepository;
    private readonly IUbicacionRepository _ubicacionRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IValidator<ZonaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<ZonaEntregaService> _logger;

    public ZonaEntregaService(
        IZonaEntregaRepository zonaRepository,
        IUbicacionRepository ubicacionRepository,
        IGeocodingService geocodingService,
        IValidator<ZonaRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<ZonaEntregaService> logger)
    {
        _zonaRepository = zonaRepository;
        _ubicacionRepository = ubicacionRepository;
        _geocodingService = geocodingService;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista todas las zonas activas de la empresa con su total de ubicaciones.</summary>
    public async Task<IEnumerable<ZonaResponseDto>> GetAllAsync(Guid empresaId)
    {
        var zonas = await _zonaRepository.GetAllAsync(empresaId);
        var responses = new List<ZonaResponseDto>();

        foreach (var zona in zonas)
        {
            responses.Add(await MapToResponse(zona, empresaId));
        }

        return responses;
    }

    /// <summary>Obtiene una zona por Id dentro de la empresa.</summary>
    public async Task<ZonaResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var zona = await _zonaRepository.GetByIdAsync(id, empresaId);
        return zona is null ? null : await MapToResponse(zona, empresaId);
    }

    /// <summary>Registra una zona nueva con su método de definición.</summary>
    public async Task<ZonaResponseDto> CreateAsync(ZonaRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new FluentValidation.ValidationException(validation.Errors);
        }

        var entidad = MapToEntity(dto, empresaId);
        var id = await _zonaRepository.CreateAsync(entidad);

        await _auditoria.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.CREATE, empresaId, null,
            "ZonaEntrega", id, new { nombre = entidad.Nombre, tipoDefinicion = entidad.TipoDefinicion });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("zonas_entrega", id);
    }

    /// <summary>Actualiza la zona (nombre, color, método de definición, listas).</summary>
    public async Task<ZonaResponseDto> UpdateAsync(
        Guid id, ZonaRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new FluentValidation.ValidationException(validation.Errors);
        }

        var existente = await _zonaRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("zonas_entrega", id);

        var entidad = MapToEntity(dto, empresaId);
        entidad.Id = id;
        entidad.FechaCreacion = existente.FechaCreacion;

        var ok = await _zonaRepository.UpdateAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("zonas_entrega", id);
        }

        await _auditoria.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.UPDATE, empresaId, null,
            "ZonaEntrega", id, new { nombre = entidad.Nombre });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("zonas_entrega", id);
    }

    /// <summary>
    /// Soft delete (ADR-005). Valida que la zona no tenga tarifas
    /// activas asociadas (HU-016 CA-06).
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var zona = await _zonaRepository.GetByIdAsync(id, empresaId)
                   ?? throw new NotFoundException("zonas_entrega", id);

        if (await _zonaRepository.TieneTarifasActivasAsync(id, empresaId))
        {
            throw new BusinessException(
                $"La zona '{zona.Nombre}' está referenciada por tarifas activas y no puede desactivarse.",
                "ZONA_TIENE_TARIFAS_ACTIVAS");
        }

        var ok = await _zonaRepository.DeactivateAsync(id, empresaId);
        await _auditoria.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.DEACTIVATE, empresaId, null,
            "ZonaEntrega", id, null);

        return ok;
    }

    /// <summary>Asigna ubicaciones a la zona (relación muchos-a-muchos, CA-04).</summary>
    public async Task AsignarUbicacionesAsync(
        Guid zonaId, IEnumerable<Guid> ubicacionIds, Guid empresaId)
    {
        var zona = await _zonaRepository.GetByIdAsync(zonaId, empresaId)
                   ?? throw new NotFoundException("zonas_entrega", zonaId);

        foreach (var ubicacionId in ubicacionIds.Distinct())
        {
            await _zonaRepository.AsignarUbicacionAsync(zonaId, ubicacionId, empresaId);
        }

        await _auditoria.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.UPDATE, empresaId, null,
            "ZonaEntrega", zonaId,
            new { accion = "asignar_ubicaciones", count = ubicacionIds.Distinct().Count() });
    }

    /// <summary>Desasigna una ubicación de la zona.</summary>
    public async Task DesasignarUbicacionAsync(Guid zonaId, Guid ubicacionId, Guid empresaId)
    {
        var zona = await _zonaRepository.GetByIdAsync(zonaId, empresaId)
                   ?? throw new NotFoundException("zonas_entrega", zonaId);

        await _zonaRepository.DesasignarUbicacionAsync(zonaId, ubicacionId, empresaId);

        await _auditoria.RegistrarAsync(
            "zonas_entrega", AccionAuditoria.UPDATE, empresaId, null,
            "ZonaEntrega", zonaId,
            new { accion = "desasignar_ubicacion", ubicacionId });
    }

    /// <summary>
    /// Retorna las zonas donde cae el punto (CA-05). Para POLIGONO usa
    /// point-in-polygon manual (ray casting, ADR-018); para los demás
    /// métodos compara la dirección reverso-geocodificada contra las listas.
    /// </summary>
    public async Task<IEnumerable<ZonaResponseDto>> VerificarPertenenciaAsync(
        double latitud, double longitud, Guid empresaId)
    {
        var candidatas = await _zonaRepository.GetZonasPorPuntoAsync(latitud, longitud, empresaId);

        // Reverse geocoding para las zonas por listas (falla-soft → null).
        var direccion = await _geocodingService.ReverseGeocodeAsync(latitud, longitud);

        var resultado = new List<ZonaResponseDto>();
        foreach (var zona in candidatas)
        {
            if (ZonaContienePunto(zona, latitud, longitud, direccion))
            {
                resultado.Add(await MapToResponse(zona, empresaId));
            }
        }

        return resultado;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private bool ZonaContienePunto(ZonaEntrega zona, double lat, double lng, string? direccion)
    {
        try
        {
            return zona.TipoDefinicion switch
            {
                TipoZonaDefinicion.Poligono =>
                    EsPuntoEnPoligono(zona.PoligonoGeoJson, lat, lng),

                // La dirección reverso-geocodificada contiene la ciudad,
                // departamento, país o código postal de la lista (ignore case).
                TipoZonaDefinicion.CodigosPostales =>
                    ContieneAlguno(direccion, zona.CodigosPostales),
                TipoZonaDefinicion.Ciudades =>
                    ContieneAlguno(direccion, zona.Ciudades),
                TipoZonaDefinicion.Departamentos =>
                    ContieneAlguno(direccion, zona.Departamentos),
                TipoZonaDefinicion.Paises =>
                    ContieneAlguno(direccion, zona.Paises),
                _ => false
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Zona {ZonaId} con GeoJSON inválido al verificar pertenencia", zona.Id);
            return false;
        }
    }

    private static bool ContieneAlguno(string? direccion, string[] valores)
    {
        if (string.IsNullOrWhiteSpace(direccion) || valores.Length == 0)
        {
            return false;
        }

        return valores.Any(v =>
            !string.IsNullOrWhiteSpace(v) &&
            direccion.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Point-in-polygon por ray casting (ADR-018). Soporta GeoJSON
    /// Polygon y MultiPolygon. Coordenadas GeoJSON: [longitud, latitud].
    /// </summary>
    internal static bool EsPuntoEnPoligono(string? geoJson, double lat, double lng)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(geoJson);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        if (type is not ("Polygon" or "MultiPolygon") ||
            !root.TryGetProperty("coordinates", out var coords) ||
            coords.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        if (type == "Polygon")
        {
            return PuntoEnPoligonoGeoJson(coords, lat, lng);
        }

        foreach (var poligono in coords.EnumerateArray())
        {
            if (PuntoEnPoligonoGeoJson(poligono, lat, lng))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PuntoEnPoligonoGeoJson(JsonElement polygon, double lat, double lng)
    {
        if (polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() == 0)
        {
            return false;
        }

        var anillo = polygon[0]; // Solo el anillo exterior (bypass de huecos).
        if (anillo.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var puntos = new List<(double Lat, double Lng)>();
        foreach (var punto in anillo.EnumerateArray())
        {
            if (punto.ValueKind != JsonValueKind.Array || punto.GetArrayLength() < 2)
            {
                return false;
            }

            // GeoJSON: [longitud, latitud].
            if (!punto[0].TryGetDouble(out var lngVal) || !punto[1].TryGetDouble(out var latVal))
            {
                return false;
            }

            puntos.Add((latVal, lngVal));
        }

        if (puntos.Count < 4)
        {
            return false;
        }

        // Ray casting: número impar de cruces del rayo horizontal ⇒ dentro.
        var dentro = false;
        for (int i = 0, j = puntos.Count - 1; i < puntos.Count; j = i++)
        {
            var (latI, lngI) = puntos[i];
            var (latJ, lngJ) = puntos[j];

            if ((latI > lat) != (latJ > lat) &&
                lng < (lngJ - lngI) * (lat - latI) / (latJ - latI) + lngI)
            {
                dentro = !dentro;
            }
        }

        return dentro;
    }

    private async Task<ZonaResponseDto> MapToResponse(ZonaEntrega zona, Guid empresaId)
    {
        var ubicaciones = await _ubicacionRepository.GetByZonaAsync(zona.Id, empresaId);

        return new ZonaResponseDto
        {
            Id = zona.Id,
            Nombre = zona.Nombre,
            Codigo = zona.Codigo,
            Descripcion = zona.Descripcion,
            ColorHex = zona.ColorHex,
            TipoDefinicion = zona.TipoDefinicion,
            PoligonoGeoJson = zona.PoligonoGeoJson,
            CodigosPostales = zona.CodigosPostales,
            Ciudades = zona.Ciudades,
            Departamentos = zona.Departamentos,
            Paises = zona.Paises,
            TotalUbicaciones = ubicaciones.Count(),
            Activo = zona.Activo,
            FechaCreacion = zona.FechaCreacion
        };
    }

    private static ZonaEntrega MapToEntity(ZonaRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Codigo = dto.Codigo.Trim(),
        Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
        ColorHex = dto.ColorHex,
        TipoDefinicion = dto.TipoDefinicion,
        PoligonoGeoJson = string.IsNullOrWhiteSpace(dto.PoligonoGeoJson) ? null : dto.PoligonoGeoJson,
        CodigosPostales = dto.CodigosPostales.Select(c => c.Trim()).ToArray(),
        Ciudades = dto.Ciudades.Select(c => c.Trim()).ToArray(),
        Departamentos = dto.Departamentos.Select(d => d.Trim()).ToArray(),
        Paises = dto.Paises.Select(p => p.Trim()).ToArray(),
        Activo = true
    };
}