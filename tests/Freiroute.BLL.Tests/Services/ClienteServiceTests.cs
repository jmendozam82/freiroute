using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Validators;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Cliente;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests unitarios del catálogo de clientes (HU-019, Sprint 3).
/// Cubre CRUD + contactos, estado de crédito (CA-05), RUC único (CA-08),
/// importación/exportación CSV fail-soft (CA-08/CA-09, ADR-017).
/// </summary>
public class ClienteServiceTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid ContactoId = Guid.NewGuid();
    private static readonly Guid UbicacionId = Guid.NewGuid();

    private readonly Mock<IClienteRepository> _repo;
    private readonly Mock<IUbicacionRepository> _ubicacionRepo;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _repo = new Mock<IClienteRepository>();
        _ubicacionRepo = new Mock<IUbicacionRepository>();
        _auditoria = new Mock<IAuditoriaService>();
        ConfigurarAuditoria();
        _service = new ClienteService(
            _repo.Object,
            _ubicacionRepo.Object,
            new ClienteValidator(),
            _auditoria.Object,
            Mock.Of<ILogger<ClienteService>>());
    }

    private void ConfigurarAuditoria()
    {
        _auditoria
            .Setup(a => a.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private static ClienteRequestDto DtoValido() => new()
    {
        Nombre = "Transportes ABC",
        RucNit = "J123456789",
        TipoDocumento = "RUC",
        TipoCliente = TipoCliente.Regular,
        Pais = "Nicaragua",
        Ciudad = "Managua",
        Moneda = "USD",
        CreditoDias = 30,
        LimiteCredito = 10000,
        Contactos =
        [
            new ContactoClienteRequestDto
            {
                Nombre = "Juan Pérez",
                Rol = RolContacto.Logistica,
                Email = "juan@abc.com",
                EsPrincipal = true
            }
        ]
    };

    private static Cliente ClienteEntity(Guid id) => new()
    {
        Id = id,
        EmpresaId = EmpresaId,
        Nombre = "Transportes ABC",
        RucNit = "J123456789",
        TipoCliente = TipoCliente.Regular,
        Pais = "Nicaragua",
        Ciudad = "Managua",
        Moneda = "USD",
        EstadoCredito = EstadoCredito.AlDia,
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    private static ContactoCliente ContactoEntity(Guid id) => new()
    {
        Id = id,
        EmpresaId = EmpresaId,
        ClienteId = ClienteId,
        Nombre = "Juan Pérez",
        Rol = RolContacto.Logistica,
        EsPrincipal = true,
        Activo = true
    };

    [Fact]
    public async Task GetAllAsync_CuandoExistenClientes_RetornaPaginadoConTotal()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<Cliente> { ClienteEntity(ClienteId), ClienteEntity(Guid.NewGuid()), ClienteEntity(Guid.NewGuid()) });
        _repo.Setup(r => r.GetContactosAsync(It.IsAny<Guid>(), EmpresaId)).ReturnsAsync(new List<ContactoCliente>());

        var result = await _service.GetAllAsync(EmpresaId, null, null, null, page: 2, pageSize: 1);

        result.TotalItems.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaClienteConUbicacionDefecto()
    {
        var cliente = ClienteEntity(ClienteId);
        cliente.UbicacionDefectoId = UbicacionId;
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(cliente);
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId)).ReturnsAsync(new List<ContactoCliente>());
        _ubicacionRepo.Setup(u => u.GetByIdAsync(UbicacionId, EmpresaId))
            .ReturnsAsync(new Ubicacion { Id = UbicacionId, Nombre = "Bodega Norte" });

        var result = await _service.GetByIdAsync(ClienteId, EmpresaId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(ClienteId);
        result.UbicacionDefectoNombre.Should().Be("Bodega Norte");
    }

    [Fact]
    public async Task CreateAsync_CuandoDatosValidos_CreaClienteContactosYAudita()
    {
        _repo.Setup(r => r.ExisteRucAsync("J123456789", EmpresaId, null)).ReturnsAsync(false);
        _repo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync(ClienteId);
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId)).ReturnsAsync(new List<ContactoCliente>());

        var result = await _service.CreateAsync(DtoValido(), EmpresaId);

        result.Id.Should().Be(ClienteId);
        _repo.Verify(r => r.CreateContactoAsync(It.IsAny<ContactoCliente>()), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", ClienteId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CuandoRucYaExiste_LanzaConflictException()
    {
        _repo.Setup(r => r.ExisteRucAsync("J123456789", EmpresaId, null)).ReturnsAsync(true);

        var act = async () => await _service.CreateAsync(DtoValido(), EmpresaId);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Ya existe un cliente con el RUC/NIT*");
    }

    [Fact]
    public async Task UpdateAsync_CuandoExiste_ActualizaContactosPosicionalmente()
    {
        var existente = ClienteEntity(ClienteId);
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(existente);
        _repo.Setup(r => r.ExisteRucAsync("J123456789", EmpresaId, ClienteId)).ReturnsAsync(false);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(true);
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId))
            .ReturnsAsync(new List<ContactoCliente> { ContactoEntity(ContactoId), ContactoEntity(Guid.NewGuid()) });
        _repo.Setup(r => r.UpdateContactoAsync(It.IsAny<ContactoCliente>())).ReturnsAsync(true);

        var dto = DtoValido();
        dto.Contactos = [new ContactoClienteRequestDto { Nombre = "Juan Pérez", Rol = RolContacto.Logistica, EsPrincipal = true }];

        var result = await _service.UpdateAsync(ClienteId, dto, EmpresaId);

        result.Id.Should().Be(ClienteId);
        _repo.Verify(r => r.UpdateContactoAsync(It.IsAny<ContactoCliente>()), Times.Once);
        // El contacto que sobra se desactiva (soft delete, nunca DELETE físico).
        _repo.Verify(r => r.DeactivateContactoAsync(It.IsAny<Guid>(), EmpresaId), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync((Cliente?)null);

        var act = async () => await _service.UpdateAsync(ClienteId, DtoValido(), EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_CuandoExiste_DesactivaConSoftDeleteYAudita()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.DeactivateAsync(ClienteId, EmpresaId)).ReturnsAsync(true);

        var ok = await _service.DeactivateAsync(ClienteId, EmpresaId);

        ok.Should().BeTrue();
        _repo.Verify(r => r.DeactivateAsync(ClienteId, EmpresaId), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.DEACTIVATE, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", ClienteId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync((Cliente?)null);

        var act = async () => await _service.DeactivateAsync(ClienteId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CambiarEstadoCreditoAsync_CuandoEstadoValido_ActualizaYAudita()
    {
        var clienteAlDia = ClienteEntity(ClienteId);
        var clienteBloqueado = ClienteEntity(ClienteId);
        clienteBloqueado.EstadoCredito = EstadoCredito.Bloqueado;
        // 1ª llamada: validación (anterior = AL_DIA). 2ª llamada: relectura tras el UPDATE.
        _repo.SetupSequence(r => r.GetByIdAsync(ClienteId, EmpresaId))
            .ReturnsAsync(clienteAlDia)
            .ReturnsAsync(clienteBloqueado);
        _repo.Setup(r => r.ActualizarEstadoCreditoAsync(ClienteId, EstadoCredito.Bloqueado, EmpresaId)).ReturnsAsync(true);
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId)).ReturnsAsync(new List<ContactoCliente>());

        var result = await _service.CambiarEstadoCreditoAsync(ClienteId, EstadoCredito.Bloqueado, EmpresaId);

        result.EstadoCredito.Should().Be(EstadoCredito.Bloqueado);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.CAMBIO_ESTADO, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", ClienteId,
            It.Is<object?>(d => d!.ToString()!.Contains(EstadoCredito.AlDia)), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoCreditoAsync_CuandoEstadoInvalido_LanzaBusinessException()
    {
        var act = async () => await _service.CambiarEstadoCreditoAsync(ClienteId, "NO_EXISTE", EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("El estado de crédito no es válido.*");
    }

    [Fact]
    public async Task AgregarContactoAsync_CuandoNombreVacio_LanzaBusinessException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));

        var dto = new ContactoClienteRequestDto { Nombre = "   " };
        var act = async () => await _service.AgregarContactoAsync(ClienteId, dto, EmpresaId);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("El nombre del contacto es obligatorio.*");
    }

    [Fact]
    public async Task AgregarContactoAsync_CuandoValido_CreaContactoYAudita()
    {
        var nuevoContacto = ContactoEntity(ContactoId);
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.CreateContactoAsync(It.IsAny<ContactoCliente>())).ReturnsAsync(ContactoId);
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId))
            .ReturnsAsync(new List<ContactoCliente> { nuevoContacto });

        var result = await _service.AgregarContactoAsync(
            ClienteId, new ContactoClienteRequestDto { Nombre = "Ana Torres", Rol = RolContacto.Compras }, EmpresaId);

        result.Id.Should().Be(ContactoId);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "ContactoCliente", ContactoId, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContactoAsync_CuandoNoExisteContacto_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId)).ReturnsAsync(new List<ContactoCliente>());

        var act = async () => await _service.UpdateContactoAsync(
            ClienteId, ContactoId, new ContactoClienteRequestDto { Nombre = "X" }, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateContactoAsync_CuandoRepoDevuelveFalse_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.DeactivateContactoAsync(ContactoId, EmpresaId)).ReturnsAsync(false);

        var act = async () => await _service.DeactivateContactoAsync(ClienteId, ContactoId, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateContactoAsync_CuandoExiste_ActualizaAuditaYRetornaContacto()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        // El servicio re-lee los contactos para construir la respuesta (línea 250).
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId))
            .ReturnsAsync(() => new List<ContactoCliente>
            {
                new()
                {
                    Id = ContactoId,
                    EmpresaId = EmpresaId,
                    ClienteId = ClienteId,
                    Nombre = "María López",
                    Rol = RolContacto.Finanzas,
                    EsPrincipal = false,
                    Activo = true
                }
            });
        _repo.Setup(r => r.UpdateContactoAsync(It.IsAny<ContactoCliente>())).ReturnsAsync(true);

        var dto = new ContactoClienteRequestDto
        {
            Nombre = "María López",
            Rol = RolContacto.Finanzas,
            Email = "maria@abc.com",
            EsPrincipal = false
        };

        var result = await _service.UpdateContactoAsync(ClienteId, ContactoId, dto, EmpresaId);

        result.Nombre.Should().Be("María López");
        _repo.Verify(r => r.UpdateContactoAsync(It.Is<ContactoCliente>(c =>
            c.Id == ContactoId && c.Nombre == "María López")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.UPDATE, EmpresaId,
            It.IsAny<Guid?>(), "ContactoCliente", ContactoId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task UpdateContactoAsync_CuandoContactoNoExiste_LanzaNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.GetContactosAsync(ClienteId, EmpresaId)).ReturnsAsync(new List<ContactoCliente>());

        var act = async () => await _service.UpdateContactoAsync(
            ClienteId, ContactoId, new ContactoClienteRequestDto { Nombre = "Nadie" }, EmpresaId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateContactoAsync_CuandoExiste_RetornaTrueYAudita()
    {
        _repo.Setup(r => r.GetByIdAsync(ClienteId, EmpresaId)).ReturnsAsync(ClienteEntity(ClienteId));
        _repo.Setup(r => r.DeactivateContactoAsync(ContactoId, EmpresaId)).ReturnsAsync(true);

        var result = await _service.DeactivateContactoAsync(ClienteId, ContactoId, EmpresaId);

        result.Should().BeTrue();
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.DEACTIVATE, EmpresaId,
            It.IsAny<Guid?>(), "ContactoCliente", ContactoId, It.IsAny<object?>(), null, null), Times.Once);
    }

    [Fact]
    public async Task ImportarCsvAsync_FilasInvalidas_FailSoftSinPropagar()
    {
        // ADR-017: las filas inválidas se omiten y se registran en el log; nunca se lanza.
        // "X" tiene menos de 2 campos y "@@@" no pasa el regex del RUC →
        // ninguna fila se importa y el proceso continúa.
        var csv = "Nombre;RUC;Tipo;Email;Teléfono;Ciudad;Crédito días;Límite crédito;Estado crédito\n" +
                  "\n" +
                  "X\n" +
                  "Empresa Invalida;@@@;REGULAR;x@x.com;;;15;0\n";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var importados = await _service.ImportarCsvAsync(stream, EmpresaId);

        importados.Should().Be(0); // Fail-soft: las filas inválidas se omiten.
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", null, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ImportarCsvAsync_CuandoFilaValida_ImportaRegistraYAudita()
    {
        var csv = "Nombre;RUC;Tipo;Email;Teléfono;Ciudad;Crédito días;Límite crédito;Estado crédito\n" +
                  "Transportes ABC;J123;REGULAR;contacto@abc.com;555-0100;Managua;30;10000\n";
        _repo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync(ClienteId);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var importados = await _service.ImportarCsvAsync(stream, EmpresaId);

        importados.Should().Be(1);
        _repo.Verify(r => r.CreateAsync(It.Is<Cliente>(c => c.Nombre == "Transportes ABC" && c.RucNit == "J123")), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.CREATE, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", null, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExportarExcelAsync_RetornaCsvConBomUtf8YAudita()
    {
        _repo.Setup(r => r.GetAllAsync(EmpresaId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<Cliente> { ClienteEntity(ClienteId) });

        var bytes = await _service.ExportarExcelAsync(EmpresaId);

        // BOM UTF-8 para compatibilidad Excel (ADR-017).
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var contenido = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        contenido.Should().Contain("Nombre;RUC;Tipo;Email");
        contenido.Should().Contain("Transportes ABC");
        _auditoria.Verify(a => a.RegistrarAsync("clientes", AccionAuditoria.EXPORT, EmpresaId,
            It.IsAny<Guid?>(), "Cliente", null, It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }
}