using FluentAssertions;
using Freiroute.BLL.Services;
using Freiroute.BLL.Tests.Builders;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Orders;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de SlaService (HU-031 — Monitoreo de SLA por cliente).
/// </summary>
public class SlaServiceTests
{
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly SlaService _service;

    public SlaServiceTests()
    {
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _service = new SlaService(_ordenRepoMock.Object, _clienteRepoMock.Object);
    }

    // ─── ÓRDENES EN RIESGO (HU-031 CA-02) ──────────────────────────

    [Fact]
    public async Task GetEnRiesgoAsync_RetornaOrdenesEnRiesgoMapeadas()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenes = new List<Orden>
        {
            new OrdenBuilder().ConEstado(OrdenEstado.InTransit).Build(),
            new OrdenBuilder().ConEstado(OrdenEstado.Assigned).Build()
        };
        _ordenRepoMock.Setup(r => r.GetSlaEnRiesgoAsync(empresaId)).ReturnsAsync(ordenes);

        // ── ACT ──
        var result = await _service.GetEnRiesgoAsync(empresaId);

        // ── ASSERT ──
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => !SlaCalculator.EsEstadoFinal(o.Estado));
        _ordenRepoMock.Verify(r => r.GetSlaEnRiesgoAsync(empresaId), Times.Once);
    }

    // ─── CUMPLIMIENTO POR CLIENTE (HU-031 CA-03) ───────────────────

    [Fact]
    public async Task GetCumplimientoClienteAsync_CuandoClienteExiste_CalculaPorcentaje()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var cliente = new Cliente
        {
            Id = clienteId,
            EmpresaId = empresaId,
            Nombre = "Distribuidora ABC S.A.",
            SlaDiasEntrega = 5
        };
        _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId, empresaId)).ReturnsAsync(cliente);
        _ordenRepoMock
            .Setup(r => r.GetSlaClienteAsync(clienteId, empresaId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((10, 8));

        // ── ACT ──
        var result = await _service.GetCumplimientoClienteAsync(clienteId, empresaId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result.ClienteId.Should().Be(clienteId);
        result.ClienteNombre.Should().Be("Distribuidora ABC S.A.");
        result.TotalOrdenes.Should().Be(10);
        result.OrdenesATiempo.Should().Be(8);
        result.OrdenesTardias.Should().Be(2);
        result.PorcentajeCumplimiento.Should().Be(80.00m);
        result.SlaDias.Should().Be(5);
        // La ventana es de 30 días.
        _ordenRepoMock.Verify(r => r.GetSlaClienteAsync(clienteId, empresaId,
            It.Is<DateTime>(d => d > DateTime.UtcNow.AddDays(-31)),
            It.Is<DateTime>(d => d <= DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task GetCumplimientoClienteAsync_CuandoClienteNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var clienteId = Guid.NewGuid();
        _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId, It.IsAny<Guid>()))
            .ReturnsAsync((Cliente?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.GetCumplimientoClienteAsync(clienteId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Cliente no encontrado*");
        _ordenRepoMock.Verify(r => r.GetSlaClienteAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetCumplimientoClienteAsync_CuandoSinOrdenes_PorcentajeCero()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId, empresaId))
            .ReturnsAsync(new Cliente { Id = clienteId, EmpresaId = empresaId, Nombre = "Sin movimientos" });
        _ordenRepoMock
            .Setup(r => r.GetSlaClienteAsync(clienteId, empresaId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((0, 0));

        // ── ACT ──
        var result = await _service.GetCumplimientoClienteAsync(clienteId, empresaId);

        // ── ASSERT ──
        result.PorcentajeCumplimiento.Should().Be(0m);
        result.SlaDias.Should().Be(30); // Default cuando el cliente no tiene SLA configurado.
    }

    // ─── REPORTE SLA (HU-031 CA-07) ────────────────────────────────

    [Fact]
    public async Task GetReporteSlaAsync_RetornaReporteDelRepositorio()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var desde = DateTime.UtcNow.AddDays(-30);
        var hasta = DateTime.UtcNow;
        var reporte = new List<SlaReporteItemDto>
        {
            new() { ClienteId = Guid.NewGuid(), ClienteNombre = "Cliente A", PorcentajeCumplimiento = 75.5m }
        };
        _ordenRepoMock.Setup(r => r.GetSlaReporteAsync(empresaId, desde, hasta)).ReturnsAsync(reporte);

        // ── ACT ──
        var result = await _service.GetReporteSlaAsync(empresaId, desde, hasta);

        // ── ASSERT ──
        result.Should().HaveCount(1);
        result.First().PorcentajeCumplimiento.Should().Be(75.5m);
    }

    // ─── CALCULO DE ESTADO SLA (HU-031 CA-04/CA-05/CA-06) ──────────

    [Fact]
    public void CalcularSlaStatus_CuandoEstadoFinal_RetornaOk()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(DateTime.UtcNow.AddHours(-2), OrdenEstado.Delivered);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.Ok);
    }

    [Fact]
    public void CalcularSlaStatus_CuandoSinFechaRequerida_RetornaOk()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(null, OrdenEstado.InTransit);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.Ok);
    }

    [Fact]
    public void CalcularSlaStatus_CuandoFechaVencida_RetornaVencido()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(DateTime.UtcNow.AddHours(-1), OrdenEstado.InTransit);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.Vencido);
    }

    [Fact]
    public void CalcularSlaStatus_CuandoFechaDentroDe6Horas_RetornaCritico()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(DateTime.UtcNow.AddHours(3), OrdenEstado.InTransit);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.Critico);
    }

    [Fact]
    public void CalcularSlaStatus_CuandoFechaDentroDe24Horas_RetornaEnRiesgo()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(DateTime.UtcNow.AddHours(12), OrdenEstado.InTransit);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.EnRiesgo);
    }

    [Fact]
    public void CalcularSlaStatus_CuandoFechaSupera24Horas_RetornaOk()
    {
        // ── ACT ──
        var result = _service.CalcularSlaStatus(DateTime.UtcNow.AddHours(48), OrdenEstado.InTransit);

        // ── ASSERT ──
        result.Should().Be(SlaStatus.Ok);
    }
}