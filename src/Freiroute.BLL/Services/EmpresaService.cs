using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Empresa;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de empresas/tenants (HU-001). Tabla raíz del SaaS:
/// NO recibe empresaId — la gestiona globalmente el SUPER_ADMIN (CA-07).
/// Al crear un tenant se transacciona: empresa + perfiles base con sus permisos
/// plantilla copiados de la empresa raíz (CA-02) + email de bienvenida (CA-03)
/// + auditoría (CA-05). Email duplicado → ConflictException 409 (CA-06).
/// </summary>
public class EmpresaService : IEmpresaService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly IValidator<EmpresaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmpresaService> _logger;

    public EmpresaService(
        IEmpresaRepository empresaRepository,
        IPerfilRepository perfilRepository,
        IPermisoRepository permisoRepository,
        IValidator<EmpresaRequestDto> validator,
        IAuditoriaService auditoria,
        IEmailService emailService,
        ILogger<EmpresaService> logger)
    {
        _empresaRepository = empresaRepository;
        _perfilRepository = perfilRepository;
        _permisoRepository = permisoRepository;
        _validator = validator;
        _auditoria = auditoria;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>Registra un nuevo tenant, crea sus perfiles base y registra la auditoría (HU-001).</summary>
    public async Task<EmpresaResponseDto> CreateAsync(EmpresaRequestDto dto)
    {
        // 1. Validación servidor (FluentValidation).
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        // 2. Unicidad global del email_admin (CA-06 → 409).
        var existente = await _empresaRepository.GetByEmailAdminAsync(dto.EmailAdmin);
        if (existente is not null)
        {
            throw new ConflictException("Ya existe una empresa con ese email.");
        }

        // 3. Mapear DTO → Entity y persistir (UUID generado por la BD — CA-01).
        var empresa = MapToEntity(dto);
        var empresaId = await _empresaRepository.CreateAsync(empresa);

        // 4. Perfiles base con permisos plantilla (CA-02).
        //    NOTA de atomicidad: los repos de Fase 2 no exponen transacciones
        //    compartidas; la creación es secuencial. Si un paso posterior falla,
        //    se loguea con error claro (Sprint 2: envolver en transacción real).
        try
        {
            await CrearPerfilesBaseConPermisosAsync(empresaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "La empresa {EmpresaId} se creó pero falló la creación de perfiles base — requiere intervención manual",
                empresaId);
            throw new BusinessException(
                "La empresa se creó, pero falló la configuración de sus perfiles base. Contacte a soporte.",
                ex);
        }

        // 5. Email de bienvenida al email_admin (CA-03).
        await _emailService.EnviarAsync(
            dto.EmailAdmin,
            "¡Bienvenido a Freiroute TMS!",
            $"<p>Tu empresa <strong>{dto.Nombre}</strong> ha sido registrada con el plan {dto.PlanSuscripcion}.</p>" +
            "<p>Completa la configuración inicial en el asistente de onboarding.</p>");

        // 6. Auditoría (CA-05). El tenant de auditoría es la empresa raíz
        //    (el SUPER_ADMIN opera sin un tenant propio distinto al de Freiroute).
        await _auditoria.RegistrarAsync(
            "empresas", AccionAuditoria.CREATE, IdsSistema.EmpresaRaizId, null,
            nameof(Empresa), empresaId,
            new { nombre = dto.Nombre, emailAdmin = dto.EmailAdmin, plan = dto.PlanSuscripcion });

        var created = await _empresaRepository.GetByIdAsync(empresaId);
        return MapToResponseDto(created!);
    }

    /// <summary>Obtiene una empresa por su Id (SUPER_ADMIN — sin filtro de tenant).</summary>
    public async Task<EmpresaResponseDto?> GetByIdAsync(Guid id)
    {
        var empresa = await _empresaRepository.GetByIdAsync(id);
        return empresa is null ? null : MapToResponseDto(empresa);
    }

    /// <summary>Obtiene todas las empresas activas (panel Super Admin — sin filtro de tenant).</summary>
    public async Task<IEnumerable<EmpresaResponseDto>> GetAllAsync()
    {
        var empresas = await _empresaRepository.GetAllAsync();
        return empresas.Select(MapToResponseDto);
    }

    /// <summary>Actualiza los datos de una empresa (SUPER_ADMIN).</summary>
    public async Task<EmpresaResponseDto> UpdateAsync(Guid id, EmpresaRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var existente = await _empresaRepository.GetByIdAsync(id);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Empresa), id);
        }

        // El email_admin NO se actualiza (clave de unicidad global, ver repositorio).
        var empresa = MapToEntity(dto);
        empresa.Id = id;

        var ok = await _empresaRepository.UpdateAsync(empresa);
        if (!ok)
        {
            throw new NotFoundException(nameof(Empresa), id);
        }

        await _auditoria.RegistrarAsync(
            "empresas", AccionAuditoria.UPDATE, IdsSistema.EmpresaRaizId, null,
            nameof(Empresa), id, new { nombre = dto.Nombre });

        var updated = await _empresaRepository.GetByIdAsync(id);
        return MapToResponseDto(updated!);
    }

    /// <summary>Soft delete de una empresa (SUPER_ADMIN). Nunca elimina físicamente.</summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        var existente = await _empresaRepository.GetByIdAsync(id);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Empresa), id);
        }

        var ok = await _empresaRepository.DeactivateAsync(id);
        if (!ok)
        {
            throw new NotFoundException(nameof(Empresa), id);
        }

        await _auditoria.RegistrarAsync(
            "empresas", AccionAuditoria.DEACTIVATE, IdsSistema.EmpresaRaizId, null,
            nameof(Empresa), id, new { nombre = existente.Nombre });

        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Crea los 5 perfiles base del tenant (ADMIN, DISPATCHER, OPERADOR,
    /// CONDUCTOR, CLIENTE) con es_sistema=true y copia sus permisos desde los
    /// perfiles plantilla de la empresa raíz (HU-001 CA-02, HU-006 CA-01).
    /// </summary>
    private async Task CrearPerfilesBaseConPermisosAsync(Guid empresaId)
    {
        foreach (var tipo in IdsSistema.PerfilesBaseTenant)
        {
            // Plantilla en la empresa raíz (migración 0008).
            var plantilla = await _perfilRepository.GetByTipoAsync(
                tipo, IdsSistema.EmpresaRaizId);

            if (plantilla is null)
            {
                _logger.LogWarning(
                    "No existe perfil plantilla {Tipo} en la empresa raíz — se omite",
                    tipo);
                continue;
            }

            var perfilId = await _perfilRepository.CreateAsync(new Perfil
            {
                EmpresaId = empresaId,
                Nombre = plantilla.Nombre,
                Descripcion = plantilla.Descripcion,
                TipoPerfil = tipo,
                EsSistema = true,
                Activo = true
            });

            // Copiar permisos de la plantilla con los mismos flags (ADR-009).
            var permisosPlantilla = await _permisoRepository.GetByPerfilAsync(
                plantilla.Id, IdsSistema.EmpresaRaizId);

            foreach (var permiso in permisosPlantilla)
            {
                await _permisoRepository.CreateAsync(new Permiso
                {
                    EmpresaId = empresaId,
                    PerfilId = perfilId,
                    Modulo = permiso.Modulo,
                    PuedeLeer = permiso.PuedeLeer,
                    PuedeCrear = permiso.PuedeCrear,
                    PuedeActualizar = permiso.PuedeActualizar,
                    Activo = true
                });
            }
        }
    }

    private static Empresa MapToEntity(EmpresaRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        RucNit = dto.RucNit,
        EmailAdmin = dto.EmailAdmin,
        Telefono = dto.Telefono,
        Pais = dto.Pais,
        Ciudad = dto.Ciudad,
        Direccion = dto.Direccion,
        LogoUrl = dto.LogoUrl,
        ColorPrimario = string.IsNullOrWhiteSpace(dto.ColorPrimario) ? "#1A73E8" : dto.ColorPrimario,
        ColorSecundario = string.IsNullOrWhiteSpace(dto.ColorSecundario) ? "#0B2545" : dto.ColorSecundario,
        PlanSuscripcion = dto.PlanSuscripcion,
        Estado = EstadoEmpresa.ACTIVE, // CA-04: tenant activo por defecto
        MonedaPrincipal = string.IsNullOrWhiteSpace(dto.MonedaPrincipal) ? "USD" : dto.MonedaPrincipal!,
        ZonaHoraria = string.IsNullOrWhiteSpace(dto.ZonaHoraria) ? "America/Managua" : dto.ZonaHoraria!,
        Idioma = string.IsNullOrWhiteSpace(dto.Idioma) ? "es" : dto.Idioma!,
        FormatoFecha = string.IsNullOrWhiteSpace(dto.FormatoFecha) ? "DD/MM/YYYY" : dto.FormatoFecha!,
        // HU-001: el prefijo de embarque son las 2 primeras letras del nombre en mayúsculas.
        PrefijoEmbarque = DerivarPrefijoEmbarque(dto.Nombre),
        ConsecutivoEmbarque = 1,
        PrefijoOrden = "ORD",
        ConsecutivoOrden = 1,
        Activo = true
    };

    /// <summary>Deriva el prefijo de embarque: 2 primeras letras del nombre en mayúsculas (fallback "FR").</summary>
    private static string DerivarPrefijoEmbarque(string nombre)
    {
        var letras = nombre
            .Where(char.IsLetterOrDigit)
            .Take(2)
            .Select(char.ToUpperInvariant)
            .ToArray();

        return letras.Length == 2 ? new string(letras) : "FR";
    }

    private static EmpresaResponseDto MapToResponseDto(Empresa e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        RucNit = e.RucNit,
        EmailAdmin = e.EmailAdmin,
        Telefono = e.Telefono,
        Pais = e.Pais,
        Ciudad = e.Ciudad,
        Direccion = e.Direccion,
        LogoUrl = e.LogoUrl,
        ColorPrimario = e.ColorPrimario,
        ColorSecundario = e.ColorSecundario,
        PlanSuscripcion = e.PlanSuscripcion,
        Estado = e.Estado,
        MonedaPrincipal = e.MonedaPrincipal,
        ZonaHoraria = e.ZonaHoraria,
        Idioma = e.Idioma,
        FormatoFecha = e.FormatoFecha,
        PrefijoEmbarque = e.PrefijoEmbarque,
        ConsecutivoEmbarque = e.ConsecutivoEmbarque,
        PrefijoOrden = e.PrefijoOrden,
        ConsecutivoOrden = e.ConsecutivoOrden,
        Activo = e.Activo,
        FechaCreacion = e.FechaCreacion,
        FechaModificacion = e.FechaModificacion
    };
}