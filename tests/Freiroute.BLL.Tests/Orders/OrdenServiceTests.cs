using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Tests.Builders;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Configuracion;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Freiroute.BLL.Tests.Orders;

public class OrdenServiceTests
{
    private readonly Mock<IOrdenRepository>         _ordenRepoMock;
    private readonly Mock<IShipmentRepository>      _shipmentRepoMock;
    private readonly Mock<IConfiguracionRepository> _configRepoMock;
    private readonly Mock<IAuditoriaRepository>     _auditoriaMock;
    private readonly Mock<IValidator<OrdenRequestDto>> _validatorMock;
    private readonly Mock<IValidator<CambiarEstadoOrdenRequestDto>> _estadoValidatorMock;
    private readonly Mock<IValidator<SplitOrdenRequestDto>> _splitValidatorMock;
    private readonly Mock<IValidator<ConsolidarOrdenesRequestDto>> _consolidarValidatorMock;
    private readonly Mock<ILogger<OrdenService>>    _loggerMock;
    private readonly OrdenService                   _service;

    public OrdenServiceTests()
    {
        _ordenRepoMock      = new Mock<IOrdenRepository>();
        _shipmentRepoMock   = new Mock<IShipmentRepository>();
        _configRepoMock     = new Mock<IConfiguracionRepository>();
        _auditoriaMock      = new Mock<IAuditoriaRepository>();
        _validatorMock      = new Mock<IValidator<OrdenRequestDto>>();
        _estadoValidatorMock = new Mock<IValidator<CambiarEstadoOrdenRequestDto>>();
        _splitValidatorMock = new Mock<IValidator<SplitOrdenRequestDto>>();
        _consolidarValidatorMock = new Mock<IValidator<ConsolidarOrdenesRequestDto>>();
        _loggerMock         = new Mock<ILogger<OrdenService>>();

        // Validator válido por defecto — sobrescribir en tests negativos
        _validatorMock.Setup(v => v.ValidateAsync(
            It.IsAny<OrdenRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _estadoValidatorMock.Setup(v => v.ValidateAsync(
            It.IsAny<CambiarEstadoOrdenRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _splitValidatorMock.Setup(v => v.ValidateAsync(
            It.IsAny<SplitOrdenRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _consolidarValidatorMock.Setup(v => v.ValidateAsync(
            It.IsAny<ConsolidarOrdenesRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _service = new OrdenService(_ordenRepoMock.Object, _auditoriaMock.Object, _configRepoMock.Object,
            _validatorMock.Object,
            _estadoValidatorMock.Object,
            _splitValidatorMock.Object,
            _consolidarValidatorMock.Object,
            _loggerMock.Object);
    }

    // ── Tests de CreateAsync — HU-021 ──────────────────────────────
    [Fact]
    public async Task CreateAsync_CuandoDtoValido_RetornaOrdenEnDraftConNumeroNull()
    {
        // Arrange
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new OrdenBuilder().BuildRequestDto();
        var nuevoId = Guid.NewGuid();

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>()))
            .ReturnsAsync(nuevoId);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId))
            .ReturnsAsync(new Orden
            {
                Id     = nuevoId,
                Estado = OrdenEstado.Draft,
                NumeroOrden = null          // CA-02: null en DRAFT
            });

        // Act
        var result = await _service.CreateAsync(dto, empresaId, usuarioId);

        // Assert
        result.Estado.Should().Be(OrdenEstado.Draft);
        result.NumeroOrden.Should().BeNull();   // CA-02 HU-021
        _ordenRepoMock.Verify(r => r.CreateAsync(It.Is<Orden>(o =>
            o.EmpresaId == empresaId &&
            o.Estado    == OrdenEstado.Draft &&
            o.OrigenCreacion == OrigenCreacion.Manual)),   // CA-08: empresa_id del JWT
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoDtoInvalido_LanzaValidationException()
    {
        // En FluentValidation 9.1+ ValidateAndThrowAsync NO lanza cuando el validador
        // es un mock (la lógica de excepción vive en AbstractValidator.RaiseValidationException).
        // Por eso se usa el validador REAL: un OrdenRequestDto vacío falla las reglas
        // obligatorias (ClienteId, OrigenId, DestinoId, Cantidad, PesoKg...).
        var validatorReal = new Freiroute.BLL.Validators.OrdenValidator();
        var service = new OrdenService(_ordenRepoMock.Object, _auditoriaMock.Object, _configRepoMock.Object,
            validatorReal,
            _estadoValidatorMock.Object,
            _splitValidatorMock.Object,
            _consolidarValidatorMock.Object,
            _loggerMock.Object);

        var act = async () =>
            await service.CreateAsync(new OrdenRequestDto(),
                Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_CuandoDtoValido_NoRegistraHistorialInicial()
    {
        // CA-04 HU-024: el historial_estados_orden se registra en las transiciones
        // (CambiarEstadoAsync), NO en la creación. La orden nace DRAFT y el primer
        // registro de historial llega con el primer cambio de estado.
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new OrdenBuilder().BuildRequestDto();
        var nuevoId = Guid.NewGuid();

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(nuevoId);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId)).ReturnsAsync(new Orden());

        await _service.CreateAsync(dto, empresaId, usuarioId);

        _ordenRepoMock.Verify(r => r.RegistrarHistorialEstadoAsync(It.IsAny<HistorialEstadoOrden>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CuandoTieneLineas_CreaLineasEnBulk()
    {
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new OrdenBuilder().BuildRequestDto(); // incluye 2 líneas
        var nuevoId = Guid.NewGuid();

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(nuevoId);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId)).ReturnsAsync(new Orden());

        await _service.CreateAsync(dto, empresaId, usuarioId);

        // Las líneas del DTO se persisten en bulk con la referencia a la orden creada
        _ordenRepoMock.Verify(r => r.CreateLineasBulkAsync(
            It.Is<IEnumerable<LineaOrden>>(l => l.Any(x => x.OrdenId == nuevoId))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoSinLineas_NoLlamaCreateLineas()
    {
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new OrdenBuilder().BuildRequestDto();
        dto.Lineas = new List<LineaOrdenRequestDto>();
        var nuevoId = Guid.NewGuid();

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(nuevoId);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId)).ReturnsAsync(new Orden());

        await _service.CreateAsync(dto, empresaId, usuarioId);

        _ordenRepoMock.Verify(r => r.CreateLineasBulkAsync(It.IsAny<IEnumerable<LineaOrden>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RegistraAuditoriaCreate()
    {
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new OrdenBuilder().BuildRequestDto();
        var nuevoId = Guid.NewGuid();

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(nuevoId);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(nuevoId, empresaId)).ReturnsAsync(new Orden());

        await _service.CreateAsync(dto, empresaId, usuarioId);

        _auditoriaMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa => 
            aa.Modulo == "ordenes" && aa.Accion == "CREATE")), Times.Once);
    }

    // ── Tests de UpdateAsync — HU-021 ──────────────────────────────
    [Fact]
    public async Task UpdateAsync_CuandoEstadoDraft_ActualizaCorrectamente()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(new Orden { });
        
        var dto = new OrdenBuilder().BuildRequestDto();
        await _service.UpdateAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());
        _ordenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Orden>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoEstadoConfirmed_ActualizaCorrectamente()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(new Orden { });
        
        var dto = new OrdenBuilder().BuildRequestDto();
        await _service.UpdateAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());
        _ordenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Orden>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoEstadoInTransit_LanzaBusinessException()
    {
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenBuilder()
                .ConEstado(OrdenEstado.InTransit).Build());

        var act = async () =>
            await _service.UpdateAsync(Guid.NewGuid(), new OrdenRequestDto(),
                Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Solo se pueden editar órdenes en estado DRAFT o CONFIRMED*");
    }

    [Theory]
    [InlineData("ASSIGNED")]
    [InlineData("PICKUP_SCHEDULED")]
    [InlineData("DELIVERED")]
    [InlineData("INVOICED")]
    [InlineData("CLOSED")]
    [InlineData("CANCELLED")]
    public async Task UpdateAsync_CuandoEstadoNoEditable_LanzaBusinessException(string estado)
    {
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenBuilder().ConEstado(estado).Build());

        var act = async () =>
            await _service.UpdateAsync(Guid.NewGuid(), new OrdenRequestDto(),
                Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task UpdateAsync_CuandoOrdenNoExiste_LanzaNotFoundException()
    {
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Orden?)null);
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), new OrdenRequestDto(), Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Tests de DeactivateAsync — HU-021 ──────────────────────────────
    [Fact]
    public async Task DeactivateAsync_CuandoEstadoDraft_RetornaTrue()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
        
        var result = await _service.DeactivateAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("CONFIRMED")]
    [InlineData("ASSIGNED")]
    [InlineData("IN_TRANSIT")]
    [InlineData("DELIVERED")]
    [InlineData("CANCELLED")]
    public async Task DeactivateAsync_CuandoEstadoNoDesactivable_LanzaBusinessException(string estado)
    {
        var orden = new OrdenBuilder().ConEstado(estado).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        
        var act = async () => await _service.DeactivateAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoOrdenNoExiste_LanzaNotFoundException()
    {
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Orden?)null);
        var act = async () => await _service.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Tests de CambiarEstadoAsync — HU-024 ──────────────────────────────
    [Fact]
    public async Task CambiarEstadoAsync_DraftAConfirmed_GeneraNumeroOrden()
    {
        // Arrange
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Orden());
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Freiroute.Entity.Empresa { Id = Guid.NewGuid() });
        _ordenRepoMock.Setup(r => r.GenerarNumeroOrdenAsync(
            It.IsAny<Guid>(), "ORD", DateTime.UtcNow.Year))
            .ReturnsAsync("ORD-2026-00001");

        // Act
        await _service.CambiarEstadoAsync(
            orden.Id,
            new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Confirmed },
            orden.EmpresaId, Guid.NewGuid());

        // Assert
        _ordenRepoMock.Verify(r => r.GenerarNumeroOrdenAsync(
            It.IsAny<Guid>(), "ORD", DateTime.UtcNow.Year),
            Times.Once);
        _ordenRepoMock.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Guid>(), OrdenEstado.Confirmed, It.IsAny<Guid>()),
            Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_TransicionNoConfirmed_NoGeneraNumero()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);

        await _service.CambiarEstadoAsync(
            orden.Id,
            new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Assigned },
            orden.EmpresaId, Guid.NewGuid());

        _ordenRepoMock.Verify(r => r.GenerarNumeroOrdenAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);  // CA crítico: no generar número en otras transiciones
    }

    [Fact]
    public async Task CambiarEstadoAsync_TransicionInvalida_LanzaBusinessException()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(orden);

        var act = async () => await _service.CambiarEstadoAsync(
            orden.Id,
            new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Delivered },
            orden.EmpresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Borrador*Entregada*");  // labels en español

        // Verificar que ActualizarEstadoAsync NO fue llamado
        _ordenRepoMock.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);

        // La validación de roles vive en la capa de Controller/API (RequirePermission),
        // no en el servicio — ver tests de integración para 403 por permisos.
        _ordenRepoMock.Verify(r => r.RegistrarHistorialEstadoAsync(It.IsAny<HistorialEstadoOrden>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_ConductorIniciaTransito_EsPermitido()
    {
        var orden = new OrdenBuilder()
            .ConEstado(OrdenEstado.PickupScheduled).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);

        var act = async () => await _service.CambiarEstadoAsync(
            orden.Id,
            new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.InTransit },
            orden.EmpresaId, Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CambiarEstadoAsync_EstadoTerminalClosed_LanzaBusinessException()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Closed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        var act = async () => await _service.CambiarEstadoAsync(orden.Id, new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Cancelled }, orden.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task CambiarEstadoAsync_EstadoTerminalCancelled_LanzaBusinessException()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Cancelled).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        var act = async () => await _service.CambiarEstadoAsync(orden.Id, new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Draft }, orden.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task CambiarEstadoAsync_RegistraHistorialConMotivo()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);

        var dto = new CambiarEstadoOrdenRequestDto
        {
            EstadoNuevo = OrdenEstado.OnHold,
            Motivo      = "Espera de documentos"
        };

        await _service.CambiarEstadoAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());

        // CA HU-024: registrar historial con motivo y transición exacta
        _ordenRepoMock.Verify(r => r.RegistrarHistorialEstadoAsync(
            It.Is<HistorialEstadoOrden>(h =>
                h.OrdenId == orden.Id &&
                h.EstadoAnterior == OrdenEstado.Confirmed &&
                h.EstadoNuevo == OrdenEstado.OnHold &&
                h.Motivo == "Espera de documentos")), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_RegistraAuditoriaCambioEstado()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(orden);
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(It.IsAny<Guid>())).ReturnsAsync(new Freiroute.Entity.Empresa { Id = Guid.NewGuid() });
        _ordenRepoMock.Setup(r => r.GenerarNumeroOrdenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync("ORD-2026-00001");

        await _service.CambiarEstadoAsync(orden.Id, new CambiarEstadoOrdenRequestDto { EstadoNuevo = OrdenEstado.Confirmed }, orden.EmpresaId, Guid.NewGuid());

        _auditoriaMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa => 
            aa.Modulo == "ordenes" && aa.Accion == "CAMBIO_ESTADO")));
    }

    [Fact]
    public async Task GetByIdAsync_RetornaTransicionesDisponibles()
    {
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build());
        _ordenRepoMock.Setup(r => r.GetLineasAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<LineaOrden>());

        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        result!.TransicionesDisponibles.Should().BeEquivalentTo(new[]
        {
            OrdenEstado.Assigned,
            OrdenEstado.OnHold,
            OrdenEstado.PartiallySplit,
            OrdenEstado.Cancelled
        });
    }

    // ── Tests de ConsolidarAsync — HU-025 ──────────────────────────────
    [Fact]
    public async Task ConsolidarAsync_CuandoOrdenesConfirmed_CreaShipmentYActualiza()
    {
        var orden1 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id } };
        var result = await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());

        // HU-025: asignar shipment y pasar a ASSIGNED vía AsignarShipmentAsync
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden1.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden2.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        // Resultado resumen con estado PLANNED
        result.Estado.Should().Be("PLANNED");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ConsolidarAsync_CuandoOrdenEnDraft_LanzaBusinessException()
    {
        var orden1 = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id } };
        var act = async () => await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Theory]
    [InlineData("ASSIGNED")]
    [InlineData("IN_TRANSIT")]
    [InlineData("DELIVERED")]
    [InlineData("CANCELLED")]
    public async Task ConsolidarAsync_CuandoOrdenEnEstadoNoPermitido_LanzaBusinessException(string estado)
    {
        var orden1 = new OrdenBuilder().ConEstado(estado).Build();
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id } };
        var act = async () => await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task ConsolidarAsync_CuandoShipmentIdExiste_UsaShipmentExistente()
    {
        var orden1 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var shipmentId = Guid.NewGuid();
        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id }, ShipmentId = shipmentId };
        await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());

        // Multi-tenant: TODAS las órdenes se asignan con el tenant del llamador
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden1.Id, shipmentId, OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden2.Id, shipmentId, OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
    }

    [Fact]
    public async Task ConsolidarAsync_ModosDiferentesEnOrdenes_HaceConsolidacion()
    {
        var orden1 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        orden1.ModoTransporte = "TERRESTRE";
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        orden2.ModoTransporte = "AEREO";
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id } };
        var result = await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());

        // La consolidación se ejecuta aunque los modos difieran.
        // Gap conocido Sprint 4: el cálculo de advertencias por modo de transporte
        // es parte del roadmap de shipments (Sprint 7+), no está en esta implementación.
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden1.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden2.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        result.Estado.Should().Be("PLANNED");
    }

    [Fact]
    public async Task ConsolidarAsync_MismosModosEnOrdenes_ConsolidaCorrectamente()
    {
        var orden1 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        orden1.ModoTransporte = "TERRESTRE";
        var orden2 = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        orden2.ModoTransporte = "TERRESTRE";
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden1.Id, It.IsAny<Guid>())).ReturnsAsync(orden1);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden2.Id, It.IsAny<Guid>())).ReturnsAsync(orden2);

        var dto = new ConsolidarOrdenesRequestDto { OrdenIds = new List<Guid> { orden1.Id, orden2.Id } };
        var result = await _service.ConsolidarAsync(dto, orden1.EmpresaId, Guid.NewGuid());

        result.Estado.Should().Be("PLANNED");
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden1.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden2.Id, It.IsAny<Guid>(), OrdenEstado.Assigned, orden1.EmpresaId), Times.Once);
    }

    // ── Tests de SplitAsync — HU-026 ──────────────────────────────
    [Fact]
    public async Task SplitAsync_CuandoSumaCorrecta_CreaSubOrdenesEnConfirmed()
    {
        var orden = new OrdenBuilder()
            .ConEstado(OrdenEstado.Confirmed)
            .ConCantidad(10).ConPeso(500).Build();

        // Genérico PRIMERO: sub-órdenes creadas por el split nacen en DRAFT
        // (CambiarEstadoAsync interno hace DRAFT→CONFIRMED).
        var ordenDraft = new Orden { EmpresaId = orden.EmpresaId, Estado = OrdenEstado.Draft };
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(ordenDraft);
        // Específico DESPUÉS: orden original CONFIRMED (Moq: gana el último setup que matchea)
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);

        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(Guid.NewGuid());
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(orden.EmpresaId)).ReturnsAsync(new Freiroute.Entity.Empresa { PrefijoOrden = "ORD" });
        _ordenRepoMock.Setup(r => r.GenerarNumeroOrdenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("ORD-2026-00001");

        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 7, PesoKg = 350 },
                new SplitItemDto { Cantidad = 3, PesoKg = 150 }
            }
        };

        await _service.SplitAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());

        // HU-026: dos sub-órdenes creadas (nacen en DRAFT, luego se confirman)
        _ordenRepoMock.Verify(r => r.CreateAsync(It.IsAny<Orden>()), Times.Exactly(2));
        // La orden original pasa a PARTIALLY_SPLIT
        _ordenRepoMock.Verify(r => r.ActualizarEstadoAsync(orden.Id, OrdenEstado.PartiallySplit, orden.EmpresaId), Times.Once);
    }

    [Theory]
    [InlineData("IN_TRANSIT")]
    [InlineData("DELIVERED")]
    [InlineData("CANCELLED")]
    [InlineData("PICKUP_SCHEDULED")]
    public async Task SplitAsync_CuandoEstadoNoPermitido_LanzaBusinessException(string estado)
    {
        var orden = new OrdenBuilder().ConEstado(estado).ConCantidad(10).ConPeso(500).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);

        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 7, PesoKg = 350 },
                new SplitItemDto { Cantidad = 3, PesoKg = 150 }
            }
        };

        var act = async () => await _service.SplitAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task SplitAsync_CuandoSumaCantidadIncorrecta_LanzaBusinessException()
    {
        var orden = new OrdenBuilder()
            .ConEstado(OrdenEstado.Confirmed)
            .ConCantidad(10).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);

        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 6, PesoKg = 300 },   // suma = 9, no 10
                new SplitItemDto { Cantidad = 3, PesoKg = 200 }
            }
        };

        var act = async () => await _service.SplitAsync(
            orden.Id, dto, orden.EmpresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*La suma de los splits debe ser igual a la cantidad y peso original*");
    }

    [Fact]
    public async Task SplitAsync_CuandoDiferenciaEnTolerancia_EsValido()
    {
        var orden = new OrdenBuilder()
            .ConEstado(OrdenEstado.Confirmed)
            .ConCantidad(10m).ConPeso(500m).Build();
        var ordenDraft = new Orden { EmpresaId = orden.EmpresaId, Estado = OrdenEstado.Draft };
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(ordenDraft);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(Guid.NewGuid());
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(orden.EmpresaId)).ReturnsAsync(new Freiroute.Entity.Empresa { PrefijoOrden = "ORD" });
        _ordenRepoMock.Setup(r => r.GenerarNumeroOrdenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("ORD-2026-00001");

        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 5.0005m, PesoKg = 250.0003m },
                new SplitItemDto { Cantidad = 4.9995m, PesoKg = 249.9997m }
                // suma: 10.0000 y 500.0000 → dentro de tolerancia
            }
        };

        var act = async () => await _service.SplitAsync(
            orden.Id, dto, orden.EmpresaId, Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SplitAsync_OrdenOriginalPasaAPartiallySplit()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).ConCantidad(10).ConPeso(500).Build();
        var ordenDraft = new Orden { EmpresaId = orden.EmpresaId, Estado = OrdenEstado.Draft };
        _ordenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(ordenDraft);
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.CreateAsync(It.IsAny<Orden>())).ReturnsAsync(Guid.NewGuid());
        _configRepoMock.Setup(r => r.GetConfiguracionAsync(orden.EmpresaId)).ReturnsAsync(new Freiroute.Entity.Empresa { PrefijoOrden = "ORD" });
        _ordenRepoMock.Setup(r => r.GenerarNumeroOrdenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("ORD-2026-00001");

        var dto = new SplitOrdenRequestDto
        {
            Splits = new List<SplitItemDto>
            {
                new SplitItemDto { Cantidad = 5, PesoKg = 250 },
                new SplitItemDto { Cantidad = 5, PesoKg = 250 }
            }
        };

        await _service.SplitAsync(orden.Id, dto, orden.EmpresaId, Guid.NewGuid());

        // CA HU-026: la orden original queda en PARTIALLY_SPLIT
        _ordenRepoMock.Verify(r => r.ActualizarEstadoAsync(orden.Id, OrdenEstado.PartiallySplit, orden.EmpresaId), Times.Once);
    }

    [Fact]
    public async Task SplitAsync_CuandoValidatorInvalido_LanzaValidationException()
    {
        // En FluentValidation 9.1+ ValidateAndThrowAsync NO lanza cuando el validador
        // es un mock (la lógica vive en AbstractValidator). Por eso este test usa el
        // validador REAL: SplitOrdenValidator rechaza splits con cantidades <= 0.
        var validatorReal = new Freiroute.BLL.Validators.SplitOrdenValidator();
        var service = new OrdenService(_ordenRepoMock.Object, _auditoriaMock.Object, _configRepoMock.Object,
            _validatorMock.Object, _estadoValidatorMock.Object, validatorReal, _consolidarValidatorMock.Object, _loggerMock.Object);

        var act = async () => await service.SplitAsync(
            Guid.NewGuid(),
            new SplitOrdenRequestDto
            {
                Splits = new List<SplitItemDto> { new SplitItemDto { Cantidad = 0, PesoKg = 0 } }
            },
            Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── Tests adicionales de cobertura — HU-021 a HU-026 ────────────

    [Fact]
    public async Task GetAllAsync_RetornaPaginado()
    {
        var empresaId = Guid.NewGuid();
        var orden = new Orden { Id = Guid.NewGuid(), EmpresaId = empresaId, Estado = OrdenEstado.Confirmed, NumeroOrden = "ORD-2026-1" };
        _ordenRepoMock.Setup(r => r.GetAllAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(),
                It.IsAny<bool?>()))
            .ReturnsAsync(new PagedResult<Orden>
            {
                Items = new List<Orden> { orden },
                TotalItems = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _service.GetAllAsync(empresaId, new OrdenFiltroDto { Page = 1, PageSize = 20 });

        result.TotalItems.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.Id == orden.Id);
    }

    [Fact]
    public async Task GetHistorialAsync_MapeaHistorial()
    {
        var ordenId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetHistorialAsync(ordenId, empresaId))
            .ReturnsAsync(new List<HistorialEstadoOrden>
            {
                new HistorialEstadoOrden { EstadoAnterior = OrdenEstado.Confirmed, EstadoNuevo = OrdenEstado.Cancelled, Motivo = "Cliente canceló" }
            });

        var result = (await _service.GetHistorialAsync(ordenId, empresaId)).ToList();

        result.Should().ContainSingle();
        result[0].EstadoAnterior.Should().Be(OrdenEstado.Confirmed);
        result[0].EstadoNuevo.Should().Be(OrdenEstado.Cancelled);
        result[0].Motivo.Should().Be("Cliente canceló");
    }

    [Fact]
    public async Task DesconsolidarAsync_CuandoAssigned_LimpiaShipmentYVuelveAConfirmed()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Assigned).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.GetLineasAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(new List<LineaOrden>());

        var result = await _service.DesconsolidarAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());

        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(orden.Id, null, OrdenEstado.Confirmed, orden.EmpresaId), Times.Once);
        _ordenRepoMock.Verify(r => r.RegistrarHistorialEstadoAsync(It.Is<HistorialEstadoOrden>(h =>
            h.EstadoNuevo == OrdenEstado.Confirmed && h.Motivo == "Desconsolidada")), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DesconsolidarAsync_CuandoNoAssigned_LanzaBusinessException()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);

        var act = async () => await _service.DesconsolidarAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*ASSIGNED*");
        _ordenRepoMock.Verify(r => r.AsignarShipmentAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByShipmentIdAsync_RetornaOrdenesDelShipment()
    {
        var shipmentId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _ordenRepoMock.Setup(r => r.GetByShipmentIdAsync(shipmentId, empresaId))
            .ReturnsAsync(new List<Orden>
            {
                new Orden { Id = Guid.NewGuid(), Estado = OrdenEstado.Assigned, ShipmentId = shipmentId }
            });

        var result = (await _service.GetByShipmentIdAsync(shipmentId, empresaId)).ToList();

        result.Should().ContainSingle(x => x.Estado == OrdenEstado.Assigned);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoDraft_DesactivaYRegistraAuditoria()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Draft).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);
        _ordenRepoMock.Setup(r => r.DeactivateAsync(orden.Id, orden.EmpresaId)).ReturnsAsync(true);

        var result = await _service.DeactivateAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());

        result.Should().BeTrue();
        _auditoriaMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa =>
            aa.Accion == "DELETE" && aa.Modulo == "ordenes")), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoDraft_LanzaBusinessException()
    {
        var orden = new OrdenBuilder().ConEstado(OrdenEstado.Confirmed).Build();
        _ordenRepoMock.Setup(r => r.GetByIdAsync(orden.Id, It.IsAny<Guid>())).ReturnsAsync(orden);

        var act = async () => await _service.DeactivateAsync(orden.Id, orden.EmpresaId, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*DRAFT*");
        _ordenRepoMock.Verify(r => r.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}














