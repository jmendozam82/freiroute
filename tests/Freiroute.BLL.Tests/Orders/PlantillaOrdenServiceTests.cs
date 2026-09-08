using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Orders;

public class PlantillaOrdenServiceTests
{
    private readonly Mock<IPlantillaOrdenRepository> _plantillaRepoMock;
    private readonly Mock<IOrdenService> _ordenServiceMock;
    private readonly Mock<ILogger<PlantillaOrdenService>> _loggerMock;
    private readonly Mock<IAuditoriaRepository> _auditoriaMock;
    private readonly PlantillaOrdenService _service;

    public PlantillaOrdenServiceTests()
    {
        _plantillaRepoMock = new Mock<IPlantillaOrdenRepository>();
        _ordenServiceMock = new Mock<IOrdenService>();
        _loggerMock = new Mock<ILogger<PlantillaOrdenService>>();
        _auditoriaMock = new Mock<IAuditoriaRepository>();
        _service = new PlantillaOrdenService(_plantillaRepoMock.Object, _ordenServiceMock.Object, _auditoriaMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GuardarComoPlantillaAsync_GuardaCorrectamente()
    {
        var empresaId = Guid.NewGuid();
        var ordenId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var dto = new PlantillaOrdenRequestDto
        {
            Nombre = "Plantilla 1",
            Descripcion = "Desc"
        };

        var ordenDto = new OrdenResponseDto { Id = ordenId };
        _ordenServiceMock.Setup(s => s.GetByIdAsync(ordenId, empresaId)).ReturnsAsync(ordenDto);

        var plantillaId = Guid.NewGuid();
        _plantillaRepoMock.Setup(r => r.CreateAsync(It.IsAny<PlantillaOrden>())).ReturnsAsync(plantillaId);
        // El servicio vuelve a leer la plantilla al final para construir la respuesta
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId))
            .ReturnsAsync(new PlantillaOrden
            {
                Id = plantillaId,
                EmpresaId = empresaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                EsRecurrente = false,
                Activo = true
            });

        var result = await _service.GuardarComoPlantillaAsync(ordenId, dto, empresaId, usuarioId);

        result.Id.Should().Be(plantillaId);
        result.Nombre.Should().Be(dto.Nombre);
        // CA-01: debe persistir un snapshot JSON con el estado de la orden
        _plantillaRepoMock.Verify(r => r.CreateAsync(It.Is<PlantillaOrden>(p =>
            p.EmpresaId == empresaId &&
            p.EsRecurrente == false &&
            p.DatosOrden != null &&
            p.DatosOrden.Contains("ClienteId"))), Times.Once);
    }

    [Fact]
    public async Task CrearOrdenDesdePlantillaAsync_LlamaOrdenServiceCreate()
    {
        var empresaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var plantillaId = Guid.NewGuid();

        var plantilla = new PlantillaOrden
        {
            Id = plantillaId,
            EmpresaId = empresaId,
            DatosOrden = "{ \"ClienteId\": \"00000000-0000-0000-0000-000000000000\" }"
        };

        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId)).ReturnsAsync(plantilla);
        _ordenServiceMock.Setup(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), empresaId, usuarioId))
            .ReturnsAsync(new OrdenResponseDto { Id = Guid.NewGuid(), Estado = OrdenEstado.Draft });

        var result = await _service.CrearOrdenDesdePlantillaAsync(plantillaId, empresaId, usuarioId);

        result.Should().NotBeNull();
        _ordenServiceMock.Verify(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), empresaId, usuarioId), Times.Once);
    }

[Fact]
    public async Task ProcesarRecurrenciasPendientesAsync_ProcesaYActualizaFecha()
    {
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var plantillas = new List<PlantillaOrden>
        {
            new PlantillaOrden
            {
                Id = plantillaId,
                EmpresaId = empresaId,
                CreadoPor = Guid.NewGuid(),
                FrecuenciaRecurrencia = FrecuenciaRecurrencia.Diaria,
                ProximaEjecucion = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DatosOrden = "{}"
            }
        };

        _plantillaRepoMock.Setup(r => r.GetRecurrentesPendientesAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync(plantillas);
        // CrearOrdenDesdePlantillaAsync vuelve a leer la plantilla por Id
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId))
            .ReturnsAsync(plantillas[0]);

        _ordenServiceMock.Setup(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrdenResponseDto { Id = Guid.NewGuid() });

        await _service.ProcesarRecurrenciasPendientesAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        _ordenServiceMock.Verify(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        _plantillaRepoMock.Verify(r => r.UpdateProximaEjecucionAsync(plantillaId, empresaId, It.IsAny<DateOnly>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarRecurrenciasPendientesAsync_ToleranciaFallosEnBucle()
    {
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();
        var p1 = new PlantillaOrden { Id = Guid.NewGuid(), EmpresaId = emp1, CreadoPor = Guid.NewGuid(), FrecuenciaRecurrencia = FrecuenciaRecurrencia.Diaria, DatosOrden = "{}" };
        var p2 = new PlantillaOrden { Id = Guid.NewGuid(), EmpresaId = emp2, CreadoPor = Guid.NewGuid(), FrecuenciaRecurrencia = FrecuenciaRecurrencia.Diaria, DatosOrden = "{}" };
        var plantillas = new List<PlantillaOrden> { p1, p2 };

        _plantillaRepoMock.Setup(r => r.GetRecurrentesPendientesAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync(plantillas);
        // Cada plantilla se vuelve a leer por Id dentro de CrearOrdenDesdePlantillaAsync
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(p1.Id, emp1)).ReturnsAsync(p1);
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(p2.Id, emp2)).ReturnsAsync(p2);

        // Falla en el primero
        _ordenServiceMock.SetupSequence(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new System.Exception("Error en 1"))
            .ReturnsAsync(new OrdenResponseDto { Id = Guid.NewGuid() });

        await _service.ProcesarRecurrenciasPendientesAsync(DateOnly.FromDateTime(DateTime.UtcNow));

// Se deben haber llamado 2 veces, comprobando que no cortó por error
        _ordenServiceMock.Verify(s => s.CreateAsync(It.IsAny<OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Exactly(2));
        _plantillaRepoMock.Verify(r => r.UpdateProximaEjecucionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>()), Times.Once); // solo el segundo se actualiza
    }

    [Theory]
    [InlineData("DIARIA", 1)]
    [InlineData("SEMANAL", 7)]
    [InlineData("QUINCENAL", 15)]
    [InlineData("MENSUAL", 30)]
    public async Task ConfigurarRecurrenciaAsync_CalculaProximaEjecucion(string frecuencia, int dias)
    {
        // CA-04/CA-05 HU-027: proxima_ejecucion = hoy + frecuencia
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var plantilla = new PlantillaOrden { Id = plantillaId, EmpresaId = empresaId, Nombre = "P" };
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId)).ReturnsAsync(plantilla);
        _plantillaRepoMock.Setup(r => r.UpdateAsync(plantilla)).ReturnsAsync(true);

        var dto = new ConfigurarRecurrenciaRequestDto { EsRecurrente = true, FrecuenciaRecurrencia = frecuencia };
        var result = await _service.ConfigurarRecurrenciaAsync(plantillaId, dto, empresaId, Guid.NewGuid());

        result.EsRecurrente.Should().BeTrue();
        result.FrecuenciaRecurrencia.Should().Be(frecuencia.ToUpper());
        result.ProximaEjecucion.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dias));
    }

    [Fact]
    public async Task ConfigurarRecurrenciaAsync_ConEsRecurrenteFalse_LimpiaCampos()
    {
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var plantilla = new PlantillaOrden
        {
            Id = plantillaId,
            EmpresaId = empresaId,
            EsRecurrente = true,
            FrecuenciaRecurrencia = "DIARIA",
            ProximaEjecucion = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)
        };
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId)).ReturnsAsync(plantilla);
        _plantillaRepoMock.Setup(r => r.UpdateAsync(plantilla)).ReturnsAsync(true);

        var dto = new ConfigurarRecurrenciaRequestDto { EsRecurrente = false };
        var result = await _service.ConfigurarRecurrenciaAsync(plantillaId, dto, empresaId, Guid.NewGuid());

        _plantillaRepoMock.Verify(r => r.UpdateAsync(It.Is<PlantillaOrden>(p =>
            p.EsRecurrente == false &&
            p.FrecuenciaRecurrencia == null &&
            p.ProximaEjecucion == null)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaNombreYDescripcion()
    {
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var plantilla = new PlantillaOrden { Id = plantillaId, EmpresaId = empresaId, Nombre = "Viejo", Descripcion = "Vieja" };
        _plantillaRepoMock.Setup(r => r.GetByIdAsync(plantillaId, empresaId)).ReturnsAsync(plantilla);
        _plantillaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PlantillaOrden>())).ReturnsAsync(true);

        var dto = new PlantillaOrdenRequestDto { Nombre = "Nuevo", Descripcion = "Nueva" };
        var result = await _service.UpdateAsync(plantillaId, dto, empresaId, Guid.NewGuid());

        result.Nombre.Should().Be("Nuevo");
        result.Descripcion.Should().Be("Nueva");
        _plantillaRepoMock.Verify(r => r.UpdateAsync(It.Is<PlantillaOrden>(p =>
            p.Nombre == "Nuevo" && p.Descripcion == "Nueva")), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoTrue_RegistraAuditoria()
    {
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _plantillaRepoMock.Setup(r => r.DeactivateAsync(plantillaId, empresaId)).ReturnsAsync(true);

        var result = await _service.DeactivateAsync(plantillaId, empresaId, Guid.NewGuid());

        result.Should().BeTrue();
        _auditoriaMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa =>
            aa.Accion == "DELETE_PLANTILLA" && aa.Modulo == "ordenes" && aa.EntidadId == plantillaId)), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CuandoNoExiste_NoRegistraAuditoria()
    {
        var plantillaId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _plantillaRepoMock.Setup(r => r.DeactivateAsync(plantillaId, empresaId)).ReturnsAsync(false);

        var result = await _service.DeactivateAsync(plantillaId, empresaId, Guid.NewGuid());

        result.Should().BeFalse();
        _auditoriaMock.Verify(a => a.RegistrarAsync(It.IsAny<AuditoriaActividad>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_MapeaPlantillas()
    {
        var empresaId = Guid.NewGuid();
        var p1 = new PlantillaOrden { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "A", EsRecurrente = true, FrecuenciaRecurrencia = "SEMANAL", Activo = true };
        var p2 = new PlantillaOrden { Id = Guid.NewGuid(), EmpresaId = empresaId, Nombre = "B", EsRecurrente = false, Activo = false };
        _plantillaRepoMock.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync(new List<PlantillaOrden> { p1, p2 });

        var result = (await _service.GetAllAsync(empresaId)).ToList();

        result.Should().HaveCount(2);
        result[0].Nombre.Should().Be("A");
        result[0].FrecuenciaRecurrencia.Should().Be("SEMANAL");
        result[1].Activo.Should().BeFalse();
    }
}








