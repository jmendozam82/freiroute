using System.Data;
using FluentAssertions;
using Freiroute.BLL.Services;
using Freiroute.DAL.Repositories;
using Freiroute.DTO.Empresa;
using Freiroute.Entity;
using Moq;
using Xunit;

namespace Freiroute.BLL.Tests.EmpresaTests;

/// <summary>
/// Tests unitarios para EmpresaService - capa BLL del modulo EMPRESA (HU-001).
/// Verifica: validacion, derivacion de slug, unicidad, creacion y mapeo de respuesta.
/// Patrón AAA: Arrange → Act → Assert con FluentAssertions + Moq.
/// </summary>
public class EmpresaServiceTests
{
    [Fact]
    public async Task CrearAsync_CuandoDtoValido_RetornaIdNuevo()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        var nuevoId = Guid.NewGuid();
        mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Empresa)null!);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Empresa>())).ReturnsAsync(nuevoId);

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Transportes del Pacifico SA",
            Slug = "",
            Plan = "professional"
        };

        var resultado = await servicio.CrearAsync(dto);

        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(nuevoId);
        resultado.Nombre.Should().Be("Transportes del Pacifico SA");
        resultado.Slug.Should().Be("transportesdelpacificosa");
        resultado.Plan.Should().Be("professional");
        resultado.Activo.Should().BeTrue();
        resultado.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        mockRepo.Verify(r => r.CreateAsync(It.Is<Empresa>(e =>
            e.Nombre == "Transportes del Pacifico SA" &&
            e.Slug == "transportesdelpacificosa" &&
            e.Plan == "professional" &&
            e.Activo == true)), Times.Once);
        mockRepo.Verify(r => r.GetBySlugAsync("transportesdelpacificosa"), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_CuandoNombreInvalido_Vacio_LanzaValidationException()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Empresa)null!);

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "",
            Slug = "test-slug",
            Plan = "starter"
        };

        Func<Task> accion = async () => await servicio.CrearAsync(dto);

        await accion.Should().ThrowAsync<FluentValidation.ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Nombre" && e.ErrorMessage.Contains("obligatorio")));
    }

    [Fact]
    public async Task CrearAsync_CuandoNombreInvalido_MuyCorto_LanzaValidationException()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Empresa)null!);

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "AB",
            Slug = "test-slug",
            Plan = "enterprise"
        };

        Func<Task> accion = async () => await servicio.CrearAsync(dto);

        await accion.Should().ThrowAsync<FluentValidation.ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Nombre" && e.ErrorMessage.Contains("3 caracteres")));
    }

    [Fact]
    public async Task CrearAsync_CuandoSlugExistente_LanzaInvalidOperationException()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        var empresaExistente = new Empresa { Id = Guid.NewGuid(), Nombre = "Ya Existente", Slug = "mi-slug" };
        mockRepo.Setup(r => r.GetBySlugAsync("mi-slug")).ReturnsAsync(empresaExistente);

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Nueva Empresa",
            Slug = "mi-slug",
            Plan = "starter"
        };

        Func<Task> accion = async () => await servicio.CrearAsync(dto);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El slug 'mi-slug' ya está en uso.");
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<Empresa>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_CuandoSlugDerivadoEsDuplicado_LanzaInvalidOperationException()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        mockRepo.Setup(r => r.GetBySlugAsync("transportespacifico"))
            .ReturnsAsync(new Empresa { Id = Guid.NewGuid(), Nombre = "Otra", Slug = "transportespacifico" });

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Transportes Pacifico",
            Slug = "",
            Plan = "starter"
        };

        Func<Task> accion = async () => await servicio.CrearAsync(dto);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<Empresa>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_CuandoPlanInvalido_LanzaValidationException()
    {
        var mockRepo = new Mock<IEmpresaRepository>();

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Empresa Valida",
            Slug = "empresa-valida",
            Plan = "premium"
        };

        Func<Task> accion = async () => await servicio.CrearAsync(dto);

        await accion.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*plan debe ser starter, professional o enterprise*");
        mockRepo.Verify(r => r.GetBySlugAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_CuandoSlugProvidido_EsNormalizadoALowercase()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Empresa)null!);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Empresa>())).ReturnsAsync(Guid.NewGuid());

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "Mi Empresa",
            Slug = "Mi-Empresa-SLUG",
            Plan = "starter"
        };

        var resultado = await servicio.CrearAsync(dto);

        resultado.Should().NotBeNull();
        resultado.Slug.Should().Be("mi-empresa-slug");

        mockRepo.Verify(r => r.CreateAsync(It.Is<Empresa>(e => e.Slug == "mi-empresa-slug")), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_CuandoTrimNombre_ResultadoSinEspaciosExtra()
    {
        var mockRepo = new Mock<IEmpresaRepository>();
        mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Empresa)null!);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Empresa>())).ReturnsAsync(Guid.NewGuid());

        var servicio = new EmpresaService(mockRepo.Object);

        var dto = new EmpresaRequestDto
        {
            Nombre = "  EmpresaConEspacios  ",
            Slug = "",
            Plan = "enterprise"
        };

        var resultado = await servicio.CrearAsync(dto);

        resultado.Nombre.Should().Be("EmpresaConEspacios");
    }

    [Fact]
    public void Constructor_InyectaRepositorio_SinNullReference()
    {
        var mockRepo = new Mock<IEmpresaRepository>();

        var service = new EmpresaService(mockRepo.Object);

        service.Should().NotBeNull();
        mockRepo.VerifyAll();
    }
}
