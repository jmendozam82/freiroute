using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Plan;
using Freiroute.Entity;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Verificación de límites del plan contratado por un tenant (HU-013 CA-08, ADR-004).
/// Lanza <see cref="BusinessException"/> (HTTP 422) si se supera un límite antes de persistir.
/// </summary>
public class PlanLimiteService : IPlanLimiteService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<PlanLimiteService> _logger;

    public PlanLimiteService(
        IEmpresaRepository empresaRepository,
        IPlanRepository planRepository,
        IUsuarioRepository usuarioRepository,
        ILogger<PlanLimiteService> logger)
    {
        _empresaRepository = empresaRepository;
        _planRepository = planRepository;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    /// <summary>
    /// Verifica que el tenant no haya superado el límite de usuarios del plan
    /// (HU-013 CA-08). Lanza BusinessException con la sugerencia de upgrade si se supera.
    /// </summary>
    public async Task VerificarLimiteUsuariosAsync(Guid empresaId)
    {
        var plan = await GetPlanActivoAsync(empresaId);
        if (plan is null || plan.LimiteUsuarios <= 0)
        {
            return; // Sin plan activo o límite ilimitado (0/negativo).
        }

        var activos = (await _usuarioRepository.GetAllAsync(empresaId)).Count();

        if (activos >= plan.LimiteUsuarios)
        {
            var sugerencia = await ObtenerSugerenciaUpgrade(plan);
            throw new BusinessException(
                $"Se alcanzó el límite de usuarios del plan {plan.Nombre} ({plan.LimiteUsuarios}).{sugerencia}");
        }
    }

    /// <summary>
    /// Verifica que el tenant no haya superado el límite de embarques del mes.
    /// El módulo de embarques (Fase 2) aún no está disponible, por lo que la
    /// verificación queda lista para activarse cuando exista el repositorio.
    /// </summary>
    public async Task VerificarLimiteEmbarquesMesAsync(Guid empresaId)
    {
        var plan = await GetPlanActivoAsync(empresaId);
        if (plan is null || plan.LimiteEmbarquesMes <= 0)
        {
            return;
        }

        // Embarques del mes: aún sin tabla (Fase 2). Se mantiene la estructura para
        // no romper el contrato; cuando exista IEmbarqueRepository se contará aquí.
        _logger.LogDebug("VerificarLimiteEmbarquesMesAsync para empresa {EmpresaId} (módulo no disponible aún)", empresaId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifica si un módulo está disponible para el plan del tenant (ADR-004 gating).
    /// Si el plan no declara módulos, se asume el catálogo STARTER.
    /// </summary>
    public async Task<bool> ModuloDisponibleAsync(string modulo, Guid empresaId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        var plan = await GetPlanActivoAsync(empresaId);
        var modulos = plan?.ModulosDisponibles;
        if (modulos is null || modulos.Length == 0)
        {
            // Sin módulos declarados: se asume acceso al módulo (catálogo abierto).
            return true;
        }

        return modulos.Contains(modulo, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Obtiene el plan activo del tenant (por PlanId de la empresa).</summary>
    public async Task<PlanResponseDto?> GetPlanActivoAsync(Guid empresaId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId);
        if (empresa is null)
        {
            throw new NotFoundException("empresas", empresaId);
        }

        Plan? plan = null;

        if (empresa.PlanId is not null)
        {
            plan = await _planRepository.GetByIdAsync(empresa.PlanId.Value);
        }

        // Fallback por código si no hay PlanId o el plan no existe.
        plan ??= await BuscarPorCodigoAsync(empresa.PlanSuscripcion);

        return plan is null ? null : MapToResponse(plan);
    }

    /// <summary>
    /// Obtiene el plan superior al actual para sugerir upgrade.
    /// Retorna null si ya está en el máximo (ENTERPRISE).
    /// </summary>
    public async Task<PlanResponseDto?> GetPlanSuperiorAsync(string planActualCodigo)
    {
        var planes = (await _planRepository.GetAllAsync(true)).ToList();

        var orden = new[] { "STARTER", "PROFESSIONAL", "ENTERPRISE" };
        var idxActual = Array.FindIndex(orden,
            o => o.Equals(planActualCodigo, StringComparison.OrdinalIgnoreCase));

        if (idxActual < 0 || idxActual >= orden.Length - 1)
        {
            return null;
        }

        var codigoSuperior = orden[idxActual + 1];
        var plan = planes.FirstOrDefault(p =>
            p.Codigo.Equals(codigoSuperior, StringComparison.OrdinalIgnoreCase));

        return plan is null ? null : MapToResponse(plan);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<Plan?> BuscarPorCodigoAsync(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }
        var planes = await _planRepository.GetAllAsync(true);
        return planes.FirstOrDefault(p =>
            p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> ObtenerSugerenciaUpgrade(PlanResponseDto planActual)
    {
        var superior = await GetPlanSuperiorAsync(planActual.Codigo);
        return superior is null
            ? " Ya se encuentra en el plan máximo."
            : $" Considere mejorar al plan {superior.Nombre}.";
    }

    private static PlanResponseDto MapToResponse(Plan p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Codigo = p.Codigo,
        Descripcion = p.Descripcion,
        LimiteUsuarios = p.LimiteUsuarios,
        LimiteEmbarquesMes = p.LimiteEmbarquesMes,
        LimiteStorageGb = p.LimiteStorageGb,
        PrecioMensual = p.PrecioMensual,
        PrecioAnual = p.PrecioAnual,
        Moneda = p.Moneda,
        ModulosDisponibles = p.ModulosDisponibles,
        EsPublico = p.EsPublico,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion
    };
}
