using FluentValidation;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Suscripcion;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del ciclo de suscripciones y facturación (HU-011).
/// Cubre el cálculo de vencimiento por ciclo (CA-01), la reactivación por pago
/// COMPLETED (CA-03) y la máquina de estados ACTIVE → PAST_DUE → SUSPENDED (CA-05/06).
/// </summary>
public class SuscripcionServiceTests
{
    private readonly Mock<ISuscripcionRepository> _susc;
    private readonly Mock<IPagoRepository> _pagos;
    private readonly Mock<IPlanRepository> _planRepo;
    private readonly Mock<IEmpresaRepository> _empresas;
    private readonly Mock<IConfiguracion2faRepository> _config2fa;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IValidator<SuscripcionRequestDto>> _suscValidator;
    private readonly Mock<IValidator<PagoRequestDto>> _pagoValidator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly SuscripcionService _service;

    public SuscripcionServiceTests()
    {
        _susc = new Mock<ISuscripcionRepository>();
        _pagos = new Mock<IPagoRepository>();
        _planRepo = new Mock<IPlanRepository>();
        _empresas = new Mock<IEmpresaRepository>();
        _config2fa = new Mock<IConfiguracion2faRepository>();
        _emailService = new Mock<IEmailService>();
        _suscValidator = new Mock<IValidator<SuscripcionRequestDto>>();
        _pagoValidator = new Mock<IValidator<PagoRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _suscValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SuscripcionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _pagoValidator
            .Setup(v => v.ValidateAsync(It.IsAny<PagoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new SuscripcionService(
            _susc.Object,
            _pagos.Object,
            _planRepo.Object,
            _empresas.Object,
            _config2fa.Object,
            _emailService.Object,
            _suscValidator.Object,
            _pagoValidator.Object,
            _auditoria.Object,
            Mock.Of<ILogger<SuscripcionService>>());
    }

    private static Plan PlanActivo() => new() { Id = Guid.NewGuid(), Nombre = "Pro", Codigo = "PROFESSIONAL", Activo = true };

    private static Empresa EmpresaActiva(Guid id) => new() { Id = id, Nombre = "Trans SA", Estado = EstadoEmpresa.ACTIVE, Activo = true };

    [Fact]
    public async Task CreateAsync_Mensual_CalculaVencimiento30Dias()
    {
        var plan = PlanActivo();
        var empresa = EmpresaActiva(Guid.NewGuid());
        var id = Guid.NewGuid();
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id)).ReturnsAsync((Suscripcion?)null);
        _susc.Setup(r => r.CreateAsync(It.IsAny<Suscripcion>())).ReturnsAsync(id);

        var dto = new SuscripcionRequestDto
        {
            EmpresaId = empresa.Id, PlanId = plan.Id, TipoCiclo = TipoCiclo.MENSUAL,
            PrecioPactado = 99, MonedaPactada = "USD"
        };

        var result = await _service.CreateAsync(dto, creadoPorId: Guid.NewGuid());

        result.Estado.Should().Be(EstadoSuscripcion.TRIAL);
        result.FechaVencimiento.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_Anual_CalculaVencimiento365Dias()
    {
        var plan = PlanActivo();
        var empresa = EmpresaActiva(Guid.NewGuid());
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id)).ReturnsAsync((Suscripcion?)null);
        _susc.Setup(r => r.CreateAsync(It.IsAny<Suscripcion>())).ReturnsAsync(Guid.NewGuid());

        var dto = new SuscripcionRequestDto
        {
            EmpresaId = empresa.Id, PlanId = plan.Id, TipoCiclo = TipoCiclo.ANUAL,
            PrecioPactado = 990, MonedaPactada = "USD"
        };

        var result = await _service.CreateAsync(dto, creadoPorId: Guid.NewGuid());

        result.FechaVencimiento.Should().BeCloseTo(DateTime.UtcNow.AddDays(365), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_PlanInactivo_LanzaNotFound()
    {
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Plan?)null);

        var act = async () => await _service.CreateAsync(
            new SuscripcionRequestDto { EmpresaId = Guid.NewGuid(), PlanId = Guid.NewGuid() },
            creadoPorId: Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_EmpresaConSuscripcionActiva_LanzaConflict()
    {
        var plan = PlanActivo();
        var empresa = EmpresaActiva(Guid.NewGuid());
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id))
            .ReturnsAsync(new Suscripcion { Id = Guid.NewGuid(), EmpresaId = empresa.Id });

        var act = async () => await _service.CreateAsync(
            new SuscripcionRequestDto { EmpresaId = empresa.Id, PlanId = plan.Id, TipoCiclo = TipoCiclo.MENSUAL },
            creadoPorId: Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegistrarPagoAsync_Completado_ActivaSuscripcionYExtiende()
    {
        var plan = PlanActivo();
        var empresa = EmpresaActiva(Guid.NewGuid());
        var suscId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _susc.Setup(r => r.GetByIdAsync(suscId)).ReturnsAsync(
            new Suscripcion
            {
                Id = suscId, EmpresaId = empresa.Id, PlanId = plan.Id,
                TipoCiclo = TipoCiclo.MENSUAL, Estado = EstadoSuscripcion.TRIAL
            });
        _pagos.Setup(r => r.CreateAsync(It.IsAny<Pago>())).ReturnsAsync(Guid.NewGuid());

        var dto = new PagoRequestDto
        {
            Monto = 99, Moneda = "USD", MetodoPago = "MANUAL",
            PeriodoDesde = DateTime.UtcNow, PeriodoHasta = DateTime.UtcNow.AddDays(30)
        };

        var result = await _service.RegistrarPagoAsync(suscId, dto, registradoPorId: Guid.NewGuid());

        result.Estado.Should().Be(EstadoPago.COMPLETED);
        // Se actualiza la suscripción a ACTIVE.
        _susc.Verify(r => r.UpdateAsync(It.Is<Suscripcion>(s => s.Estado == EstadoSuscripcion.ACTIVE)), Times.Once);
    }

    [Fact]
    public async Task RegistrarPagoAsync_SuscripcionNoExiste_LanzaNotFound()
    {
        _susc.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Suscripcion?)null);

        var act = async () => await _service.RegistrarPagoAsync(
            Guid.NewGuid(),
            new PagoRequestDto { Monto = 99, PeriodoDesde = DateTime.UtcNow, PeriodoHasta = DateTime.UtcNow.AddDays(30) },
            registradoPorId: Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ProcesarVencimientosAsync_ActiveVencida_PasaAPastDueYEmpresaSync()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        var activa = new Suscripcion
        {
            Id = Guid.NewGuid(), EmpresaId = empresa.Id, PlanId = Guid.NewGuid(),
            Estado = EstadoSuscripcion.ACTIVE, FechaVencimiento = DateTime.UtcNow.AddDays(-1)
        };
        _susc.Setup(r => r.GetProximasAVencerAsync(0)).ReturnsAsync([activa]);
        _susc.Setup(r => r.GetVencidasEnGraciaAsync(It.IsAny<int>())).ReturnsAsync([]);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        await _service.ProcesarVencimientosAsync();

        _susc.Verify(r => r.UpdateAsync(It.Is<Suscripcion>(s => s.Estado == EstadoSuscripcion.PAST_DUE)), Times.Once);
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e => e.Estado == EstadoEmpresa.PAST_DUE)), Times.Once);
    }

    [Fact]
    public async Task ProcesarVencimientosAsync_PastDueVencida_PasaASuspended()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        var pastDue = new Suscripcion
        {
            Id = Guid.NewGuid(), EmpresaId = empresa.Id, PlanId = Guid.NewGuid(),
            Estado = EstadoSuscripcion.PAST_DUE
        };
        _susc.Setup(r => r.GetProximasAVencerAsync(0)).ReturnsAsync([]);
        _susc.Setup(r => r.GetVencidasEnGraciaAsync(7)).ReturnsAsync([pastDue]);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        await _service.ProcesarVencimientosAsync();

        _susc.Verify(r => r.UpdateAsync(It.Is<Suscripcion>(s => s.Estado == EstadoSuscripcion.SUSPENDED)), Times.Once);
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e => e.Estado == EstadoEmpresa.SUSPENDED)), Times.Once);
    }

    [Fact]
    public async Task GetDashboardFinancieroAsync_CalculaMrrArrIngresos()
    {
        _pagos.Setup(r => r.GetMrrAsync()).ReturnsAsync(1000m);
        _pagos.Setup(r => r.GetIngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(500m);
        _susc.Setup(r => r.GetProximasAVencerAsync(30)).ReturnsAsync([]);

        var result = await _service.GetDashboardFinancieroAsync();

        result.Mrr.Should().Be(1000m);
        result.Arr.Should().Be(12000m);
        result.IngresosMes.Should().Be(500m);
    }

    // ── GetAllAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ConEstado_ReturnaPagedResultConItems()
    {
        // Arrange
        var empresa = EmpresaActiva(Guid.NewGuid());
        var plan = PlanActivo();
        var suscripcion = new Suscripcion
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            PlanId = plan.Id,
            Estado = EstadoSuscripcion.ACTIVE,
            TipoCiclo = TipoCiclo.MENSUAL,
            PrecioPactado = 99,
            MonedaPactada = "USD",
            Activo = true,
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaVencimiento = DateTime.UtcNow.AddDays(30)
        };

        _susc.Setup(r => r.GetAllAsync(EstadoSuscripcion.ACTIVE, 1, 20))
            .ReturnsAsync(new List<Suscripcion> { suscripcion });
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        // Act
        var result = await _service.GetAllAsync(EstadoSuscripcion.ACTIVE, 1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.First().EmpresaNombre.Should().Be("Trans SA");
        result.Items.First().PlanNombre.Should().Be("Pro");

        _susc.Verify(r => r.GetAllAsync(EstadoSuscripcion.ACTIVE, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ConEstadoNull_PasaNullAlRepositorio()
    {
        // Arrange
        _susc.Setup(r => r.GetAllAsync(null, 1, 10))
            .ReturnsAsync(new List<Suscripcion>());

        // Act
        var result = await _service.GetAllAsync(null, 1, 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        _susc.Verify(r => r.GetAllAsync(null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ConMultiplesSuscripciones_RetornaTodosLosItems()
    {
        // Arrange
        var empresa = EmpresaActiva(Guid.NewGuid());
        var plan = PlanActivo();
        var suscripciones = new List<Suscripcion>
        {
            new()
            {
                Id = Guid.NewGuid(), EmpresaId = empresa.Id, PlanId = plan.Id,
                Estado = EstadoSuscripcion.ACTIVE, TipoCiclo = TipoCiclo.MENSUAL,
                PrecioPactado = 99, MonedaPactada = "USD", Activo = true,
                FechaInicio = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddDays(30)
            },
            new()
            {
                Id = Guid.NewGuid(), EmpresaId = empresa.Id, PlanId = plan.Id,
                Estado = EstadoSuscripcion.PAST_DUE, TipoCiclo = TipoCiclo.ANUAL,
                PrecioPactado = 990, MonedaPactada = "USD", Activo = true,
                FechaInicio = DateTime.UtcNow.AddDays(-200), FechaVencimiento = DateTime.UtcNow.AddDays(-5)
            }
        };

        _susc.Setup(r => r.GetAllAsync(null, 1, 20)).ReturnsAsync(suscripciones);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        // Act
        var result = await _service.GetAllAsync(null, 1, 20);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_SuscripcionExiste_RetornaDto()
    {
        // Arrange
        var empresa = EmpresaActiva(Guid.NewGuid());
        var plan = PlanActivo();
        var suscripcionId = Guid.NewGuid();
        var suscripcion = new Suscripcion
        {
            Id = suscripcionId,
            EmpresaId = empresa.Id,
            PlanId = plan.Id,
            Estado = EstadoSuscripcion.ACTIVE,
            TipoCiclo = TipoCiclo.MENSUAL,
            PrecioPactado = 99,
            MonedaPactada = "USD",
            Activo = true,
            FechaInicio = DateTime.UtcNow.AddDays(-15),
            FechaVencimiento = DateTime.UtcNow.AddDays(15)
        };

        _susc.Setup(r => r.GetByIdAsync(suscripcionId)).ReturnsAsync(suscripcion);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        // Act
        var result = await _service.GetByIdAsync(suscripcionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(suscripcionId);
        result.EmpresaNombre.Should().Be("Trans SA");
        result.PlanNombre.Should().Be("Pro");
        result.Estado.Should().Be(EstadoSuscripcion.ACTIVE);
    }

    [Fact]
    public async Task GetByIdAsync_SuscripcionNoExiste_RetornaNull()
    {
        // Arrange
        _susc.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Suscripcion?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ── GetActivaByEmpresaIdAsync ──────────────────────────────────

    [Fact]
    public async Task GetActivaByEmpresaIdAsync_ExisteActiva_RetornaDto()
    {
        // Arrange
        var empresa = EmpresaActiva(Guid.NewGuid());
        var plan = PlanActivo();
        var suscripcion = new Suscripcion
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            PlanId = plan.Id,
            Estado = EstadoSuscripcion.ACTIVE,
            TipoCiclo = TipoCiclo.MENSUAL,
            PrecioPactado = 99,
            MonedaPactada = "USD",
            Activo = true,
            FechaInicio = DateTime.UtcNow.AddDays(-10),
            FechaVencimiento = DateTime.UtcNow.AddDays(20)
        };

        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id)).ReturnsAsync(suscripcion);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        // Act
        var result = await _service.GetActivaByEmpresaIdAsync(empresa.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Estado.Should().Be(EstadoSuscripcion.ACTIVE);
        result.EmpresaNombre.Should().Be("Trans SA");
        result.PlanNombre.Should().Be("Pro");
    }

    [Fact]
    public async Task GetActivaByEmpresaIdAsync_NoExiste_RetornaNull()
    {
        // Arrange
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Suscripcion?)null);

        // Act
        var result = await _service.GetActivaByEmpresaIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ── GetPagosByEmpresaAsync ─────────────────────────────────────

    [Fact]
    public async Task GetPagosByEmpresaAsync_RetornaPagos()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        var empresa = EmpresaActiva(empresaId);
        var pagos = new List<Pago>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                SuscripcionId = Guid.NewGuid(),
                Monto = 99,
                Moneda = "USD",
                MetodoPago = "MANUAL",
                Estado = EstadoPago.COMPLETED,
                PeriodoDesde = DateTime.UtcNow.AddMonths(-1),
                PeriodoHasta = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                SuscripcionId = Guid.NewGuid(),
                Monto = 990,
                Moneda = "USD",
                MetodoPago = "MANUAL",
                Estado = EstadoPago.COMPLETED,
                PeriodoDesde = DateTime.UtcNow.AddDays(-365),
                PeriodoHasta = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow.AddDays(-365)
            }
        };

        _pagos.Setup(r => r.GetByEmpresaIdAsync(empresaId, 1, 100)).ReturnsAsync(pagos);
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(empresa);

        // Act
        var result = (await _service.GetPagosByEmpresaAsync(empresaId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Monto.Should().Be(99);
        result[1].Monto.Should().Be(990);
        result[0].EmpresaNombre.Should().Be("Trans SA");

        _pagos.Verify(r => r.GetByEmpresaIdAsync(empresaId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetPagosByEmpresaAsync_SinPagos_RetornaListaVacia()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        _pagos.Setup(r => r.GetByEmpresaIdAsync(empresaId, 1, 100))
            .ReturnsAsync(new List<Pago>());

        // Act
        var result = (await _service.GetPagosByEmpresaAsync(empresaId)).ToList();

        // Assert
        result.Should().BeEmpty();
        _pagos.Verify(r => r.GetByEmpresaIdAsync(empresaId, 1, 100), Times.Once);
    }
}
