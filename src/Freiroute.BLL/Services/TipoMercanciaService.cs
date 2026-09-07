using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Mercancia;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del catálogo de tipos de mercancía (HU-017).
/// Validaciones HAZMAT en el validator (clase ONU, refrigeración);
/// la importación CSV es ADR-017 (fail-soft, filas inválidas al log).
/// </summary>
public class TipoMercanciaService : ITipoMercanciaService
{
    private readonly ITipoMercanciaRepository _mercanciaRepository;
    private readonly IValidator<TipoMercanciaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<TipoMercanciaService> _logger;

    public TipoMercanciaService(
        ITipoMercanciaRepository mercanciaRepository,
        IValidator<TipoMercanciaRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<TipoMercanciaService> logger)
    {
        _mercanciaRepository = mercanciaRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista tipos de mercancía con filtro opcional de peligrosas (CA-07).</summary>
    public async Task<IEnumerable<TipoMercanciaResponseDto>> GetAllAsync(
        Guid empresaId, bool? soloPeligrosas = null)
    {
        var tipos = await _mercanciaRepository.GetAllAsync(empresaId, soloPeligrosas);
        return tipos.Select(MapToResponse);
    }

    /// <summary>Obtiene un tipo de mercancía por Id dentro de la empresa.</summary>
    public async Task<TipoMercanciaResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var tipo = await _mercanciaRepository.GetByIdAsync(id, empresaId);
        return tipo is null ? null : MapToResponse(tipo);
    }

    /// <summary>Crea el tipo de mercancía con validaciones HAZMAT.</summary>
    public async Task<TipoMercanciaResponseDto> CreateAsync(
        TipoMercanciaRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var entidad = MapToEntity(dto, empresaId);
        var id = await _mercanciaRepository.CreateAsync(entidad);

        await _auditoria.RegistrarAsync(
            "tipos_mercancia", AccionAuditoria.CREATE, empresaId, null,
            "TipoMercancia", id, new { nombre = entidad.Nombre, esPeligroso = entidad.EsPeligroso });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("tipos_mercancia", id);
    }

    /// <summary>Actualiza el tipo de mercancía con validaciones HAZMAT.</summary>
    public async Task<TipoMercanciaResponseDto> UpdateAsync(
        Guid id, TipoMercanciaRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _mercanciaRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("tipos_mercancia", id);

        var entidad = MapToEntity(dto, empresaId);
        entidad.Id = id;
        entidad.FechaCreacion = existente.FechaCreacion;

        var ok = await _mercanciaRepository.UpdateAsync(entidad);
        if (!ok)
        {
            throw new NotFoundException("tipos_mercancia", id);
        }

        await _auditoria.RegistrarAsync(
            "tipos_mercancia", AccionAuditoria.UPDATE, empresaId, null,
            "TipoMercancia", id, new { nombre = entidad.Nombre });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("tipos_mercancia", id);
    }

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var tipo = await _mercanciaRepository.GetByIdAsync(id, empresaId)
                   ?? throw new NotFoundException("tipos_mercancia", id);

        var ok = await _mercanciaRepository.DeactivateAsync(id, empresaId);

        await _auditoria.RegistrarAsync(
            "tipos_mercancia", AccionAuditoria.DEACTIVATE, empresaId, null,
            "TipoMercancia", id, new { nombre = tipo.Nombre });

        return ok;
    }

    /// <summary>
    /// Importación masiva CSV (HU-017 CA-06, ADR-017). Columnas:
    /// Nombre;Código;Descripción;Categoría;ClasePeligrosidad;CódigoONU;
    /// CódigoHS;PesoMaxKg;VolumenMaxM3;RequiereRefrigeracion;EsFragil;
    /// EsPeligroso;EsPerecedero;EsSobredimensionado;RequiereFumigacion.
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
                if (campos.Length < 1 || string.IsNullOrWhiteSpace(campos[0]))
                {
                    _logger.LogWarning("CSV tipos mercancía: fila {Fila} omitida (nombre vacío)", fila);
                    continue;
                }

                var dto = new TipoMercanciaRequestDto
                {
                    Nombre = campos[0].Trim(),
                    Codigo = ValorOpcional(campos, 1),
                    Descripcion = ValorOpcional(campos, 2),
                    Categoria = ValorOpcional(campos, 3),
                    ClasePeligrosidad = ValorOpcional(campos, 4),
                    CodigoOnu = ValorOpcional(campos, 5),
                    CodigoHs = ValorOpcional(campos, 6),
                    PesoMaximoKg = DecimalOpcional(campos, 7),
                    VolumenMaximoM3 = DecimalOpcional(campos, 8),
                    RequiereRefrigeracion = BoolOpcional(campos, 9) ?? false,
                    EsFragil = BoolOpcional(campos, 10) ?? false,
                    EsPeligroso = BoolOpcional(campos, 11) ?? false,
                    EsPerecedero = BoolOpcional(campos, 12) ?? false,
                    EsSobredimensionado = BoolOpcional(campos, 13) ?? false,
                    RequiereFumigacion = BoolOpcional(campos, 14) ?? false
                };

                // Si viene clase de peligrosidad, se normaliza el flag.
                if (!string.IsNullOrWhiteSpace(dto.ClasePeligrosidad))
                {
                    dto.EsPeligroso = true;
                }

                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    _logger.LogWarning(
                        "CSV tipos mercancía: fila {Fila} omitida ({Errores})",
                        fila, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                    continue;
                }

                await _mercanciaRepository.CreateAsync(MapToEntity(dto, empresaId));
                importados++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSV tipos mercancía: fila {Fila} omitida por error inesperado", fila);
            }
        }

        await _auditoria.RegistrarAsync(
            "tipos_mercancia", AccionAuditoria.CREATE, empresaId, null,
            "TipoMercancia", null, new { importados, archivo = "csv" });

        return importados;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(TipoMercanciaRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private static string? ValorOpcional(string[] campos, int indice) =>
        campos.Length > indice && !string.IsNullOrWhiteSpace(campos[indice])
            ? campos[indice].Trim()
            : null;

    private static decimal? DecimalOpcional(string[] campos, int indice)
    {
        var valor = ValorOpcional(campos, indice);
        return decimal.TryParse(valor, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static bool? BoolOpcional(string[] campos, int indice)
    {
        var valor = ValorOpcional(campos, indice);
        return bool.TryParse(valor, out var result) ? result : null;
    }

    private static TipoMercancia MapToEntity(TipoMercanciaRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        Codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim(),
        Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
        Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? null : dto.Categoria.Trim(),
        ClasePeligrosidad = string.IsNullOrWhiteSpace(dto.ClasePeligrosidad) ? null : dto.ClasePeligrosidad.Trim(),
        CodigoOnu = string.IsNullOrWhiteSpace(dto.CodigoOnu) ? null : dto.CodigoOnu.Trim(),
        CodigoHs = string.IsNullOrWhiteSpace(dto.CodigoHs) ? null : dto.CodigoHs.Trim(),
        PesoMaximoKg = dto.PesoMaximoKg,
        VolumenMaximoM3 = dto.VolumenMaximoM3,
        TemperaturaMinC = dto.TemperaturaMinC,
        TemperaturaMaxC = dto.TemperaturaMaxC,
        RequiereRefrigeracion = dto.RequiereRefrigeracion,
        EsFragil = dto.EsFragil,
        EsPeligroso = dto.EsPeligroso,
        EsPerecedero = dto.EsPerecedero,
        EsSobredimensionado = dto.EsSobredimensionado,
        RequiereFumigacion = dto.RequiereFumigacion,
        Activo = true
    };

    private static TipoMercanciaResponseDto MapToResponse(TipoMercancia t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Codigo = t.Codigo,
        Descripcion = t.Descripcion,
        Categoria = t.Categoria,
        ClasePeligrosidad = t.ClasePeligrosidad,
        CodigoOnu = t.CodigoOnu,
        CodigoHs = t.CodigoHs,
        PesoMaximoKg = t.PesoMaximoKg,
        VolumenMaximoM3 = t.VolumenMaximoM3,
        TemperaturaMinC = t.TemperaturaMinC,
        TemperaturaMaxC = t.TemperaturaMaxC,
        RequiereRefrigeracion = t.RequiereRefrigeracion,
        EsFragil = t.EsFragil,
        EsPeligroso = t.EsPeligroso,
        EsPerecedero = t.EsPerecedero,
        EsSobredimensionado = t.EsSobredimensionado,
        RequiereFumigacion = t.RequiereFumigacion,
        EsHazmat = !string.IsNullOrWhiteSpace(t.ClasePeligrosidad),
        Activo = t.Activo,
        FechaCreacion = t.FechaCreacion
    };
}