using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Admin;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Suscripcion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del panel de administración global del Super Admin
/// (HU-009, HU-010, HU-011). Opera sobre TODOS los tenants del SaaS.
/// - Dashboard global y financiero (métricas agregadas).
/// - Impersonación de tenant (HU-009 CA-05) con JWT trazable.
/// - Cambio de plan (CA-04) y de estado de empresa (CAMBIO_ESTADO).
/// </summary>
public class AdminDashboardService : IAdminDashboardService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly IJwtService _jwtService;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(
        IEmpresaRepository empresaRepository,
        ISuscripcionRepository suscripcionRepository,
        IPagoRepository pagoRepository,
        IPlanRepository planRepository,
        IUsuarioRepository usuarioRepository,
        IPermisoRepository permisoRepository,
        IJwtService jwtService,
        IAuditoriaService auditoria,
        ILogger<AdminDashboardService> logger)
    {
        _empresaRepository = empresaRepository;
        _suscripcionRepository = suscripcionRepository;
        _pagoRepository = pagoRepository;
        _planRepository = planRepository;
        _usuarioRepository = usuarioRepository;
        _permisoRepository = permisoRepository;
        _jwtService = jwtService;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene las métricas del dashboard global del SaaS (HU-009).</summary>
    public async Task<DashboardGlobalResponseDto> GetDashboardGlobalAsync()
    {
        var empresas = (await _empresaRepository.GetAllAsync()).ToList();
        var now = DateTime.UtcNow;

        var mrr = await _pagoRepository.GetMrrAsync();

        var proximas = await _suscripcionRepository.GetProximasAVencerAsync(15);
        var tenantsPorVencer = new List<SuscripcionResponseDto>();
        foreach (var s in proximas)
        {
            tenantsPorVencer.Add(await MapToSuscripcionDtoAsync(s));
        }

        return new DashboardGlobalResponseDto
        {
            TotalEmpresasActivas = empresas.Count(e => e.Activo),
            NuevasEstesMes = empresas.Count(e =>
                e.FechaCreacion.Year == now.Year && e.FechaCreacion.Month == now.Month),
            Mrr = mrr,
            Arr = mrr * 12,
            // HU-009: el módulo de embarques aún no existe en este sprint (Fase 2).
            TotalEmbarquesHoy = 0,
            EmpresasPorEstado = empresas
                .GroupBy(e => e.Estado)
                .ToDictionary(g => g.Key, g => g.Count()),
            EmpresasPorPlan = empresas
                .GroupBy(e => e.PlanSuscripcion)
                .ToDictionary(g => g.Key, g => g.Count()),
            TenantsPorVencer = tenantsPorVencer
        };
    }

    /// <summary>Obtiene las métricas del dashboard financiero (MRR, ARR, churn, ingresos).</summary>
    public async Task<DashboardFinancieroResponseDto> GetDashboardFinancieroAsync()
    {
        var now = DateTime.UtcNow;
        var mrr = await _pagoRepository.GetMrrAsync();
        var ingresosMes = await _pagoRepository.GetIngresosDelMesAsync(now.Year, now.Month);

        decimal ingresosAnio = 0;
        for (var mes = 1; mes <= 12; mes++)
        {
            ingresosAnio += await _pagoRepository.GetIngresosDelMesAsync(now.Year, mes);
        }

        return new DashboardFinancieroResponseDto
        {
            Mrr = mrr,
            Arr = mrr * 12,
            IngresosMes = ingresosMes,
            IngresosAño = ingresosAnio,
            NuevosMes = 0,
            ChurnMes = 0,
            PagosPendientes = 0
        };
    }

    /// <summary>
    /// Genera un JWT de impersonación del tenant (HU-009 CA-05).
    /// El JWT incluye el claim "impersonado_por" y registra auditoría con acción IMPERSONACION.
    /// Resuelve el primer ADMIN del tenant para emitir el token con su contexto.
    /// </summary>
    public async Task<LoginResponseDto> ImpersonarAsync(Guid empresaId, Guid superAdminId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException(nameof(Empresa), empresaId);
        }

        if (empresa.Estado != EstadoEmpresa.ACTIVE &&
            empresa.Estado != EstadoEmpresa.TRIAL &&
            empresa.Estado != EstadoEmpresa.PAST_DUE)
        {
            throw new BusinessException("La empresa no está activa y no puede ser impersonada.");
        }

        // Resolver el primer ADMIN del tenant para emitir el token con su contexto.
        var usuarios = (await _usuarioRepository.GetAllAsync(empresaId)).ToList();
        var admin = usuarios.FirstOrDefault(u => u.TipoUsuario == TipoUsuario.ADMIN)
                 ?? usuarios.FirstOrDefault(u => u.Estado == EstadoUsuario.ACTIVE);

        if (admin is null)
        {
            throw new BusinessException("La empresa no tiene un administrador activo para impersonar.");
        }

        var permisos = await CargarPermisosAsync(admin.PerfilId, empresaId);

        var accessToken = _jwtService.GenerateImpersonationToken(
            admin.Id, empresaId, admin.PerfilId,
            admin.TipoUsuario, admin.NombreCompleto, permisos, superAdminId);

        var superAdmin = await _usuarioRepository.GetByIdAsync(
            superAdminId, IdsSistema.EmpresaRaizId);

        await _auditoria.RegistrarAsync(
            "admin", AccionAuditoria.IMPERSONACION, empresaId, superAdminId,
            nameof(Empresa), empresaId,
            new { empresa = empresa.Nombre, adminImpersonado = admin.NombreCompleto });

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = string.Empty,
            ExpiresIn = 8 * 3600,
            Usuario = new UsuarioTokenDto
            {
                Id = admin.Id,
                Nombre = admin.NombreCompleto,
                Email = admin.Email,
                TipoUsuario = admin.TipoUsuario,
                EmpresaNombre = empresa.Nombre,
                Permisos = permisos.ToList()
            }
        };
    }

    /// <summary>
    /// Cambia el plan de un tenant (HU-009 CA-04). Si existe una suscripción activa
    /// se actualiza su plan; si no, se crea una nueva. Registra auditoría CAMBIAR_PLAN.
    /// </summary>
    public async Task CambiarPlanAsync(Guid empresaId, Guid nuevoPlanId,
        string? motivo, Guid cambiadoPorId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException(nameof(Empresa), empresaId);
        }

        var plan = await _planRepository.GetByIdAsync(nuevoPlanId);
        if (plan is null || !plan.Activo)
        {
            throw new NotFoundException(nameof(Plan), nuevoPlanId);
        }

        var activa = await _suscripcionRepository.GetActivaByEmpresaIdAsync(empresaId);

        if (activa is not null)
        {
            activa.PlanId = nuevoPlanId;
            activa.Estado = EstadoSuscripcion.ACTIVE;
            activa.FechaVencimiento = CalcularVencimiento(DateTime.UtcNow, activa.TipoCiclo);
            activa.FechaModificacion = DateTime.UtcNow;
            await _suscripcionRepository.UpdateAsync(activa);
        }
        else
        {
            await _suscripcionRepository.CreateAsync(new Suscripcion
            {
                EmpresaId = empresaId,
                PlanId = nuevoPlanId,
                TipoCiclo = TipoCiclo.MENSUAL,
                FechaInicio = DateTime.UtcNow,
                FechaVencimiento = CalcularVencimiento(DateTime.UtcNow, TipoCiclo.MENSUAL),
                Estado = EstadoSuscripcion.ACTIVE,
                PrecioPactado = plan.PrecioMensual,
                MonedaPactada = plan.Moneda,
                CreadoPorId = cambiadoPorId,
                Activo = true
            });
        }

        empresa.PlanId = nuevoPlanId;
        empresa.PlanSuscripcion = plan.Codigo;
        empresa.Estado = EstadoEmpresa.ACTIVE;
        await _empresaRepository.UpdateAsync(empresa);

        await _auditoria.RegistrarAsync(
            "admin", AccionAuditoria.CAMBIAR_PLAN, empresaId, cambiadoPorId,
            nameof(Empresa), empresaId,
            new { planNuevo = plan.Codigo, motivo });
    }

    /// <summary>
    /// Cambia el estado de una empresa (suspender/reactivar/cancelar).
    /// Registra auditoría con acción CAMBIO_ESTADO.
    /// </summary>
    public async Task CambiarEstadoEmpresaAsync(Guid empresaId, string nuevoEstado,
        Guid cambiadoPorId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException(nameof(Empresa), empresaId);
        }

        var estadosValidos = new[]
        {
            EstadoEmpresa.ACTIVE, EstadoEmpresa.SUSPENDED,
            EstadoEmpresa.CANCELLED, EstadoEmpresa.TRIAL, EstadoEmpresa.PAST_DUE
        };

        if (!estadosValidos.Contains(nuevoEstado))
        {
            throw new BusinessException("El estado de empresa no es válido.");
        }

        var estadoAnterior = empresa.Estado;
        empresa.Estado = nuevoEstado;
        await _empresaRepository.UpdateAsync(empresa);

        // Reflejar el estado en la suscripción activa.
        var activa = await _suscripcionRepository.GetActivaByEmpresaIdAsync(empresaId);
        if (activa is not null)
        {
            var nuevoEstadoSuscripcion = nuevoEstado switch
            {
                EstadoEmpresa.SUSPENDED => EstadoSuscripcion.SUSPENDED,
                EstadoEmpresa.CANCELLED => EstadoSuscripcion.CANCELLED,
                _ => EstadoSuscripcion.ACTIVE
            };
            if (activa.Estado != nuevoEstadoSuscripcion)
            {
                activa.Estado = nuevoEstadoSuscripcion;
                activa.FechaModificacion = DateTime.UtcNow;
                await _suscripcionRepository.UpdateAsync(activa);
            }
        }

        await _auditoria.RegistrarAsync(
            "admin", AccionAuditoria.CAMBIO_ESTADO, empresaId, cambiadoPorId,
            nameof(Empresa), empresaId,
            new { estadoAnterior, estadoNuevo = nuevoEstado });
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<IEnumerable<string>> CargarPermisosAsync(Guid perfilId, Guid empresaId)
    {
        var permisos = await _permisoRepository.GetByPerfilAsync(perfilId, empresaId);

        return permisos
            .SelectMany(p => new[]
            {
                p.PuedeLeer ? $"{p.Modulo}:read" : null,
                p.PuedeCrear ? $"{p.Modulo}:create" : null,
                p.PuedeActualizar ? $"{p.Modulo}:update" : null
            })
            .Where(v => v is not null)
            .Select(v => v!);
    }

    private async Task<SuscripcionResponseDto> MapToSuscripcionDtoAsync(Suscripcion s)
    {
        var plan = await _planRepository.GetByIdAsync(s.PlanId);
        var empresa = await _empresaRepository.GetByIdAsync(s.EmpresaId);

        return new SuscripcionResponseDto
        {
            Id = s.Id,
            EmpresaId = s.EmpresaId,
            EmpresaNombre = empresa?.Nombre ?? string.Empty,
            PlanId = s.PlanId,
            PlanNombre = plan?.Nombre ?? string.Empty,
            PlanCodigo = plan?.Codigo ?? string.Empty,
            TipoCiclo = s.TipoCiclo,
            FechaInicio = s.FechaInicio,
            FechaVencimiento = s.FechaVencimiento,
            Estado = s.Estado,
            PrecioPactado = s.PrecioPactado,
            MonedaPactada = s.MonedaPactada,
            DiasParaVencimiento = (int)Math.Floor((s.FechaVencimiento - DateTime.UtcNow).TotalDays),
            Activo = s.Activo,
            FechaCreacion = s.FechaCreacion
        };
    }

    private static DateTime CalcularVencimiento(DateTime desde, string ciclo) => ciclo switch
    {
        TipoCiclo.ANUAL => desde.AddDays(365),
        _ => desde.AddDays(30)
    };
}
