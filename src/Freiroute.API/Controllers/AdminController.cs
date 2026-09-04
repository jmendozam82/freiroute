using System.Globalization;
using System.Text;
using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Admin;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Plan;
using Freiroute.DTO.Suscripcion;
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Panel de administración global del SUPER_ADMIN (HU-009, HU-010, HU-011).
/// Dashboards global/financiero, gestión de empresas/tenants, planes y
/// suscripciones. El [RequirePermission] se omite automáticamente para
/// SUPER_ADMIN (que es el único que debe acceder). Todos los endpoints
/// verifican IsSuperAdmin() al inicio.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _adminService;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IPlanService _planService;
    private readonly ISuscripcionService _suscripcionService;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IEmpresaService _empresaService;
    private readonly IAuditoriaService _auditoria;

    public AdminController(
        IAdminDashboardService adminService,
        IEmpresaRepository empresaRepository,
        IPlanService planService,
        ISuscripcionService suscripcionService,
        ISuscripcionRepository suscripcionRepository,
        IEmpresaService empresaService,
        IAuditoriaService auditoria)
    {
        _adminService = adminService;
        _empresaRepository = empresaRepository;
        _planService = planService;
        _suscripcionService = suscripcionService;
        _suscripcionRepository = suscripcionRepository;
        _empresaService = empresaService;
        _auditoria = auditoria;
    }

    // ── Guard (Super Admin only) ────────────────────────────────────

    /// <summary>
    /// Verifica que el usuario autenticado sea SUPER_ADMIN. Lanza ForbiddenException
    /// si no es así. Ejecutado por todos los endpoints del panel.
    /// </summary>
    private void VerificarSuperAdmin()
    {
        if (!User.IsSuperAdmin())
        {
            throw new ForbiddenException("Solo el Super Admin puede acceder al panel de administración global.");
        }
    }

    // ── Dashboards (HU-009, HU-011) ─────────────────────────────────

    /// <summary>Métricas del dashboard global del SaaS (empresas, MRR, ARR, distribuciones).</summary>
    [HttpGet("dashboard")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DashboardGlobalResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardGlobalResponseDto>>> DashboardGlobal()
    {
        VerificarSuperAdmin();
        var data = await _adminService.GetDashboardGlobalAsync();
        return Ok(ApiResponse<DashboardGlobalResponseDto>.Ok(data));
    }

    /// <summary>Métricas del dashboard financiero (MRR, ARR, ingresos, churn).</summary>
    [HttpGet("dashboard/financiero")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<DashboardFinancieroResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardFinancieroResponseDto>>> DashboardFinanciero()
    {
        VerificarSuperAdmin();
        var data = await _adminService.GetDashboardFinancieroAsync();
        return Ok(ApiResponse<DashboardFinancieroResponseDto>.Ok(data));
    }

    // ── Empresas / Tenants (HU-009) ────────────────────────────────

    /// <summary>Lista paginada de todos los tenants del SaaS con filtros (estado, plan, búsqueda).</summary>
    [HttpGet("empresas")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmpresaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<EmpresaResponseDto>>>> GetAllEmpresas(
        [FromQuery] string? estado = null,
        [FromQuery] string? plan = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1)
    {
        VerificarSuperAdmin();

        if (page < 1) page = 1;
        const int pageSize = 20;

        var empresas = (await _empresaRepository.GetAllAsync()).ToList();

        if (!string.IsNullOrWhiteSpace(estado))
            empresas = empresas.Where(e => e.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(plan))
            empresas = empresas.Where(e => e.PlanSuscripcion.Equals(plan, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var busqueda = q.ToLowerInvariant();
            empresas = empresas.Where(e =>
                e.Nombre.ToLowerInvariant().Contains(busqueda) ||
                e.EmailAdmin.ToLowerInvariant().Contains(busqueda)).ToList();
        }

        var items = new List<EmpresaResponseDto>();
        foreach (var e in empresas.Skip((page - 1) * pageSize).Take(pageSize))
        {
            items.Add(await MapToAdminResponseDtoAsync(e));
        }

        var result = new PagedResult<EmpresaResponseDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = empresas.Count
        };

        return Ok(ApiResponse<PagedResult<EmpresaResponseDto>>.Ok(result));
    }

    /// <summary>Detalle de un tenant por su Id (Super Admin — sin filtro de empresa).</summary>
    [HttpGet("empresas/{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<EmpresaResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EmpresaResponseDto>>> GetEmpresa(Guid id)
    {
        VerificarSuperAdmin();
        var empresa = await _empresaRepository.GetByIdAsync(id);
        if (empresa is null)
        {
            throw new NotFoundException(nameof(Empresa), id);
        }

        return Ok(ApiResponse<EmpresaResponseDto>.Ok(await MapToAdminResponseDtoAsync(empresa)));
    }

    /// <summary>
    /// Exporta la lista de tenants a CSV (HU-009 CA-07). Genera un archivo con
    /// nombre, email_admin, plan, estado, fecha_registro y próximo vencimiento.
    /// </summary>
    [HttpGet("empresas/export")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    public async Task<IActionResult> ExportEmpresas([FromQuery] string? estado = null)
    {
        VerificarSuperAdmin();
        var empresas = (await _empresaRepository.GetAllAsync()).ToList();

        if (!string.IsNullOrWhiteSpace(estado))
            empresas = empresas.Where(e => e.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("nombre,email_admin,plan,estado,fecha_registro,proximo_vencimiento");

        foreach (var e in empresas)
        {
            var proximoVencimiento = await ObtenerProximoVencimientoAsync(e.Id);
            sb.AppendLine(string.Join(",",
                CsvEscape(e.Nombre),
                CsvEscape(e.EmailAdmin),
                CsvEscape(e.PlanSuscripcion),
                CsvEscape(e.Estado),
                e.FechaCreacion.ToString("yyyy-MM-dd"),
                proximoVencimiento?.ToString("yyyy-MM-dd") ?? ""));
        }

        // Auditoría de exportación.
        await _auditoria.RegistrarAsync(
            "admin", AccionAuditoria.EXPORT, IdsSistema.EmpresaRaizId, User.GetUsuarioId(),
            nameof(Empresa), null, new { tipo = "csv", cantidad = empresas.Count });

        var fecha = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return File(Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv; charset=utf-8",
            $"empresas_{fecha}.csv");
    }

    /// <summary>Cambia el plan de un tenant (HU-009 CA-04) — PUT /api/admin/empresas/{id}/plan.</summary>
    [HttpPut("empresas/{empresaId:guid}/plan")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> CambiarPlan(Guid empresaId, CambiarPlanRequestDto request)
    {
        VerificarSuperAdmin();
        await _adminService.CambiarPlanAsync(empresaId, request.PlanId, request.Motivo, User.GetUsuarioId());
        return Ok(ApiResponse<string>.Ok(string.Empty, "Plan actualizado"));
    }

    /// <summary>Cambia el estado de una empresa (suspender/reactivar/cancelar) — PUT /api/admin/empresas/{id}/estado.</summary>
    [HttpPut("empresas/{empresaId:guid}/estado")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> CambiarEstado(Guid empresaId, [FromBody] CambiarEstadoRequestDto request)
    {
        VerificarSuperAdmin();
        await _adminService.CambiarEstadoEmpresaAsync(empresaId, request.NuevoEstado, User.GetUsuarioId());
        return Ok(ApiResponse<string>.Ok(string.Empty, "Estado de empresa actualizado"));
    }

    /// <summary>
    /// Impersona a un tenant (HU-009 CA-05): genera un JWT del primer ADMIN del
    /// tenant con claim "impersonado_por". Regresa el token + datos de contexto.
    /// </summary>
    [HttpPost("empresas/{empresaId:guid}/impersonar")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<ImpersonarResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ImpersonarResponseDto>>> Impersonar(Guid empresaId)
    {
        VerificarSuperAdmin();
        var superAdminId = User.GetUsuarioId();
        var login = await _adminService.ImpersonarAsync(empresaId, superAdminId);

        var response = new ImpersonarResponseDto
        {
            AccessToken = login.AccessToken,
            EmpresaNombre = login.Usuario.EmpresaNombre,
            AdminNombre = User.GetNombre(),
            ExpiraEn = login.ExpiresIn
        };

        return Ok(ApiResponse<ImpersonarResponseDto>.Ok(response, "Impersonación iniciada"));
    }

    // ── Planes (HU-010) ─────────────────────────────────────────────

    /// <summary>Lista todos los planes del SaaS (Super Admin ve activos E inactivos).</summary>
    [HttpGet("planes")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PlanResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PlanResponseDto>>>> GetAllPlanes()
    {
        VerificarSuperAdmin();
        var planes = await _planService.GetAllAsync(soloActivos: false);
        return Ok(ApiResponse<IEnumerable<PlanResponseDto>>.Ok(planes));
    }

    /// <summary>Obtiene un plan por su Id.</summary>
    [HttpGet("planes/{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PlanResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlanResponseDto>>> GetPlanById(Guid id)
    {
        VerificarSuperAdmin();
        var plan = await _planService.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException(nameof(Plan), id);
        }
        return Ok(ApiResponse<PlanResponseDto>.Ok(plan));
    }

    /// <summary>Crea un plan nuevo (HU-010 CA-01). Retorna 201 Created.</summary>
    [HttpPost("planes")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<PlanResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PlanResponseDto>>> CreatePlan(PlanRequestDto request)
    {
        VerificarSuperAdmin();
        var plan = await _planService.CreateAsync(request);
        return CreatedAtAction(
            nameof(GetPlanById),
            new { id = plan.Id },
            ApiResponse<PlanResponseDto>.Ok(plan, "Plan creado exitosamente"));
    }

    /// <summary>Actualiza un plan existente (HU-010 CA-02).</summary>
    [HttpPut("planes/{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<PlanResponseDto>>> UpdatePlan(Guid id, PlanRequestDto request)
    {
        VerificarSuperAdmin();
        var plan = await _planService.UpdateAsync(id, request);
        return Ok(ApiResponse<PlanResponseDto>.Ok(plan, "Plan actualizado"));
    }

    /// <summary>
    /// Desactiva un plan (HU-010 CA-04). Si el plan tiene empresas suscritas
    /// activas → BusinessException → 422.
    /// </summary>
    [HttpDelete("planes/{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Update)]
    public async Task<IActionResult> DeactivatePlan(Guid id)
    {
        VerificarSuperAdmin();
        await _planService.DeactivateAsync(id);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Plan desactivado"));
    }

    // ── Suscripciones (HU-011) ──────────────────────────────────────

    /// <summary>Lista paginada de suscripciones con filtro opcional por estado.</summary>
    [HttpGet("suscripciones")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SuscripcionResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<SuscripcionResponseDto>>>> GetAllSuscripciones(
        [FromQuery] string? estado = null, [FromQuery] int page = 1)
    {
        VerificarSuperAdmin();
        if (page < 1) page = 1;
        var data = await _suscripcionService.GetAllAsync(estado, page, 20);
        return Ok(ApiResponse<PagedResult<SuscripcionResponseDto>>.Ok(data));
    }

    /// <summary>Detalle de una suscripción por su Id.</summary>
    [HttpGet("suscripciones/{id:guid}")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<SuscripcionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SuscripcionResponseDto>>> GetSuscripcion(Guid id)
    {
        VerificarSuperAdmin();
        var suscripcion = await _suscripcionService.GetByIdAsync(id);
        if (suscripcion is null)
        {
            throw new NotFoundException(nameof(Suscripcion), id);
        }
        return Ok(ApiResponse<SuscripcionResponseDto>.Ok(suscripcion));
    }

    /// <summary>
    /// Registra un pago manual para una suscripción (HU-011 CA-02/03).
    /// Un pago COMPLETED activa la suscripción y extiende el vencimiento. Retorna 201.
    /// </summary>
    [HttpPost("suscripciones/{id:guid}/pago")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<PagoResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PagoResponseDto>>> RegistrarPago(Guid id, PagoRequestDto request)
    {
        VerificarSuperAdmin();
        var pago = await _suscripcionService.RegistrarPagoAsync(id, request, User.GetUsuarioId());
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<PagoResponseDto>.Ok(pago, "Pago registrado exitosamente"));
    }

    /// <summary>
    /// Historial de pagos de una suscripción (HU-011 CA-08). Resuelve el empresaId
    /// desde la suscripción y devuelve todos sus pagos.
    /// </summary>
    [HttpGet("suscripciones/{id:guid}/pagos")]
    [RequirePermission(ModuloPermiso.Configuracion, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PagoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PagoResponseDto>>>> GetPagosSuscripcion(Guid id)
    {
        VerificarSuperAdmin();
        var suscripcion = await _suscripcionRepository.GetByIdAsync(id);
        if (suscripcion is null)
        {
            throw new NotFoundException(nameof(Suscripcion), id);
        }

        var pagos = await _suscripcionService.GetPagosByEmpresaAsync(suscripcion.EmpresaId);
        return Ok(ApiResponse<IEnumerable<PagoResponseDto>>.Ok(pagos));
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<EmpresaResponseDto> MapToAdminResponseDtoAsync(Empresa e)
    {
        var proximoVencimiento = await ObtenerProximoVencimientoAsync(e.Id);

        return new EmpresaResponseDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            EmailAdmin = e.EmailAdmin,
            RucNit = e.RucNit,
            Telefono = e.Telefono,
            Pais = e.Pais,
            Industria = e.Industria,
            Estado = e.Estado,
            PlanSuscripcion = e.PlanSuscripcion,
            FechaCreacion = e.FechaCreacion,
            OnboardingCompletado = e.OnboardingCompletado,
            Activo = e.Activo,
            ProximoVencimiento = proximoVencimiento
        };
    }

    private async Task<DateTime?> ObtenerProximoVencimientoAsync(Guid empresaId)
    {
        var activa = await _suscripcionRepository.GetActivaByEmpresaIdAsync(empresaId);
        return activa?.FechaVencimiento;
    }

    private static string CsvEscape(string value)
        => $"\"{value.Replace("\"", "\"\"")}\"";
}
