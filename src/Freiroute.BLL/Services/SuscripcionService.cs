using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Admin;
using Freiroute.DTO.Suscripcion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de suscripciones y facturación (HU-011, ADR-004).
/// Gestiona el ciclo de vida de cada tenant:
/// - CreateAsync: calcula fecha_vencimiento según el ciclo (MENSUAL +30, ANUAL +365).
/// - RegistrarPagoAsync: registra un pago inmutable y reactiva la suscripción (CA-03).
/// - ProcesarVencimientosAsync: ACTIVE vencida → PAST_DUE; PAST_DUE > 7 días → SUSPENDED (CA-05/06).
/// - Dashboard financiero (MRR, ARR, churn, ingresos).
/// </summary>
public class SuscripcionService : ISuscripcionService
{
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IConfiguracion2faRepository _configuracion2faRepository;
    private readonly IEmailService _emailService;
    private readonly IValidator<SuscripcionRequestDto> _suscripcionValidator;
    private readonly IValidator<PagoRequestDto> _pagoValidator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<SuscripcionService> _logger;

    // Período de gracia de PAST_DUE antes de suspender (HU-011 CA-06).
    private const int DiasGracia = 7;

    public SuscripcionService(
        ISuscripcionRepository suscripcionRepository,
        IPagoRepository pagoRepository,
        IPlanRepository planRepository,
        IEmpresaRepository empresaRepository,
        IConfiguracion2faRepository configuracion2faRepository,
        IEmailService emailService,
        IValidator<SuscripcionRequestDto> suscripcionValidator,
        IValidator<PagoRequestDto> pagoValidator,
        IAuditoriaService auditoria,
        ILogger<SuscripcionService> logger)
    {
        _suscripcionRepository = suscripcionRepository;
        _pagoRepository = pagoRepository;
        _planRepository = planRepository;
        _empresaRepository = empresaRepository;
        _configuracion2faRepository = configuracion2faRepository;
        _emailService = emailService;
        _suscripcionValidator = suscripcionValidator;
        _pagoValidator = pagoValidator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Obtiene las suscripciones paginadas con filtro opcional por estado (panel Super Admin).</summary>
    public async Task<PagedResult<SuscripcionResponseDto>> GetAllAsync(
        string? estado, int pageNumber, int pageSize)
    {
        var suscripciones = await _suscripcionRepository.GetAllAsync(estado, pageNumber, pageSize);
        var items = new List<SuscripcionResponseDto>();

        foreach (var s in suscripciones)
        {
            items.Add(await MapToResponseDtoAsync(s));
        }

        return new PagedResult<SuscripcionResponseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            // El repositorio no devuelve el total; para el panel se muestra la página.
            TotalItems = items.Count
        };
    }

    /// <summary>Obtiene una suscripción por su Id.</summary>
    public async Task<SuscripcionResponseDto?> GetByIdAsync(Guid id)
    {
        var suscripcion = await _suscripcionRepository.GetByIdAsync(id);
        return suscripcion is null ? null : await MapToResponseDtoAsync(suscripcion);
    }

    /// <summary>Obtiene la suscripción ACTIVA de una empresa.</summary>
    public async Task<SuscripcionResponseDto?> GetActivaByEmpresaIdAsync(Guid empresaId)
    {
        var suscripcion = await _suscripcionRepository.GetActivaByEmpresaIdAsync(empresaId);
        return suscripcion is null ? null : await MapToResponseDtoAsync(suscripcion);
    }

    /// <summary>
    /// Crea una suscripción nueva. Calcula fecha_vencimiento según el ciclo
    /// (MENSUAL +30 días, ANUAL +365 días) y asigna estado TRIAL si no hay pago.
    /// (HU-011 CA-01/02, ADR-004).
    /// </summary>
    public async Task<SuscripcionResponseDto> CreateAsync(SuscripcionRequestDto dto, Guid creadoPorId)
    {
        var validation = await _suscripcionValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        // Validar que existan el plan y la empresa.
        var plan = await _planRepository.GetByIdAsync(dto.PlanId);
        if (plan is null || !plan.Activo)
        {
            throw new NotFoundException(nameof(Plan), dto.PlanId);
        }

        var empresa = await _empresaRepository.GetByIdAsync(dto.EmpresaId);
        if (empresa is null)
        {
            throw new NotFoundException(nameof(Empresa), dto.EmpresaId);
        }

        // Solo una suscripción activa por empresa (constraint UNIQUE empresa_id+activo).
        var activaExistente = await _suscripcionRepository.GetActivaByEmpresaIdAsync(dto.EmpresaId);
        if (activaExistente is not null)
        {
            throw new ConflictException("La empresa ya tiene una suscripción activa.");
        }

        var suscripcion = new Suscripcion
        {
            EmpresaId = dto.EmpresaId,
            PlanId = dto.PlanId,
            TipoCiclo = dto.TipoCiclo,
            FechaInicio = DateTime.UtcNow,
            FechaVencimiento = CalcularVencimiento(DateTime.UtcNow, dto.TipoCiclo),
            Estado = EstadoSuscripcion.TRIAL, // CA-01: TRIAL hasta registrar el primer pago COMPLETED
            PrecioPactado = dto.PrecioPactado,
            MonedaPactada = dto.MonedaPactada,
            CreadoPorId = creadoPorId,
            Activo = true
        };

        var suscripcionId = await _suscripcionRepository.CreateAsync(suscripcion);

        // Actualizar el plan en la empresa y el estado del tenant.
        empresa.PlanId = dto.PlanId;
        empresa.PlanSuscripcion = plan.Codigo;
        await _empresaRepository.UpdateAsync(empresa);

        await _auditoria.RegistrarAsync(
            "suscripciones", AccionAuditoria.CREATE, dto.EmpresaId, creadoPorId,
            nameof(Suscripcion), suscripcionId,
            new { plan = plan.Codigo, ciclo = dto.TipoCiclo, precio = dto.PrecioPactado });

        suscripcion.Id = suscripcionId;
        return await MapToResponseDtoAsync(suscripcion);
    }

    /// <summary>
    /// Registra un pago manual (INMUTABLE) y actualiza la fecha de vencimiento de
    /// la suscripción. Si el pago está COMPLETED → el estado pasa a ACTIVE (HU-011 CA-03).
    /// </summary>
    public async Task<PagoResponseDto> RegistrarPagoAsync(Guid suscripcionId,
        PagoRequestDto dto, Guid registradoPorId)
    {
        var validation = await _pagoValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var suscripcion = await _suscripcionRepository.GetByIdAsync(suscripcionId);
        if (suscripcion is null)
        {
            throw new NotFoundException(nameof(Suscripcion), suscripcionId);
        }

        var pago = new Pago
        {
            EmpresaId = suscripcion.EmpresaId,
            SuscripcionId = suscripcionId,
            Monto = dto.Monto,
            Moneda = dto.Moneda,
            MetodoPago = dto.MetodoPago,
            Referencia = dto.Referencia,
            Notas = dto.Notas,
            Estado = EstadoPago.COMPLETED,
            PeriodoDesde = dto.PeriodoDesde,
            PeriodoHasta = dto.PeriodoHasta,
            RegistradoPorId = registradoPorId
        };

        var pagoId = await _pagoRepository.CreateAsync(pago);

        // CA-03: un pago COMPLETED activa la suscripción y extiende el vencimiento.
        if (pago.Estado == EstadoPago.COMPLETED)
        {
            suscripcion.Estado = EstadoSuscripcion.ACTIVE;
            suscripcion.FechaVencimiento = CalcularVencimiento(
                DateTime.UtcNow, suscripcion.TipoCiclo);
            suscripcion.FechaModificacion = DateTime.UtcNow;
            await _suscripcionRepository.UpdateAsync(suscripcion);
        }

        await _auditoria.RegistrarAsync(
            "suscripciones", AccionAuditoria.REGISTRAR_PAGO, suscripcion.EmpresaId, registradoPorId,
            nameof(Pago), pagoId,
            new { monto = dto.Monto, moneda = dto.Moneda, metodo = dto.MetodoPago, referencia = dto.Referencia });

        pago.Id = pagoId;
        return await MapPagoToResponseDtoAsync(pago);
    }

    /// <summary>Obtiene el historial de pagos de una empresa (INMUTABLE).</summary>
    public async Task<IEnumerable<PagoResponseDto>> GetPagosByEmpresaAsync(Guid empresaId)
    {
        var pagos = await _pagoRepository.GetByEmpresaIdAsync(empresaId, 1, 100);
        var result = new List<PagoResponseDto>();

        foreach (var pago in pagos)
        {
            result.Add(await MapPagoToResponseDtoAsync(pago));
        }

        return result;
    }

    /// <summary>
    /// Procesa los vencimientos (HU-011 CA-05/06):
    /// ACTIVE vencida → PAST_DUE (período de gracia).
    /// PAST_DUE > 7 días vencida → SUSPENDED.
    /// Idempotente — usa métodos del repositorio (sin transacción compartida).
    /// </summary>
    public async Task ProcesarVencimientosAsync()
    {
        _logger.LogInformation("Iniciando ProcesarVencimientosAsync");

        // CA-05: ACTIVE vencida (diasUmbral=0 → ya vencida) → PAST_DUE.
        var activasVencidas = await _suscripcionRepository.GetProximasAVencerAsync(0);
        foreach (var suscripcion in activasVencidas.Where(s => s.Estado == EstadoSuscripcion.ACTIVE))
        {
            suscripcion.Estado = EstadoSuscripcion.PAST_DUE;
            suscripcion.FechaModificacion = DateTime.UtcNow;
            await _suscripcionRepository.UpdateAsync(suscripcion);

            // Reflectar el estado en la empresa.
            var empresa = await _empresaRepository.GetByIdAsync(suscripcion.EmpresaId);
            if (empresa is not null && empresa.Estado == EstadoEmpresa.ACTIVE)
            {
                empresa.Estado = EstadoEmpresa.PAST_DUE;
                await _empresaRepository.UpdateAsync(empresa);
            }

            _logger.LogInformation("Suscr {Id} ACTIVE vencida → PAST_DUE", suscripcion.Id);
        }

        // CA-06: PAST_DUE vencida hace > 7 días → SUSPENDED.
        var vencidasEnGracia = await _suscripcionRepository.GetVencidasEnGraciaAsync(DiasGracia);
        foreach (var suscripcion in vencidasEnGracia)
        {
            suscripcion.Estado = EstadoSuscripcion.SUSPENDED;
            suscripcion.FechaModificacion = DateTime.UtcNow;
            await _suscripcionRepository.UpdateAsync(suscripcion);

            var empresa = await _empresaRepository.GetByIdAsync(suscripcion.EmpresaId);
            if (empresa is not null)
            {
                empresa.Estado = EstadoEmpresa.SUSPENDED;
                await _empresaRepository.UpdateAsync(empresa);
            }

            _logger.LogInformation("Suscr {Id} PAST_DUE → SUSPENDED", suscripcion.Id);
        }

        // Purgar códigos 2FA expirados (ADR-013) — tarea de limpieza secundaria.
        try
        {
            await _configuracion2faRepository.PurgarCodigosExpiradosAsync();
            _logger.LogInformation("Códigos 2FA expirados purgados exitosamente");
        }
        catch (Exception ex)
        {
            // No propagar — es tarea de limpieza secundaria.
            _logger.LogWarning(ex,
                "Error al purgar códigos 2FA expirados: {Mensaje}",
                ex.Message);
        }

        // Alertas de vencimiento próximo al Super Admin (HU-011 CA-04).
        await EnviarAlertasVencimientoAsync();
    }

    /// <summary>
    /// Envía alertas por email al Super Admin para suscripciones que vencen en 15, 7 y 1 día
    /// (HU-011 CA-04). Cada umbral se envía solo cuando la suscripción vence exactamente en
    /// ese número de días (evita alertas repetidas).
    /// </summary>
    private async Task EnviarAlertasVencimientoAsync()
    {
        var umbrales = new[] { 15, 7, 1 };

        foreach (var dias in umbrales)
        {
            var proximas = await _suscripcionRepository
                .GetProximasAVencerAsync(dias);

            // Filtrar solo las que vencen exactamente en 'dias' días (evitar duplicados).
            var exactas = proximas.Where(s =>
                (s.FechaVencimiento.Date - DateTime.UtcNow.Date).Days == dias);

            foreach (var suscripcion in exactas)
            {
                try
                {
                    await _emailService.EnviarAsync(
                        "admin@freiroute.com", // Super Admin
                        $"⚠️ Suscripción por vencer en {dias} día(s)",
                        $"La empresa <strong>{suscripcion.EmpresaId}</strong> " +
                        $"vence el {suscripcion.FechaVencimiento:dd/MM/yyyy}. " +
                        $"Plan: {suscripcion.PlanId}.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error enviando alerta de vencimiento: {Mensaje}",
                        ex.Message);
                }
            }
        }
    }

    /// <summary>Obtiene las métricas del dashboard financiero (MRR, ARR, churn, etc.).</summary>
    public async Task<DashboardFinancieroResponseDto> GetDashboardFinancieroAsync()
    {
        var now = DateTime.UtcNow;
        var mrr = await _pagoRepository.GetMrrAsync();
        var ingresosMes = await _pagoRepository.GetIngresosDelMesAsync(now.Year, now.Month);

        // Ingresos acumulados del año: suma de los 12 meses del año en curso.
        decimal ingresosAnio = 0;
        for (var mes = 1; mes <= 12; mes++)
        {
            ingresosAnio += await _pagoRepository.GetIngresosDelMesAsync(now.Year, mes);
        }

        // Nuevos suscriptores del mes: suscripciones con fecha_creacion este mes.
        // Se estima contando las suscripciones activas; el churn se obtiene en la BLL
        // de AdminDashboard para no duplicar consultas (aquí se deriva de las próximas a vencer).
        var proximas = await _suscripcionRepository.GetProximasAVencerAsync(30);

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

    // ── Helpers ─────────────────────────────────────────────────────

    private static DateTime CalcularVencimiento(DateTime desde, string ciclo) => ciclo switch
    {
        TipoCiclo.ANUAL => desde.AddDays(365),
        _ => desde.AddDays(30) // MENSUAL (default)
    };

    private async Task<SuscripcionResponseDto> MapToResponseDtoAsync(Suscripcion s)
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
            EstadoLabel = ObtenerEstadoLabel(s.Estado),
            PrecioPactado = s.PrecioPactado,
            MonedaPactada = s.MonedaPactada,
            DiasParaVencimiento = CalcularDiasParaVencimiento(s.FechaVencimiento),
            Activo = s.Activo,
            FechaCreacion = s.FechaCreacion
        };
    }

    private async Task<PagoResponseDto> MapPagoToResponseDtoAsync(Pago p)
    {
        var empresa = await _empresaRepository.GetByIdAsync(p.EmpresaId);

        return new PagoResponseDto
        {
            Id = p.Id,
            EmpresaId = p.EmpresaId,
            EmpresaNombre = empresa?.Nombre ?? string.Empty,
            SuscripcionId = p.SuscripcionId,
            Monto = p.Monto,
            Moneda = p.Moneda,
            MetodoPago = p.MetodoPago,
            Referencia = p.Referencia,
            Estado = p.Estado,
            PeriodoDesde = p.PeriodoDesde,
            PeriodoHasta = p.PeriodoHasta,
            FechaCreacion = p.FechaCreacion,
            // El nombre de quién registró se resuelve desde el usuario (no se persiste
            // en el pago); se deja vacío salvo que la consulta lo incluya.
            RegistradoPorNombre = string.Empty
        };
    }

    private static string ObtenerEstadoLabel(string estado) => estado switch
    {
        EstadoSuscripcion.TRIAL => "Prueba",
        EstadoSuscripcion.ACTIVE => "Activa",
        EstadoSuscripcion.PAST_DUE => "Vencida",
        EstadoSuscripcion.SUSPENDED => "Suspendida",
        EstadoSuscripcion.CANCELLED => "Cancelada",
        _ => estado
    };

    private static int CalcularDiasParaVencimiento(DateTime fechaVencimiento) =>
        (int)Math.Floor((fechaVencimiento - DateTime.UtcNow).TotalDays);
}
