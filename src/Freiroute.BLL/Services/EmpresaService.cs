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
/// Al crear un tenant se orquesta el flujo completo (Fix smoke test):
/// empresa (estado TRIAL) + perfiles base con sus permisos plantilla (CA-02)
/// + suscripción TRIAL de 30 días (ADR-004) + usuario admin con contraseña
/// temporal (Supabase Auth) + email de bienvenida + auditoría (CA-05).
/// Email duplicado → ConflictException 409 (CA-06). Si falla un paso posterior
/// a la creación de la empresa, se desactiva como compensación (soft delete).
/// </summary>
public class EmpresaService : IEmpresaService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ISupabaseAuthService _supabaseAuth;
    private readonly IValidator<EmpresaRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmpresaService> _logger;

    // Duración de la suscripción TRIAL inicial (ADR-004).
    private const int DiasTrial = 30;

    public EmpresaService(
        IEmpresaRepository empresaRepository,
        IPerfilRepository perfilRepository,
        IPermisoRepository permisoRepository,
        ISuscripcionRepository suscripcionRepository,
        IPlanRepository planRepository,
        IUsuarioRepository usuarioRepository,
        ISupabaseAuthService supabaseAuth,
        IValidator<EmpresaRequestDto> validator,
        IAuditoriaService auditoria,
        IEmailService emailService,
        ILogger<EmpresaService> logger)
    {
        _empresaRepository = empresaRepository;
        _perfilRepository = perfilRepository;
        _permisoRepository = permisoRepository;
        _suscripcionRepository = suscripcionRepository;
        _planRepository = planRepository;
        _usuarioRepository = usuarioRepository;
        _supabaseAuth = supabaseAuth;
        _validator = validator;
        _auditoria = auditoria;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo tenant orquestando el flujo completo del SaaS:
    /// empresa (TRIAL) → perfiles base → suscripción TRIAL → plan_id →
    /// usuario admin → email de bienvenida → auditoría (HU-001, HU-011, ADR-004).
    /// Si falla un paso posterior a la creación de la empresa, se desactiva
    /// como compensación (soft delete — nunca DELETE físico).
    /// </summary>
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

        // NOTA de atomicidad: los repos de Fase 2 no exponen transacciones
        // compartidas; la creación es secuencial. Si falla un paso posterior,
        // se compensa desactivando la empresa (soft delete).
        Guid empresaId = Guid.Empty;

        try
        {
            // 3. Mapear DTO → Entity y persistir (UUID generado por la BD — CA-01).
            //    Fix smoke test: estado inicial TRIAL (ADR-004), no ACTIVE.
            var empresa = MapToEntity(dto);
            empresaId = await _empresaRepository.CreateAsync(empresa);

            // 4. Perfiles base con permisos plantilla (CA-02).
            try
            {
                await CrearPerfilesBaseConPermisosAsync(empresaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "La empresa {EmpresaId} se creó pero falló la creación de perfiles base",
                    empresaId);
                throw new BusinessException(
                    "La empresa se creó, pero falló la configuración de sus perfiles base. Contacte a soporte.",
                    ex);
            }

            // 5. Plan de suscripción: por Id si se envió, si no por código (fallback STARTER).
            var plan = await ResolverPlanAsync(dto);

            // 6. Suscripción TRIAL de 30 días (HU-011 CA-01/02, ADR-004).
            await CrearSuscripcionTrialAsync(empresaId, plan);

            // 7. Vincular el plan en la empresa (columna de alter_empresas_sprint2).
            await _empresaRepository.UpdatePlanIdAsync(empresaId, plan.Id);

            // 8. Usuario admin del tenant + email de bienvenida con contraseña temporal.
            await CrearUsuarioAdminAsync(empresaId, dto.EmailAdmin, dto.Nombre);

            // 9. Auditoría (CA-05). El tenant de auditoría es la empresa raíz
            //    (el SUPER_ADMIN opera sin un tenant propio distinto al de Freiroute).
            await _auditoria.RegistrarAsync(
                "empresas", AccionAuditoria.CREATE, IdsSistema.EmpresaRaizId, null,
                nameof(Empresa), empresaId,
                new { nombre = dto.Nombre, emailAdmin = dto.EmailAdmin, plan = plan.Codigo });

            var created = await _empresaRepository.GetByIdAsync(empresaId);
            return MapToResponseDto(created!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en CreateAsync de empresa {Email}: {Mensaje}",
                dto.EmailAdmin, ex.Message);

            // Compensación: si la empresa se creó pero falló el resto del flujo,
            // se desactiva (soft delete — nunca DELETE físico).
            if (empresaId != Guid.Empty)
            {
                try
                {
                    await _empresaRepository.DeactivateAsync(empresaId);
                }
                catch (Exception compEx)
                {
                    _logger.LogWarning(compEx,
                        "No se pudo desactivar la empresa {EmpresaId} tras un fallo de compensación",
                        empresaId);
                }
            }

            throw; // propagar para que GlobalExceptionMiddleware maneje
        }
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
        // Preservar el estado de facturación: MapToEntity fija TRIAL para el alta,
        // pero un UPDATE no debe revertir el estado del tenant (Activa/Vencida/Suspendida).
        empresa.Estado = existente.Estado;

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

    /// <summary>
    /// Resuelve el plan de suscripción inicial: por PlanId si se envió;
    /// si no, por código (PlanSuscripcion) y defaultea a STARTER.
    /// </summary>
    private async Task<Plan> ResolverPlanAsync(EmpresaRequestDto dto)
    {
        if (dto.PlanId.HasValue)
        {
            var porId = await _planRepository.GetByIdAsync(dto.PlanId.Value);
            if (porId is not null)
            {
                return porId;
            }
        }

        var porCodigo = await _planRepository.GetByCodigoAsync(dto.PlanSuscripcion)
            ?? await _planRepository.GetByCodigoAsync("STARTER");

        return porCodigo
            ?? throw new BusinessException(
                "No existe el plan STARTER en el catálogo de planes.");
    }

    /// <summary>Crea la suscripción TRIAL de 30 días del tenant nuevo (HU-011 / ADR-004).</summary>
    private async Task CrearSuscripcionTrialAsync(Guid empresaId, Plan plan)
    {
        var suscripcion = new Suscripcion
        {
            EmpresaId = empresaId,
            PlanId = plan.Id,
            TipoCiclo = TipoCiclo.MENSUAL,
            FechaInicio = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddDays(DiasTrial),
            Estado = EstadoSuscripcion.TRIAL,
            PrecioPactado = plan.PrecioMensual,
            MonedaPactada = plan.Moneda,
            CreadoPorId = null, // creada por el sistema durante el alta del tenant
            Activo = true
        };

        await _suscripcionRepository.CreateAsync(suscripcion);
    }

    /// <summary>
    /// Crea el usuario admin del tenant en la tabla 'usuarios' y su vínculo en
    /// Supabase Auth (stub en Sprint 1). Envía el email de bienvenida con la
    /// contraseña temporal (Fix smoke test — orquestación de tenant).
    /// </summary>
    private async Task CrearUsuarioAdminAsync(Guid empresaId, string emailAdmin, string nombreEmpresa)
    {
        // Perfil ADMIN del tenant recién creado (perfiles base, CA-02).
        var perfilAdmin = await _perfilRepository.GetByTipoAsync(TipoPerfil.ADMIN, empresaId);
        if (perfilAdmin is null)
        {
            throw new BusinessException(
                "No se pudo resolver el perfil ADMIN del tenant recién creado.");
        }

        // Contraseña temporal segura: Fr + 4 dígitos + ! (mayúscula, número, especial).
        var passwordTemporal = GenerarPasswordTemporal();

        // Crear el vínculo de autenticación en Supabase Auth (stub — registra en logs).
        var supabaseUserId = await _supabaseAuth.SignUpAsync(emailAdmin, passwordTemporal);

        // Registro de negocio en la tabla 'usuarios'.
        var usuarioAdmin = new Usuario
        {
            EmpresaId = empresaId,
            PerfilId = perfilAdmin.Id,
            NombreCompleto = $"Administrador de {nombreEmpresa}",
            Email = emailAdmin,
            TipoUsuario = TipoUsuario.ADMIN,
            Estado = EstadoUsuario.ACTIVE,
            SupabaseUserId = supabaseUserId,
            Activo = true
        };

        await _usuarioRepository.CreateAsync(usuarioAdmin);

        // Email de bienvenida con la contraseña temporal (CA-03).
        await _emailService.EnviarAsync(
            emailAdmin,
            "Bienvenido a Freiroute — Tu cuenta de administrador",
            $"<p>Tu empresa <strong>{nombreEmpresa}</strong> ha sido activada con tu plan de prueba.</p>" +
            $"<p>Ingresa con:<br>" +
            $"Email: <strong>{emailAdmin}</strong><br>" +
            $"Contraseña temporal: <strong>{passwordTemporal}</strong></p>" +
            "<p>Te recomendamos cambiar tu contraseña al ingresar por primera vez.</p>");
    }

    /// <summary>Genera una contraseña temporal: Fr + 4 dígitos + ! (política: mayúscula, número, especial).</summary>
    private static string GenerarPasswordTemporal()
    {
        var random = new Random();
        var numeros = random.Next(1000, 9999).ToString();
        return $"Fr{numeros}!";
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
        // Fix smoke test: estado inicial TRIAL según ADR-004 (antes ACTIVE).
        Estado = EstadoEmpresa.TRIAL,
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