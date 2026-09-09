using System;
using System.Threading.Tasks;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Moq;
using Xunit;
using BCrypt.Net;

namespace Freiroute.BLL.Tests.Orders;

public class OrdenApiExternaServiceTests
{
    private readonly Mock<IApiKeyTenantRepository> _apiKeyRepoMock;
    private readonly Mock<IOrdenService> _ordenServiceMock;
    private readonly Mock<IAuditoriaRepository> _auditoriaRepoMock;
    private readonly OrdenApiExternaService _service;

    public OrdenApiExternaServiceTests()
    {
        _apiKeyRepoMock = new Mock<IApiKeyTenantRepository>();
        _ordenServiceMock = new Mock<IOrdenService>();
        _auditoriaRepoMock = new Mock<IAuditoriaRepository>();
        _service = new OrdenApiExternaService(_apiKeyRepoMock.Object, _ordenServiceMock.Object, _auditoriaRepoMock.Object);
    }

    [Fact]
    public async Task GenerarApiKeyAsync_GeneraHashYGudaAuditoria()
    {
        var empresaId = Guid.NewGuid();
        var dto = new ApiKeyOrdenRequestDto { Nombre = "Key ERP" };
        var idRetorno = Guid.NewGuid();
        _apiKeyRepoMock.Setup(r => r.CreateAsync(It.IsAny<ApiKeyTenant>())).ReturnsAsync(idRetorno);

        var result = await _service.GenerarApiKeyAsync(dto, empresaId);

        result.Should().NotBeNull();
        result.RawKey.Should().StartWith("frk_live_");
        
        _apiKeyRepoMock.Verify(r => r.CreateAsync(It.Is<ApiKeyTenant>(k => 
            k.Nombre == dto.Nombre &&
            k.ClaveHash != result.RawKey && // nunca guardar en plano
            k.EmpresaId == empresaId
        )), Times.Once);

        _auditoriaRepoMock.Verify(a => a.RegistrarAsync(It.IsAny<AuditoriaActividad>()), Times.Once);
    }

    [Fact]
    public async Task ValidarApiKeyAsync_CuandoKeyValida_RetornaEmpresaId()
    {
        var rawKey = "frk_live_12345678abcdefg";
        var empresaId = Guid.NewGuid();

        _apiKeyRepoMock.Setup(r => r.GetByClaveHashAsync(rawKey))
            .ReturnsAsync(new ApiKeyTenant { Id = Guid.NewGuid(), EmpresaId = empresaId, Activo = true });

        var result = await _service.ValidarApiKeyAsync(rawKey);

        result.Should().Be(empresaId);
        _apiKeyRepoMock.Verify(r => r.ActualizarUltimoUsoAsync(It.IsAny<Guid>(), empresaId), Times.Once);
    }

    [Fact]
    public async Task ValidarApiKeyAsync_CuandoClaveInvalida_RetornaNull()
    {
        var rawKey = "invalid_key";
        _apiKeyRepoMock.Setup(r => r.GetByClaveHashAsync(rawKey)).ReturnsAsync((ApiKeyTenant?)null);

        var result = await _service.ValidarApiKeyAsync(rawKey);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CrearOrdenDesdeApiAsync_PasaValoresCorrectos()
    {
        var dto = new Freiroute.DTO.Orden.OrdenRequestDto();
        var empresaId = Guid.NewGuid();

        await _service.CrearOrdenDesdeApiAsync(dto, empresaId);

        _ordenServiceMock.Verify(s => s.CreateAsync(dto, empresaId, Guid.Empty, Freiroute.Utility.Constants.OrigenCreacion.Api), Times.Once);
    }

    [Fact]
    public async Task GetApiKeysAsync_RetornaKeysSinClaveHash()
    {
        var empresaId = Guid.NewGuid();
        var key = new ApiKeyTenant
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nombre = "Key ERP",
            ClaveHash = "hash_bcrypt_que_nunca_debe_salir",
            Activo = true
        };
        _apiKeyRepoMock.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync(new List<ApiKeyTenant> { key });

        var result = (await _service.GetApiKeysAsync(empresaId)).ToList();

        result.Should().ContainSingle();
        result[0].Nombre.Should().Be("Key ERP");
        // El DTO de respuesta no expone ClaveHash → nunca se filtra el hash (CA-08 HU-023)
        result[0].GetType().GetProperty("ClaveHash").Should().BeNull();
    }

    [Fact]
    public async Task DesactivarApiKeyAsync_CuandoTrue_RegistraAuditoria()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _apiKeyRepoMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(true);

        var result = await _service.DesactivarApiKeyAsync(id, empresaId);

        result.Should().BeTrue();
        _auditoriaRepoMock.Verify(a => a.RegistrarAsync(It.Is<AuditoriaActividad>(aa =>
            aa.Accion == "DEACTIVATE" && aa.Modulo == "configuracion" && aa.EntidadId == id)), Times.Once);
    }

    [Fact]
    public async Task DesactivarApiKeyAsync_CuandoNoExiste_NoRegistraAuditoria()
    {
        var id = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _apiKeyRepoMock.Setup(r => r.DeactivateAsync(id, empresaId)).ReturnsAsync(false);

        var result = await _service.DesactivarApiKeyAsync(id, empresaId);

        result.Should().BeFalse();
        _auditoriaRepoMock.Verify(a => a.RegistrarAsync(It.IsAny<AuditoriaActividad>()), Times.Never);
    }
}
