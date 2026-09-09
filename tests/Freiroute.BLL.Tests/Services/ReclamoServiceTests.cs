using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Tests.Builders;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Reclamo;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de ReclamoService (HU-032 — Claims Management).
/// </summary>
public class ReclamoServiceTests
{
    private readonly Mock<IReclamoRepository> _reclamoRepoMock;
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IConfiguracionRepository> _configRepoMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly ReclamoService _service;

    public ReclamoServiceTests()
    {
        _reclamoRepoMock = new Mock<IReclamoRepository>();
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _configRepoMock = new Mock<IConfiguracionRepository>();
        _auditoriaMock = new Mock<IAuditoriaService>();
        _service = new ReclamoService(
            _reclamoRepoMock.Object, _ordenRepoMock.Object,
            _configRepoMock.Object, _auditoriaMock.Object);
    }

    // ─── CREATE (HU-032 CA-01/CA-02/CA-03/CA-11) ───────────────────

    [Fact]
    public async Task CreateAsync_CuandoOrdenExiste_CreaReclamoAbiertoConHistorialYNumero()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var reclamoId = Guid.NewGuid();
        var dto = new ReclamoBuilder().BuildRequestDto();
        var numero = "REC-ORD-2026-0001";

        _ordenRepoMock.Setup(r => r.GetByIdAsync(dto.OrdenId, empresaId))
            .ReturnsAsync(new Orden { Id = dto.OrdenId, EmpresaId = empresaId });
        _reclamoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reclamo>())).ReturnsAsync(reclamoId);
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(empresaId))
            .ReturnsAsync(new Empresa { PrefijoOrden = "ORD" });
        _reclamoRepoMock.Setup(r => r.GenerarNumeroReclamoAsync(empresaId, "ORD", It.IsAny<int>()))
            .ReturnsAsync(numero);
        _reclamoRepoMock.Setup(r => r.GetByIdAsync(reclamoId, empresaId))
            .ReturnsAsync(new ReclamoBuilder()
                .ConId(reclamoId)
                .ConEmpresa(empresaId)
                .ConOrden(dto.OrdenId)
                .ConTipo(dto.Tipo)
                .ConNumero(numero)
                .ConDescripcion(dto.Descripcion)
                .ConMonto(dto.MontoReclamado)
                .ConEstado(EstadoReclamo.Abierto)
                .Build());
        _reclamoRepoMock.Setup(r => r.GetHistorialAsync(reclamoId, empresaId))
            .ReturnsAsync(new List<HistorialEstadoReclamo>());

        // ── ACT ──
        var result = await _service.CreateAsync(dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Estado.Should().Be(EstadoReclamo.Abierto); // CA-01: nace ABIERTO.
        result.NumeroReclamo.Should().Be(numero);
        result.Tipo.Should().Be(dto.Tipo);
        // CA-02: valida la orden en el mismo tenant antes de crear.
        _ordenRepoMock.Verify(r => r.GetByIdAsync(dto.OrdenId, empresaId), Times.Once);
        // Historial inicial: null → ABIERTO (espejo G-17A de órdenes).
        _reclamoRepoMock.Verify(r => r.InsertHistorialAsync(It.Is<HistorialEstadoReclamo>(h =>
            h.ReclamoId == reclamoId &&
            h.EstadoAnterior == null &&
            h.EstadoNuevo == EstadoReclamo.Abierto &&
            h.Motivo == "Creación de reclamo" &&
            h.UsuarioId == usuarioId)), Times.Once);
        // CA-03/CA-11: número legible REC-{PREFIJO}-{AÑO}-{NNNN}.
        _reclamoRepoMock.Verify(r => r.GenerarNumeroReclamoAsync(
            empresaId, "ORD", DateTime.UtcNow.Year), Times.Once);
        _reclamoRepoMock.Verify(r => r.UpdateNumeroReclamoAsync(reclamoId, empresaId, numero), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CREAR_RECLAMO, empresaId, usuarioId,
            nameof(Reclamo), reclamoId,
            It.Is<object>(d => d != null && ((string)d).Contains("numeroReclamo")),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoOrdenNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var dto = new ReclamoBuilder().BuildRequestDto();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(dto.OrdenId, It.IsAny<Guid>()))
            .ReturnsAsync((Orden?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.CreateAsync(dto, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*La orden vinculada no existe*");
        _reclamoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Reclamo>()), Times.Never);
    }

    // ─── GET BY ID CON HISTORIAL (HU-032 CA-07) ────────────────────

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaReclamoConHistorial()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var reclamoId = Guid.NewGuid();
        var reclamo = new ReclamoBuilder()
            .ConId(reclamoId)
            .ConEmpresa(empresaId)
            .ConEstado(EstadoReclamo.EnRevision)
            .Build();
        var historial = new List<HistorialEstadoReclamo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ReclamoId = reclamoId,
                EmpresaId = empresaId,
                EstadoNuevo = EstadoReclamo.EnRevision,
                EstadoAnterior = EstadoReclamo.Abierto,
                Motivo = "Revisión inicial",
                UsuarioNombre = "Juan Perez",
                FechaCreacion = DateTime.UtcNow
            }
        };
        _reclamoRepoMock.Setup(r => r.GetByIdAsync(reclamoId, empresaId)).ReturnsAsync(reclamo);
        _reclamoRepoMock.Setup(r => r.GetHistorialAsync(reclamoId, empresaId)).ReturnsAsync(historial);

        // ── ACT ──
        var result = await _service.GetByIdAsync(reclamoId, empresaId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Historial.Should().HaveCount(1);
        result.Historial[0].UsuarioNombre.Should().Be("Juan Perez"); // JOIN con usuarios.
        result.Historial[0].EstadoAnterior.Should().Be(EstadoReclamo.Abierto);
        result.Historial[0].EstadoNuevo.Should().Be(EstadoReclamo.EnRevision);
        result.OrdenNumero.Should().NotBeNullOrEmpty(); // JOIN con ordenes.
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        // ── ARRANGE ──
        _reclamoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Reclamo?)null);

        // ── ACT ──
        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        // ── ASSERT ──
        result.Should().BeNull();
    }

    // ─── LISTADO PAGINADO (HU-032 CA-06) ───────────────────────────

    [Fact]
    public async Task GetAllAsync_RetornaItemsYTotal()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var filtro = new ReclamoFiltroDto { Page = 1, PageSize = 20 };
        var items = new List<Reclamo>
        {
            new ReclamoBuilder().ConEmpresa(empresaId).ConEstado(EstadoReclamo.Abierto).Build(),
            new ReclamoBuilder().ConEmpresa(empresaId).ConEstado(EstadoReclamo.EnRevision).Build()
        };
        _reclamoRepoMock.Setup(r => r.GetAllAsync(empresaId, filtro))
            .ReturnsAsync((items, 3));

        // ── ACT ──
        var (resultItems, total) = await _service.GetAllAsync(empresaId, filtro);

        // ── ASSERT ──
        total.Should().Be(3);
        resultItems.Should().HaveCount(2);
        resultItems.First().OrdenNumero.Should().NotBeNullOrEmpty(); // JOIN con ordenes.
        resultItems.First().ClienteNombre.Should().NotBeNullOrEmpty(); // JOIN con clientes.
    }

    // ─── CAMBIO DE ESTADO (HU-032 CA-04/CA-12) ─────────────────────

    [Fact]
    public async Task CambiarEstadoAsync_CuandoTransicionValida_ActualizaRegistraHistorialYAudita()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var reclamoId = Guid.NewGuid();
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = EstadoReclamo.EnRevision,
            Motivo = "Iniciamos revisión con el transportista"
        };
        var abierto = new ReclamoBuilder()
            .ConId(reclamoId)
            .ConEmpresa(empresaId)
            .ConEstado(EstadoReclamo.Abierto)
            .Build();
        var enRevision = new ReclamoBuilder()
            .ConId(reclamoId)
            .ConEmpresa(empresaId)
            .ConEstado(EstadoReclamo.EnRevision)
            .Build();

        _reclamoRepoMock.SetupSequence(r => r.GetByIdAsync(reclamoId, empresaId))
            .ReturnsAsync(abierto)      // 1ª llamada: validación FSM.
            .ReturnsAsync(enRevision);  // 2ª llamada: respuesta final.
        _reclamoRepoMock.Setup(r => r.GetHistorialAsync(reclamoId, empresaId))
            .ReturnsAsync(new List<HistorialEstadoReclamo>());

        // ── ACT ──
        var result = await _service.CambiarEstadoAsync(reclamoId, dto, empresaId, usuarioId);

        // ── ASSERT ──
        result.Should().NotBeNull();
        result!.Estado.Should().Be(EstadoReclamo.EnRevision);
        _reclamoRepoMock.Verify(r => r.UpdateEstadoAsync(reclamoId, empresaId,
            EstadoReclamo.EnRevision, usuarioId, It.IsAny<DateTime>()), Times.Once);
        // CA-04: historial in-mutable con motivo obligatorio.
        _reclamoRepoMock.Verify(r => r.InsertHistorialAsync(It.Is<HistorialEstadoReclamo>(h =>
            h.ReclamoId == reclamoId &&
            h.EstadoAnterior == EstadoReclamo.Abierto &&
            h.EstadoNuevo == EstadoReclamo.EnRevision &&
            h.Motivo == "Iniciamos revisión con el transportista" &&
            h.UsuarioId == usuarioId)), Times.Once);
        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloPermiso.Ordenes, AccionAuditoria.CAMBIO_ESTADO_RECLAMO, empresaId, usuarioId,
            nameof(Reclamo), reclamoId,
            It.Is<object>(d => d != null && ((string)d).Contains(EstadoReclamo.Abierto)
                && ((string)d).Contains(EstadoReclamo.EnRevision)),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_CuandoMotivoVacio_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var dto = new ReclamoEstadoRequestDto { EstadoNuevo = EstadoReclamo.EnRevision, Motivo = " " };

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarEstadoAsync(
            Guid.NewGuid(), dto, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*motivo de la transición es obligatorio*");
        _reclamoRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_CuandoTransicionInvalida_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var reclamoId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var dto = new ReclamoEstadoRequestDto
        {
            EstadoNuevo = EstadoReclamo.Cerrado, // ABIERTO solo admite → EN_REVISION.
            Motivo = "Cierre directo"
        };
        _reclamoRepoMock.Setup(r => r.GetByIdAsync(reclamoId, empresaId))
            .ReturnsAsync(new ReclamoBuilder()
                .ConId(reclamoId)
                .ConEmpresa(empresaId)
                .ConEstado(EstadoReclamo.Abierto)
                .Build());

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarEstadoAsync(reclamoId, dto, empresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Transición no permitida*");
        _reclamoRepoMock.Verify(r => r.UpdateEstadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_CuandoReclamoNoExiste_LanzaBusinessException()
    {
        // ── ARRANGE ──
        var reclamoId = Guid.NewGuid();
        _reclamoRepoMock.Setup(r => r.GetByIdAsync(reclamoId, It.IsAny<Guid>()))
            .ReturnsAsync((Reclamo?)null);

        // ── ACT & ASSERT ──
        var act = async () => await _service.CambiarEstadoAsync(reclamoId,
            new ReclamoEstadoRequestDto { EstadoNuevo = EstadoReclamo.EnRevision, Motivo = "Revisión" },
            Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Reclamo no encontrado*");
    }

    // ─── RECLAMOS POR CLIENTE (HU-032 CA-10) ───────────────────────

    [Fact]
    public async Task GetByClienteAsync_RetornaReclamosDelCliente()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var items = new List<Reclamo>
        {
            new ReclamoBuilder().ConEmpresa(empresaId).Build(),
            new ReclamoBuilder().ConEmpresa(empresaId).Build()
        };
        _reclamoRepoMock.Setup(r => r.GetByClienteAsync(clienteId, empresaId)).ReturnsAsync(items);

        // ── ACT ──
        var result = await _service.GetByClienteAsync(clienteId, empresaId);

        // ── ASSERT ──
        result.Should().HaveCount(2);
        result.First().ClienteNombre.Should().NotBeNullOrEmpty(); // JOIN con clientes.
        _reclamoRepoMock.Verify(r => r.GetByClienteAsync(clienteId, empresaId), Times.Once);
    }

    // ─── REPORTE (HU-032 CA-09) ────────────────────────────────────

    [Fact]
    public async Task GetReporteAsync_RetornaReporteDelRepositorio()
    {
        // ── ARRANGE ──
        var empresaId = Guid.NewGuid();
        var desde = DateTime.UtcNow.AddDays(-30);
        var hasta = DateTime.UtcNow;
        var reporte = new List<ReclamoReporteItemDto>
        {
            new() { Tipo = TipoReclamo.Dano, Estado = EstadoReclamo.Abierto, Total = 3, MontoTotal = 4500.00m }
        };
        _reclamoRepoMock.Setup(r => r.GetReporteAsync(empresaId, desde, hasta)).ReturnsAsync(reporte);

        // ── ACT ──
        var result = await _service.GetReporteAsync(empresaId, desde, hasta);

        // ── ASSERT ──
        result.Should().HaveCount(1);
        result.First().Total.Should().Be(3);
        result.First().Tipo.Should().Be(TipoReclamo.Dano);
    }
}