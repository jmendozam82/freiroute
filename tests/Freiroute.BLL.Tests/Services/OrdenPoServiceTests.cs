using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Tests.Builders;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de OrdenPoService (HU-028 — PO/SO Integration).
/// </summary>
public class OrdenPoServiceTests
{
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IOrdenService> _ordenServiceMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly OrdenPoService _service;

    public OrdenPoServiceTests()
    {
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _ordenServiceMock = new Mock<IOrdenService>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new OrdenPoService(
            _ordenRepoMock.Object, _ordenServiceMock.Object, _auditoriaMock.Object);
    }

    // ─── GET POR PO (HU-028 CA-05) ─────────────────────────────────

    [Fact]
    public async Task GetPorPoAsync_CuandoExistenOrdenesConPo_RetornaListaMapeada()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenes = new List<Orden>
        {
            new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build(),
            new OrdenBuilder().ConEstado(OrdenEstado.InTransit).Build()
        };
        ordenes.ForEach(o => o.NumeroPo = "PO-1000");

        _ordenRepoMock
            .Setup(r => r.GetPorPoAsync("PO-1000", empresaId))
            .ReturnsAsync(ordenes);

        // ── ACT ──
        var result = await _service.GetPorPoAsync("PO-1000", empresaId);

        // ── ASSERT ──
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.NumeroPo == "PO-1000");
        result.First().EstadoLabel.Should().Be(OrdenEstado.GetLabel(OrdenEstado.Confirmed));
        _ordenRepoMock.Verify(r => r.GetPorPoAsync("PO-1000", empresaId), Times.Once);
    }

    [Fact]
    public async Task GetPorPoAsync_CuandoNoExisten_RetornaListaVacia()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        _ordenRepoMock
            .Setup(r => r.GetPorPoAsync(It.IsAny<string>(), empresaId))
            .ReturnsAsync(new List<Orden>());

        // ── ACT ──
        var result = await _service.GetPorPoAsync("PO-999", empresaId);

        // ── ASSERT ──
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPorPoAsync_RecortaEspaciosDelNumero_ConsultaSinEspacios()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        _ordenRepoMock
            .Setup(r => r.GetPorPoAsync(It.IsAny<string>(), empresaId))
            .ReturnsAsync(new List<Orden>());

        // ── ACT ──
        await _service.GetPorPoAsync("  PO-1000  ", empresaId);

        // ── ASSERT ──
        // La búsqueda es exacta (no ILIKE) — el trim es responsabilidad del servicio.
        _ordenRepoMock.Verify(r => r.GetPorPoAsync("PO-1000", empresaId), Times.Once);
    }

    // ─── VINCULAR PO (HU-028 CA-09) ────────────────────────────────

    [Fact]
    public async Task VincularPoAsync_CuandoOrdenExiste_ActualizaPoSoYAudita()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var orden = new OrdenBuilder().ConId(ordenId).ConEmpresa(empresaId).Build();
        var dto = new OrdenPoRequestDto { NumeroPo = "PO-500", NumeroSo = "SO-77" };

        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Orden>())).ReturnsAsync(true);
        _ordenServiceMock
            .Setup(s => s.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = ordenId, NumeroPo = "PO-500" });

        // ── ACT ──
        var result = await _service.VincularPoAsync(ordenId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.NumeroPo.Should().Be("PO-500");
        _ordenRepoMock.Verify(r => r.UpdateAsync(It.Is<Orden>(o =>
            o.NumeroPo == "PO-500" && o.NumeroSo == "SO-77")), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.VINCULAR_PO, empresaId, usuarioId,
            nameof(Orden), ordenId,
            It.Is<object>(d => d != null && ((string)d).Contains("PO-500")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task VincularPoAsync_CuandoOrdenNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync((Orden?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.VincularPoAsync(
            ordenId, new OrdenPoRequestDto { NumeroPo = "PO-1" }, empresaId, usuarioId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Orden no encontrada*");
        _ordenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Orden>()), Times.Never);
    }

    [Fact]
    public async Task VincularPoAsync_CuandoUpdateFalla_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenBuilder().ConId(ordenId).Build());
        _ordenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Orden>())).ReturnsAsync(false);

        // ── ACT & ASSERT ──
        var act = async () => await _service.VincularPoAsync(
            ordenId, new OrdenPoRequestDto { NumeroPo = "PO-1" }, empresaId, usuarioId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*No se pudo actualizar la orden*");
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task AuditarCambioPo_RegistraAuditoriaVinculacion()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();

        // ── ACT ──
        await _service.AuditarCambioPo(ordenId, "PO-88", empresaId, usuarioId);

        // ── ASSERT ──
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.VINCULAR_PO, empresaId, usuarioId,
            nameof(Orden), ordenId,
            It.Is<object>(d => d != null && ((string)d).Contains("PO-88")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }
}