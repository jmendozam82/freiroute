using FluentAssertions;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text;
using System.Linq;

namespace Freiroute.BLL.Tests.Orders;

public class OrdenImportServiceTests
{
    private readonly Mock<IImportacionOrdenRepository> _importacionRepoMock;
    private readonly Mock<IOrdenRepository> _ordenRepoMock;
    private readonly Mock<IClienteRepository> _clienteRepoMock;
    private readonly Mock<IUbicacionRepository> _ubicacionRepoMock;
    private readonly Mock<ITipoMercanciaRepository> _tipoMercanciaRepoMock;
    private readonly Mock<IUnidadMedidaRepository> _unidadMedidaRepoMock;
    private readonly Mock<IAuditoriaRepository> _auditoriaRepoMock;
    private readonly Mock<ILogger<OrdenImportService>> _loggerMock;
    private readonly OrdenImportService _service;

    public OrdenImportServiceTests()
    {
        _importacionRepoMock = new Mock<IImportacionOrdenRepository>();
        _ordenRepoMock = new Mock<IOrdenRepository>();
        _clienteRepoMock = new Mock<IClienteRepository>();
        _ubicacionRepoMock = new Mock<IUbicacionRepository>();
        _tipoMercanciaRepoMock = new Mock<ITipoMercanciaRepository>();
        _unidadMedidaRepoMock = new Mock<IUnidadMedidaRepository>();
        _auditoriaRepoMock = new Mock<IAuditoriaRepository>();
        _loggerMock = new Mock<ILogger<OrdenImportService>>();

        _service = new OrdenImportService(
            _importacionRepoMock.Object,
            _ordenRepoMock.Object,
            _clienteRepoMock.Object,
            _ubicacionRepoMock.Object,
            _tipoMercanciaRepoMock.Object,
            _unidadMedidaRepoMock.Object,
            _auditoriaRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ImportarCsvAsync_FailSoft_RetornaResultados()
    {
        var csv = "ref,cliente,origen,destino\nREF-1,ClienteA,CiudadA,CiudadB";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _clienteRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<Cliente>());
        _ubicacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<Ubicacion>());
        _tipoMercanciaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(new List<TipoMercancia>());
        _unidadMedidaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<UnidadMedida>());
        _importacionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ImportacionOrden>())).ReturnsAsync(Guid.NewGuid());

        var result = await _service.ImportarCsvAsync(stream, "file.csv", Guid.NewGuid(), Guid.NewGuid());
        result.Should().NotBeNull();
        result.TotalFilas.Should().Be(1);
    }

[Fact]
    public async Task ObtenerPlantillaCsvAsync_RetornaCsv()
    {
        var result = await _service.ObtenerPlantillaCsvAsync();
        result.Should().Contain("referencia_cliente");
    }

    [Fact]
    public async Task ImportarCsvAsync_FilaValida_CreaOrdenConOrigenCsvYDraft()
    {
        // CA-03 HU-022: las filas válidas nacen DRAFT con origen_creacion = 'CSV'
        var cliente = new Cliente { Id = Guid.NewGuid(), Nombre = "Cliente A" };
        var origen = new Ubicacion { Id = Guid.NewGuid(), Ciudad = "Bogotá" };
        var destino = new Ubicacion { Id = Guid.NewGuid(), Ciudad = "Medellín" };
        var tipoM = new TipoMercancia { Id = Guid.NewGuid(), Nombre = "Electrónicos" };
        var unidad = new UnidadMedida { Id = Guid.NewGuid() };

        _clienteRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Cliente> { cliente });
        _ubicacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Ubicacion> { origen, destino });
        _tipoMercanciaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<TipoMercancia> { tipoM });
        _unidadMedidaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UnidadMedida> { unidad });
        var impId = Guid.NewGuid();
        _importacionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ImportacionOrden>())).ReturnsAsync(impId);

        var csv = "referencia_cliente,nombre_cliente,ciudad_origen,ciudad_destino,tipo_mercancia,cantidad,peso_kg,modo_transporte,nivel_servicio,fecha_pickup,fecha_entrega\n" +
                  "REF-001,Cliente A,Bogotá,Medellín,Electrónicos,10,250.5,TERRESTRE,ESTANDAR,2026-10-01,2026-10-05";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _service.ImportarCsvAsync(stream, "ordenes.csv", Guid.NewGuid(), Guid.NewGuid());

        result.FilasOk.Should().Be(1);
        result.FilasError.Should().Be(0);
        result.TotalFilas.Should().Be(1);
        result.ImportacionId.Should().Be(impId);

        _ordenRepoMock.Verify(r => r.CreateBulkAsync(It.Is<IEnumerable<Orden>>(o =>
            o.Single().OrigenCreacion == "CSV" &&
            o.Single().Estado == Freiroute.Utility.Constants.OrdenEstado.Draft &&
            o.Single().ReferenciaCliente == "REF-001" &&
            o.Single().EmpresaId != Guid.Empty)), Times.Once);

        _auditoriaRepoMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa =>
            aa.Accion == "IMPORTAR_ORDENES")), Times.Once); // CA-10 HU-022
    }

    [Fact]
    public async Task ImportarCsvAsync_FilaConColumnasInsuficientes_ReportaErrorSinCrearOrden()
    {
        _clienteRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Cliente>());
        _ubicacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Ubicacion>());
        _tipoMercanciaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<TipoMercancia>());
        _unidadMedidaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UnidadMedida>());
        _importacionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ImportacionOrden>())).ReturnsAsync(Guid.NewGuid());

        var csv = "headers\nREF-1,ClienteA,CiudadA"; // solo 3 columnas
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _service.ImportarCsvAsync(stream, "file.csv", Guid.NewGuid(), Guid.NewGuid());

        result.FilasError.Should().Be(1);
        result.DetalleErrores.Should().ContainSingle(e => e.Error == "No tiene suficientes columnas");
        _ordenRepoMock.Verify(r => r.CreateBulkAsync(It.IsAny<IEnumerable<Orden>>()), Times.Never);
    }

    [Fact]
    public async Task ImportarCsvAsync_ClienteNoEncontrado_ErrorDescriptivo()
    {
        // CA-09 HU-022: mensaje descriptivo exacto
        _clienteRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Cliente>());
        _ubicacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Ubicacion>
            {
                new Ubicacion { Id = Guid.NewGuid(), Ciudad = "Bogotá" },
                new Ubicacion { Id = Guid.NewGuid(), Ciudad = "Medellín" }
            });
        _tipoMercanciaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<TipoMercancia> { new TipoMercancia { Nombre = "Electrónicos" } });
        _unidadMedidaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UnidadMedida> { new UnidadMedida() });
        _importacionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ImportacionOrden>())).ReturnsAsync(Guid.NewGuid());

        var csv = "headers\nREF-1,Cliente Inexistente,Bogotá,Medellín,Electrónicos,10,250.5,TERRESTRE,ESTANDAR,2026-10-01,2026-10-05";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _service.ImportarCsvAsync(stream, "file.csv", Guid.NewGuid(), Guid.NewGuid());

        result.FilasError.Should().Be(1);
        result.DetalleErrores.Should().ContainSingle(e =>
            e.Campo == "nombre_cliente" && e.Error == "No se encontró un cliente con ese nombre");
    }

    [Fact]
    public async Task GetHistorialImportacionesAsync_ConDetalleErroresJson_DeserializaLista()
    {
        var erroresJson = "[{\"Fila\":2,\"Campo\":\"cantidad\",\"Error\":\"Debe ser número mayor a cero\"}]";
        var h = new ImportacionOrden
        {
            Id = Guid.NewGuid(),
            TotalFilas = 3,
            FilasOk = 2,
            FilasError = 1,
            DetalleErrores = erroresJson
        };
        _importacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ImportacionOrden> { h });

        var result = (await _service.GetHistorialImportacionesAsync(Guid.NewGuid())).ToList();

        result.Should().ContainSingle();
        result[0].DetalleErrores.Should().ContainSingle(e => e.Campo == "cantidad");
    }

    [Fact]
    public async Task GetHistorialImportacionesAsync_SinDetalleErrores_NoLanza()
    {
        var h = new ImportacionOrden { Id = Guid.NewGuid(), TotalFilas = 1, FilasOk = 0, FilasError = 1, DetalleErrores = null };
        _importacionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ImportacionOrden> { h });

        var result = await _service.GetHistorialImportacionesAsync(Guid.NewGuid());

        result.Should().ContainSingle(x => x.DetalleErrores != null && !x.DetalleErrores.Any());
    }
}


