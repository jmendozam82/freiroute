using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Ubicacion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del catálogo de ubicaciones (HU-015, ADR-014).
/// Al crear/actualizar con dirección se geocodifica automáticamente
/// (fail-soft: si falla, se guarda sin coordenadas — CA-03, nunca bloquea).
/// El ajuste manual de coordenadas es CA-05.
/// </summary>
public class UbicacionService : IUbicacionService
{
    private readonly IUbicacionRepository _ubicacionRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IValidator<UbicacionRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<UbicacionService> _logger;

    public UbicacionService(
        IUbicacionRepository ubicacionRepository,
        IGeocodingService geocodingService,
        IValidator<UbicacionRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<UbicacionService> logger)
    {
        _ubicacionRepository = ubicacionRepository;
        _geocodingService = geocodingService;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista paginada de ubicaciones con filtros por tipo y texto.</summary>
    public async Task<PagedResult<UbicacionResponseDto>> GetAllAsync(
        Guid empresaId, string? tipo, string? q, int page, int pageSize)
    {
        var todas = await _ubicacionRepository.GetAllAsync(empresaId, tipo, q);

        var pageNumber = Math.Max(page, 1);
        var size = pageSize <= 0 ? 20 : pageSize;

        var items = todas
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(MapToResponse)
            .ToList();

        return new PagedResult<UbicacionResponseDto>
        {
            Items = items,
            TotalItems = todas.Count(),
            PageNumber = pageNumber,
            PageSize = size
        };
    }

    /// <summary>Obtiene una ubicación por Id dentro de la empresa.</summary>
    public async Task<UbicacionResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var entidad = await _ubicacionRepository.GetByIdAsync(id, empresaId);
        return entidad is null ? null : MapToResponse(entidad);
    }

    /// <summary>Todas las ubicaciones georreferenciadas para el mapa (payload mínimo).</summary>
    public async Task<IEnumerable<UbicacionMapaDto>> GetParaMapaAsync(Guid empresaId)
    {
        var ubicaciones = await _ubicacionRepository.GetGeoreferenciadasAsync(empresaId);
        return ubicaciones.Select(u => new UbicacionMapaDto
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Tipo = u.Tipo,
            Latitud = u.Latitud,
            Longitud = u.Longitud
        });
    }

    /// <summary>
    /// Crea la ubicación. Si recibe coordenadas manuales las usa; si no,
    /// geocodifica la dirección (fail-soft — CA-03).
    /// </summary>
    public async Task<UbicacionResponseDto> CreateAsync(UbicacionRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var entidad = MapToEntity(dto, empresaId);

        // Geocodificación automática fail-soft (CA-02/CA-03): nunca bloquea la respuesta.
        if (!dto.Latitud.HasValue && !dto.Longitud.HasValue &&
            !string.IsNullOrWhiteSpace(dto.Direccion))
        {
            var geo = await _geocodingService.GeocodeAsync(dto.Direccion, dto.Ciudad, dto.Pais);
            if (geo is not null)
            {
                entidad.Latitud = geo.Latitud;
                entidad.Longitud = geo.Longitud;
                entidad.DireccionNormalizada = geo.DireccionNormalizada;
                entidad.Georeferenciada = true;
            }
            else
            {
                _logger.LogWarning(
                    "Geocodificación falló para '{Direccion}' — la ubicación se guarda sin coordenadas (CA-03)",
                    dto.Direccion);
            }
        }

        var id = await _ubicacionRepository.CreateAsync(entidad);

        await _auditoria.RegistrarAsync(
            "ubicaciones", AccionAuditoria.CREATE, empresaId, null,
            "Ubicacion", id, new { nombre = entidad.Nombre, tipo = entidad.Tipo });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("ubicaciones", id);
    }

    /// <summary>
    /// Actualiza la ubicación. Re-geocodifica si cambió la dirección y
    /// no vienen coordenadas manuales (HU-015 CA-02).
    /// </summary>
    public async Task<UbicacionResponseDto> UpdateAsync(
        Guid id, UbicacionRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _ubicacionRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("ubicaciones", id);

        var entidad = MapToEntity(dto, empresaId);
        entidad.Id = id;
        entidad.Georeferenciada = existente.Georeferenciada;
        entidad.Latitud = existente.Latitud;
        entidad.Longitud = existente.Longitud;
        entidad.DireccionNormalizada = existente.DireccionNormalizada;

        // Si trae coordenadas manuales, se sobre-escriben (CA-05).
        if (dto.Latitud.HasValue && dto.Longitud.HasValue)
        {
            entidad.Latitud = dto.Latitud;
            entidad.Longitud = dto.Longitud;
            entidad.Georeferenciada = true;
        }
        else if (CambioDireccion(existente, dto) &&
                 !string.IsNullOrWhiteSpace(dto.Direccion))
        {
            var geo = await _geocodingService.GeocodeAsync(dto.Direccion, dto.Ciudad, dto.Pais);
            if (geo is not null)
            {
                entidad.Latitud = geo.Latitud;
                entidad.Longitud = geo.Longitud;
                entidad.DireccionNormalizada = geo.DireccionNormalizada;
                entidad.Georeferenciada = true;
            }
            else
            {
                entidad.Latitud = null;
                entidad.Longitud = null;
                entidad.Georeferenciada = false;
                _logger.LogWarning(
                    "Geocodificación falló para ubicación {UbicacionId} ('{Direccion}') — se guarda sin coordenadas (CA-03)",
                    id, dto.Direccion);
            }
        }

        var ok = await _ubicacionRepository.UpdateAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("ubicaciones", id);
        }

        await _auditoria.RegistrarAsync(
            "ubicaciones", AccionAuditoria.UPDATE, empresaId, null,
            "Ubicacion", id, new { nombre = entidad.Nombre });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("ubicaciones", id);
    }

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var ok = await _ubicacionRepository.DeactivateAsync(id, empresaId);
        if (!ok)
        {
            throw new NotFoundException("ubicaciones", id);
        }

        await _auditoria.RegistrarAsync(
            "ubicaciones", AccionAuditoria.DEACTIVATE, empresaId, null,
            "Ubicacion", id, null);

        return ok;
    }

    /// <summary>Ajuste manual de coordenadas tras geocodificación inexacta (CA-05).</summary>
    public async Task<UbicacionResponseDto> ActualizarCoordenadasAsync(
        Guid id, ActualizarCoordenadasDto dto, Guid empresaId)
    {
        if (dto.Latitud is < -90 or > 90)
        {
            throw new BusinessException("La latitud debe estar entre -90 y 90.", "UBICACION_LATITUD_INVALIDA");
        }

        if (dto.Longitud is < -180 or > 180)
        {
            throw new BusinessException("La longitud debe estar entre -180 y 180.", "UBICACION_LONGITUD_INVALIDA");
        }

        var ok = await _ubicacionRepository.ActualizarCoordenadasAsync(
            id, empresaId, dto.Latitud, dto.Longitud, null);
        if (!ok)
        {
            throw new NotFoundException("ubicaciones", id);
        }

        await _auditoria.RegistrarAsync(
            "ubicaciones", AccionAuditoria.UPDATE, empresaId, null,
            "Ubicacion", id, new { latitud = dto.Latitud, longitud = dto.Longitud });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("ubicaciones", id);
    }

    /// <summary>
    /// Importación masiva CSV (HU-015 CA-06, ADR-017). Columnas:
    /// Nombre;Código;Tipo;Dirección;País;Departamento;Ciudad;CódigoPostal;
    /// ContactoNombre;ContactoTeléfono;ContactoEmail;Instrucciones.
    /// Las filas inválidas se registran en el log y la importación continúa.
    /// </summary>
    public async Task<int> ImportarCsvAsync(Stream csv, Guid empresaId)
    {
        var importados = 0;
        var fila = 0;

        using var reader = new StreamReader(csv, System.Text.Encoding.UTF8);
        while (await reader.ReadLineAsync() is { } linea)
        {
            fila++;
            if (fila == 1)
            {
                continue; // Cabecera.
            }

            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            try
            {
                var campos = linea.Split(';');
                if (campos.Length < 3 || string.IsNullOrWhiteSpace(campos[0]))
                {
                    _logger.LogWarning("CSV ubicaciones: fila {Fila} omitida (datos incompletos)", fila);
                    continue;
                }

                var dto = new UbicacionRequestDto
                {
                    Nombre = campos[0].Trim(),
                    Codigo = ValorOpcional(campos, 1),
                    Tipo = ValorOpcional(campos, 2) ?? TipoUbicacion.Otro,
                    Direccion = ValorOpcional(campos, 3),
                    Pais = ValorOpcional(campos, 4) ?? "Nicaragua",
                    Departamento = ValorOpcional(campos, 5),
                    Ciudad = ValorOpcional(campos, 6),
                    CodigoPostal = ValorOpcional(campos, 7),
                    ContactoNombre = ValorOpcional(campos, 8),
                    ContactoTelefono = ValorOpcional(campos, 9),
                    ContactoEmail = ValorOpcional(campos, 10),
                    Instrucciones = ValorOpcional(campos, 11)
                };

                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    _logger.LogWarning(
                        "CSV ubicaciones: fila {Fila} omitida ({Errores})",
                        fila, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                    continue;
                }

                await _ubicacionRepository.CreateAsync(MapToEntity(dto, empresaId));
                importados++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSV ubicaciones: fila {Fila} omitida por error inesperado", fila);
            }
        }

        await _auditoria.RegistrarAsync(
            "ubicaciones", AccionAuditoria.CREATE, empresaId, null,
            "Ubicacion", null, new { importados, archivo = "csv" });

        return importados;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(UbicacionRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private static bool CambioDireccion(Ubicacion existente, UbicacionRequestDto dto)
    {
        return !string.Equals(existente.Direccion ?? string.Empty, dto.Direccion ?? string.Empty,
            StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(existente.Ciudad ?? string.Empty, dto.Ciudad ?? string.Empty,
                   StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(existente.Pais ?? string.Empty, dto.Pais ?? string.Empty,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string? ValorOpcional(string[] campos, int indice) =>
        campos.Length > indice && !string.IsNullOrWhiteSpace(campos[indice])
            ? campos[indice].Trim()
            : null;

    private static Ubicacion MapToEntity(UbicacionRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim(),
        Tipo = dto.Tipo,
        Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim(),
        Pais = dto.Pais,
        Departamento = NormalizarOpcional(dto.Departamento),
        Ciudad = NormalizarOpcional(dto.Ciudad),
        CodigoPostal = NormalizarOpcional(dto.CodigoPostal),
        Latitud = dto.Latitud,
        Longitud = dto.Longitud,
        Georeferenciada = dto.Latitud.HasValue && dto.Longitud.HasValue,
        ContactoNombre = NormalizarOpcional(dto.ContactoNombre),
        ContactoTelefono = NormalizarOpcional(dto.ContactoTelefono),
        ContactoEmail = NormalizarOpcional(dto.ContactoEmail),
        HorarioApertura = ParseHora(dto.HorarioApertura),
        HorarioCierre = ParseHora(dto.HorarioCierre),
        TiempoServicioMin = dto.TiempoServicioMin,
        Instrucciones = NormalizarOpcional(dto.Instrucciones),
        Activo = true
    };

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static TimeOnly? ParseHora(string? hora) =>
        string.IsNullOrWhiteSpace(hora)
            ? null
            : TimeOnly.TryParse(hora, out var time) ? time : null;

    private static UbicacionResponseDto MapToResponse(Ubicacion u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Codigo = u.Codigo,
        Tipo = u.Tipo,
        TipoLabel = TipoLabel(u.Tipo),
        Direccion = u.Direccion,
        Pais = u.Pais,
        Departamento = u.Departamento,
        Ciudad = u.Ciudad,
        Latitud = u.Latitud,
        Longitud = u.Longitud,
        Georeferenciada = u.Georeferenciada,
        DireccionNormalizada = u.DireccionNormalizada,
        ContactoNombre = u.ContactoNombre,
        ContactoTelefono = u.ContactoTelefono,
        TiempoServicioMin = u.TiempoServicioMin,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };

    private static string TipoLabel(string tipo) => tipo switch
    {
        TipoUbicacion.Almacen => "Almacén",
        TipoUbicacion.Cliente => "Cliente",
        TipoUbicacion.Puerto => "Puerto",
        TipoUbicacion.Aeropuerto => "Aeropuerto",
        TipoUbicacion.Terminal => "Terminal",
        TipoUbicacion.CruceFrontera => "Cruce fronterizo",
        TipoUbicacion.PuntoRecarga => "Punto de recarga",
        _ => "Otro"
    };
}