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
/// Tests de PrioridadOrdenService (HU-029 — Prioridades dinámicas).
/// </summary>
public class PrioridadOrdenServiceTests
{
    private readonly Mock<IOrdenService> _ordenServiceMock;
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IReglaPrioridadRepository> _reglaRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly PrioridadOrdenService _service;

    public PrioridadOrdenServiceTests()
    {
        _ordenServiceMock = new Mock<IOrdenService>();
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _reglaRepoMock = new Mock<IReglaPrioridadRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new PrioridadOrdenService(
            _ordenServiceMock.Object, _ordenRepoMock.Object,
            _reglaRepoMock.Object, _clienteRepoMock.Object, _auditoriaMock.Object);
    }

    // ─── CAMBIO MANUAL (HU-029 CA-09) ──────────────────────────────

    [Fact]
    public async Task CambiarPrioridadAsync_CuandoPrioridadValida_ActualizaAuditaYRetorna()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var orden = new OrdenBuilder()
            .ConId(ordenId)
            .ConEmpresa(empresaId)
            .ConPrioridad(OrdenPrioridad.Normal)
            .Build();
        var dto = new PrioridadRequestDto { Prioridad = OrdenPrioridad.Alto, Motivo = "Cliente urgente" };

        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Alto))
            .ReturnsAsync(true);
        _ordenServiceMock
            .Setup(s => s.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = ordenId, Prioridad = OrdenPrioridad.Alto });

        // ── ACT ──
        var result = await _service.CambiarPrioridadAsync(ordenId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Prioridad.Should().Be(OrdenPrioridad.Alto);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Alto), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CAMBIO_PRIORIDAD, empresaId, usuarioId,
            nameof(Orden), ordenId,
            It.Is<object>(d => d != null && ((string)d).Contains("prioridadAnterior")
                && ((string)d).Contains(OrdenPrioridad.Normal)
                && ((string)d).Contains(OrdenPrioridad.Alto)),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CambiarPrioridadAsync_CuandoPrioridadInvalida_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var dto = new PrioridadRequestDto { Prioridad = "URGENTE" };

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarPrioridadAsync(
            Guid.NewGuid(), dto, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Prioridad inválida*");
        _ordenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPrioridadAsync_CuandoMismaPrioridad_NoActualizaYRetornaActual()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var orden = new OrdenBuilder()
            .ConId(ordenId)
            .ConEmpresa(empresaId)
            .ConPrioridad(OrdenPrioridad.Normal)
            .Build();
        var dto = new PrioridadRequestDto { Prioridad = OrdenPrioridad.Normal };

        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(orden);
        _ordenServiceMock
            .Setup(s => s.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenResponseDto { Id = ordenId, Prioridad = OrdenPrioridad.Normal });

        // ── ACT ──
        var result = await _service.CambiarPrioridadAsync(ordenId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        // Idempotente: no toca la BD ni audita.
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPrioridadAsync_CuandoOrdenNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, It.IsAny<Guid>()))
            .ReturnsAsync((Orden?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarPrioridadAsync(
            ordenId, new PrioridadRequestDto { Prioridad = OrdenPrioridad.Alto },
            Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Orden no encontrada*");
    }

    [Fact]
    public async Task CambiarPrioridadAsync_CuandoUpdateFalla_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(ordenId, empresaId))
            .ReturnsAsync(new OrdenBuilder()
                .ConId(ordenId)
                .ConEmpresa(empresaId)
                .ConPrioridad(OrdenPrioridad.Normal)
                .Build());
        _ordenRepoMock.Setup(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Alto))
            .ReturnsAsync(false);

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarPrioridadAsync(
            ordenId, new PrioridadRequestDto { Prioridad = OrdenPrioridad.Alto },
            empresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*No se pudo actualizar la prioridad*");
    }

    // ─── ÓRDENES CRÍTICAS (HU-029 CA-04) ───────────────────────────

    [Fact]
    public async Task GetCriticasAsync_RetornaOrdenesCriticasMapeadas()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenes = new List<Orden>
        {
            new OrdenBuilder().ConEstado(OrdenEstado.InTransit).ConPrioridad(OrdenPrioridad.Critico).Build(),
            new OrdenBuilder().ConEstado(OrdenEstado.Assigned).ConPrioridad(OrdenPrioridad.Alto).Build()
        };
        _ordenRepoMock.Setup(r => r.GetCriticasAsync(empresaId)).ReturnsAsync(ordenes);

        // ── ACT ──
        var result = await _service.GetCriticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o =>
            o.Prioridad == OrdenPrioridad.Critico || o.Prioridad == OrdenPrioridad.Alto);
        _ordenRepoMock.Verify(r => r.GetCriticasAsync(empresaId), Times.Once);
    }

    // ─── ELEVACIÓN AUTOMÁTICA (HU-029 CA-08) ───────────────────────

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoSinReglasActivas_RetornaCero()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad>());

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(0);
        _ordenRepoMock.Verify(r => r.GetCriticasAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoReglaSINAVANCE_ElevaOrdenesEstancadas()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Estancadas 4h",
            Condicion = "SIN_AVANCE",
            NivelDestino = OrdenPrioridad.Alto,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });
        _ordenRepoMock.Setup(r => r.GetCriticasAsync(empresaId))
            .ReturnsAsync(new List<Orden>
            {
                new OrdenBuilder()
                    .ConId(ordenId)
                    .ConEmpresa(empresaId)
                    .ConEstado(OrdenEstado.InTransit)
                    .ConPrioridad(OrdenPrioridad.Normal)
                    .Build()
            });
        _ordenRepoMock
            .Setup(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Alto))
            .ReturnsAsync(true);

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(1);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Alto), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.AUTO_PRIORIDAD, empresaId, null,
            nameof(Orden), ordenId,
            It.Is<object>(d => d != null && ((string)d).Contains("prioridadAnterior")
                && ((string)d).Contains("condicion")
                && ((string)d).Contains("SIN_AVANCE")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoReglaCLIENTEVIP_ElevaSoloClientesVip()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var vipId = Guid.NewGuid();
        var ordenVipId = Guid.NewGuid();
        var ordenNoVipId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "VIP en riesgo",
            Condicion = "CLIENTE_VIP",
            NivelDestino = OrdenPrioridad.Alto,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });
        _clienteRepoMock.Setup(r => r.GetClientesSlaVipAsync(empresaId))
            .ReturnsAsync(new List<Cliente> { new() { Id = vipId, Nombre = "Cliente VIP" } });
        _ordenRepoMock.Setup(r => r.GetSlaEnRiesgoAsync(empresaId))
            .ReturnsAsync(new List<Orden>
            {
                new OrdenBuilder().ConId(ordenVipId).ConEmpresa(empresaId).ConCliente(vipId)
                    .ConEstado(OrdenEstado.InTransit).ConPrioridad(OrdenPrioridad.Normal).Build(),
                new OrdenBuilder().ConId(ordenNoVipId).ConEmpresa(empresaId)
                    .ConEstado(OrdenEstado.InTransit).ConPrioridad(OrdenPrioridad.Normal).Build()
            });
        _ordenRepoMock
            .Setup(r => r.UpdatePrioridadAsync(It.IsAny<Guid>(), empresaId, OrdenPrioridad.Alto))
            .ReturnsAsync(true);

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(1); // Solo el cliente VIP se eleva.
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(ordenVipId, empresaId, OrdenPrioridad.Alto), Times.Once);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(ordenNoVipId, empresaId, OrdenPrioridad.Alto), Times.Never);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoReglaENTREGAPROXIMA_ElevaEnRiesgo()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Entrega próxima",
            Condicion = "ENTREGA_PROXIMA",
            NivelDestino = OrdenPrioridad.Critico,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });
        _ordenRepoMock.Setup(r => r.GetSlaEnRiesgoAsync(empresaId))
            .ReturnsAsync(new List<Orden>
            {
                new OrdenBuilder().ConId(ordenId).ConEmpresa(empresaId)
                    .ConEstado(OrdenEstado.InTransit).ConPrioridad(OrdenPrioridad.Normal).Build()
            });
        _ordenRepoMock
            .Setup(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Critico))
            .ReturnsAsync(true);

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(1);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(ordenId, empresaId, OrdenPrioridad.Critico), Times.Once);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoOrdenYaEnNivelMayor_NoEleva()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Regla a ALTO",
            Condicion = "SIN_AVANCE",
            NivelDestino = OrdenPrioridad.Alto,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });
        _ordenRepoMock.Setup(r => r.GetCriticasAsync(empresaId))
            .ReturnsAsync(new List<Orden>
            {
                // Ya está en ALTO — RangoPrioridad(ALTO) >= RangoPrioridad(ALTO) → skip.
                new OrdenBuilder().ConId(ordenId).ConEmpresa(empresaId)
                    .ConEstado(OrdenEstado.InTransit).ConPrioridad(OrdenPrioridad.Alto).Build()
            });

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(0);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(
            It.IsAny<Guid>(), empresaId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoOrdenEnEstadoFinal_NoEleva()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Estancadas 4h",
            Condicion = "SIN_AVANCE",
            NivelDestino = OrdenPrioridad.Alto,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });
        _ordenRepoMock.Setup(r => r.GetCriticasAsync(empresaId))
            .ReturnsAsync(new List<Orden>
            {
                // DELIVERED es estado final (SlaCalculator.EsEstadoFinal) → no se toca.
                new OrdenBuilder().ConEmpresa(empresaId)
                    .ConEstado(OrdenEstado.Delivered).ConPrioridad(OrdenPrioridad.Normal).Build()
            });

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(0);
        _ordenRepoMock.Verify(r => r.UpdatePrioridadAsync(
            It.IsAny<Guid>(), empresaId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ElevarPrioridadesAutomaticasAsync_CuandoCondicionDesconocida_SaltaRegla()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var regla = new ReglaPrioridad
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Regla rara",
            Condicion = "DESCONOCIDA",
            NivelDestino = OrdenPrioridad.Alto,
            Activo = true
        };
        _reglaRepoMock.Setup(r => r.GetActivasByEmpresaAsync(empresaId))
            .ReturnsAsync(new List<ReglaPrioridad> { regla });

        // ── ACT ──
        var result = await _service.ElevarPrioridadesAutomaticasAsync(empresaId);

        // ── ASSERT ──
        result.Should().Be(0);
        _ordenRepoMock.Verify(r => r.GetCriticasAsync(It.IsAny<Guid>()), Times.Never);
        _ordenRepoMock.Verify(r => r.GetSlaEnRiesgoAsync(It.IsAny<Guid>()), Times.Never);
    }
}