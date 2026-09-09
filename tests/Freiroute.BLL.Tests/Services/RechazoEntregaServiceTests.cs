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
/// Tests de RechazoEntregaService (HU-030 — Rechazos y re-entregas).
/// </summary>
public class RechazoEntregaServiceTests
{
    private const string MotivoValido = "CLIENTE_AUSENTE";

    private readonly Mock<IOrdenService> _ordenServiceMock;
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IRechazoEntregaRepository> _rechazoRepoMock;
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly RechazoEntregaService _service;

    public RechazoEntregaServiceTests()
    {
        _ordenServiceMock = new Mock<IOrdenService>();
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _rechazoRepoMock = new Mock<IRechazoEntregaRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new RechazoEntregaService(
            _ordenServiceMock.Object, _ordenRepoMock.Object,
            _rechazoRepoMock.Object, _usuarioRepoMock.Object, _auditoriaMock.Object);
    }

    // ─── REGISTRAR RECHAZO (HU-030 CA-01/CA-02/CA-03) ──────────────

    [Fact]
    public async Task RegistrarRechazoAsync_CuandoMotivoValido_MueveAFailedDeliveryYRegistra()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var rechazoId = Guid.NewGuid();
        var orden = new OrdenBuilder()
            .ConId(ordenId)
            .ConEmpresa(empresaId)
            .ConEstado(OrdenEstado.InTransit)
            .Build();
        var dto = new RechazoEntregaRequestDto
        {
            Motivo = MotivoValido,
            Descripcion = "El cliente no estaba en el domicilio"
        };

        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(orden);
        _ordenServiceMock
            .Setup(s => s.CambiarEstadoAsync(ordenId, It.IsAny<CambiarEstadoOrdenRequestDto>(),
                empresaId, usuarioId))
            .ReturnsAsync(new OrdenResponseDto { Id = ordenId, Estado = OrdenEstado.FailedDelivery });
        _rechazoRepoMock.Setup(r => r.CreateAsync(It.IsAny<RechazoEntrega>())).ReturnsAsync(rechazoId);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(usuarioId, empresaId))
            .ReturnsAsync(new Usuario { Id = usuarioId, NombreCompleto = "Maria Lopez" });

        // ── ACT ──
        var result = await _service.RegistrarRechazoAsync(ordenId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Id.Should().Be(rechazoId);
        result.Motivo.Should().Be(MotivoValido);
        result.UsuarioNombre.Should().Be("Maria Lopez");
        // CA-03: la orden se mueve a FAILED_DELIVERY vía la FSM.
        _ordenServiceMock.Verify(s => s.CambiarEstadoAsync(ordenId,
            It.Is<CambiarEstadoOrdenRequestDto>(c =>
                c.EstadoNuevo == OrdenEstado.FailedDelivery &&
                c.Motivo!.Contains(MotivoValido)),
            empresaId, usuarioId), Times.Once);
        _rechazoRepoMock.Verify(r => r.CreateAsync(It.Is<RechazoEntrega>(e =>
            e.EmpresaId == empresaId && e.OrdenId == ordenId &&
            e.Motivo == MotivoValido && e.UsuarioId == usuarioId)), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.RECHAZO_ENTREGA, empresaId, usuarioId,
            nameof(RechazoEntrega), rechazoId,
            It.Is<object>(d => d != null && ((string)d).Contains(MotivoValido)),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarRechazoAsync_CuandoMotivoVacio_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var dto = new RechazoEntregaRequestDto { Motivo = "  " };

        // ── ACT & ASSERT ──
        var act = async () => await _service.RegistrarRechazoAsync(
            Guid.NewGuid(), dto, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*motivo del rechazo es obligatorio*");
        _ordenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarRechazoAsync_CuandoMotivoInvalido_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var dto = new RechazoEntregaRequestDto { Motivo = "NO_SE_ENCONTRO" };

        // ── ACT & ASSERT ──
        var act = async () => await _service.RegistrarRechazoAsync(
            Guid.NewGuid(), dto, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Motivo de rechazo inválido*");
        _ordenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarRechazoAsync_CuandoOrdenNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, It.IsAny<Guid>()))
            .ReturnsAsync((Orden?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.RegistrarRechazoAsync(
            ordenId, new RechazoEntregaRequestDto { Motivo = MotivoValido },
            Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Orden no encontrada*");
        _rechazoRepoMock.Verify(r => r.CreateAsync(It.IsAny<RechazoEntrega>()), Times.Never);
    }

    // ─── CREAR RE-ENTREGA (HU-030 CA-05/CA-06/CA-07) ───────────────

    [Fact]
    public async Task CrearReentregaAsync_CuandoOrdenEnFailedDelivery_CreaOrdenConfirmadaConVinculo()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var nuevaId = Guid.NewGuid();
        var original = new OrdenBuilder()
            .ConId(ordenId)
            .ConEmpresa(empresaId)
            .ConEstado(OrdenEstado.FailedDelivery)
            .Build();
        original.NumeroOrden = "ORD-2026-00001";
        var dto = new ReEntregaRequestDto { Instrucciones = "Entregar después de las 18h" };

        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(original);
        _ordenServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), empresaId, usuarioId, It.IsAny<string>()))
            .ReturnsAsync(new OrdenResponseDto { Id = nuevaId, Estado = OrdenEstado.Draft });
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevaId, empresaId))
            .ReturnsAsync(new Orden { Id = nuevaId, EmpresaId = empresaId });
        _ordenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Orden>())).ReturnsAsync(true);
        _ordenServiceMock
            .Setup(s => s.CambiarEstadoAsync(nuevaId, It.IsAny<CambiarEstadoOrdenRequestDto>(),
                empresaId, usuarioId))
            .ReturnsAsync(new OrdenResponseDto { Id = nuevaId, Estado = OrdenEstado.Confirmed });
        _ordenServiceMock
            .Setup(s => s.GetByIdAsync(nuevaId, empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = nuevaId, Estado = OrdenEstado.Confirmed });

        // ── ACT ──
        var result = await _service.CrearReentregaAsync(ordenId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Id.Should().Be(nuevaId);
        result.Estado.Should().Be(OrdenEstado.Confirmed); // CA-07: nace CONFIRMED.
        // CA-05: hereda cliente/origen/destino de la orden fallida.
        _ordenServiceMock.Verify(s => s.CreateAsync(It.Is<OrdenRequestDto>(r =>
            r.ClienteId == original.ClienteId &&
            r.OrigenId == original.OrigenId &&
            r.DestinoId == original.DestinoId &&
            r.Instrucciones == "Entregar después de las 18h"), empresaId, usuarioId, It.IsAny<string>()), Times.Once);
        // CA-06: vínculo orden_origen_id hacia la original.
        _ordenRepoMock.Verify(r => r.UpdateAsync(It.Is<Orden>(o =>
            o.Id == nuevaId && o.OrdenOrigenId == ordenId)), Times.Once);
        _ordenServiceMock.Verify(s => s.CambiarEstadoAsync(nuevaId,
            It.Is<CambiarEstadoOrdenRequestDto>(c =>
                c.EstadoNuevo == OrdenEstado.Confirmed &&
                c.Motivo!.Contains("ORD-2026-00001")),
            empresaId, usuarioId), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CREAR_REENTREGA, empresaId, usuarioId,
            nameof(Orden), nuevaId,
            It.Is<object>(d => d != null && ((string)d).Contains(ordenId.ToString())
                && ((string)d).Contains("ORD-2026-00001")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CrearReentregaAsync_CuandoOrdenNoEstaEnFailedDelivery_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenBuilder()
                .ConId(ordenId)
                .ConEmpresa(empresaId)
                .ConEstado(OrdenEstado.Confirmed)
                .Build());

        // ── ACT & ASSERT ──
        var act = async () => await _service.CrearReentregaAsync(
            ordenId, null, empresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Solo se puede crear una re-entrega desde una orden en FAILED_DELIVERY*");
        _ordenServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CrearReentregaAsync_CuandoOrdenNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, It.IsAny<Guid>()))
            .ReturnsAsync((Orden?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.CrearReentregaAsync(
            ordenId, null, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Orden no encontrada*");
    }

    // ─── HISTORIAL DE RE-ENTREGAS (HU-030 CA-10) ───────────────────

    [Fact]
    public async Task GetReentregasAsync_RetornaOrdenesMapeadas()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var reentregas = new List<Orden>
        {
            new OrdenBuilder().ConEmpresa(empresaId).ConEstado(OrdenEstado.InTransit).Build(),
            new OrdenBuilder().ConEmpresa(empresaId).ConEstado(OrdenEstado.Confirmed).Build()
        };
        _ordenRepoMock.Setup(r => r.GetSubOrdenesAsync(ordenId, empresaId)).ReturnsAsync(reentregas);

        // ── ACT ──
        var result = await _service.GetReentregasAsync(ordenId, empresaId);

        // ── ASSERT ──
        result.Should().HaveCount(2);
        result.First().EstadoLabel.Should().Be(OrdenEstado.GetLabel(OrdenEstado.InTransit));
        _ordenRepoMock.Verify(r => r.GetSubOrdenesAsync(ordenId, empresaId), Times.Once);
    }
}