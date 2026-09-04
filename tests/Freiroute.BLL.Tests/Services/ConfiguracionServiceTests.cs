using FluentValidation;
using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Configuracion;
using Freiroute.Entity;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de la configuración general del tenant (HU-014).
/// Cubre get/update de configuración, numeración (prefijos) y subida/borrado de logo.
/// </summary>
public class ConfiguracionServiceTests
{
    private readonly Mock<IConfiguracionRepository> _config;
    private readonly Mock<IEmpresaRepository> _empresas;
    private readonly Mock<IStorageService> _storage;
    private readonly Mock<IValidator<ConfiguracionRequestDto>> _configValidator;
    private readonly Mock<IValidator<NumeracionRequestDto>> _numeracionValidator;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly ConfiguracionService _service;

    public ConfiguracionServiceTests()
    {
        _config = new Mock<IConfiguracionRepository>();
        _empresas = new Mock<IEmpresaRepository>();
        _storage = new Mock<IStorageService>();
        _configValidator = new Mock<IValidator<ConfiguracionRequestDto>>();
        _numeracionValidator = new Mock<IValidator<NumeracionRequestDto>>();
        _auditoria = new Mock<IAuditoriaService>();

        _configValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ConfiguracionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _numeracionValidator
            .Setup(v => v.ValidateAsync(It.IsAny<NumeracionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new ConfiguracionService(
            _config.Object, _empresas.Object, _storage.Object,
            _configValidator.Object, _numeracionValidator.Object,
            _auditoria.Object, Mock.Of<ILogger<ConfiguracionService>>());
    }

    private static Empresa Empresa(Guid id) => new()
    {
        Id = id, Nombre = "Trans SA", MonedaPrincipal = "USD",
        ZonaHoraria = "America/Managua", FormatoFecha = "DD/MM/YYYY",
        PrefijoEmbarque = "FR", PrefijoOrden = "ORD", PrefijoCartaPorte = "CP"
    };

    [Fact]
    public async Task GetAsync_EmpresaNoExiste_LanzaNotFound()
    {
        _config.Setup(r => r.GetConfiguracionAsync(It.IsAny<Guid>())).ReturnsAsync((Empresa?)null);

        var act = async () => await _service.GetAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAsync_Exitoso_DevuelveConfiguracion()
    {
        var id = Guid.NewGuid();
        _config.Setup(r => r.GetConfiguracionAsync(id)).ReturnsAsync(Empresa(id));

        var result = await _service.GetAsync(id);

        result.Nombre.Should().Be("Trans SA");
        result.Moneda.Should().Be("USD");
    }

    [Fact]
    public async Task UpdateAsync_Exitoso_PersisteYDevuelveNuevo()
    {
        var id = Guid.NewGuid();
        _config.Setup(r => r.UpdateConfiguracionAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        // GetAsync posterior re-lee la empresa actualizada.
        _config.Setup(r => r.GetConfiguracionAsync(id)).ReturnsAsync(() =>
        {
            var e = Empresa(id);
            e.Nombre = "Trans Nicaragua SA";
            return e;
        });

        var result = await _service.UpdateAsync(new ConfiguracionRequestDto { Nombre = "Trans Nicaragua SA" }, id);

        result.EmpresaId.Should().Be(id); // EmpresaId
        _config.Verify(r => r.UpdateConfiguracionAsync(id, It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdateFallido_LanzaNotFound()
    {
        _config.Setup(r => r.UpdateConfiguracionAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(false);

        var act = async () => await _service.UpdateAsync(new ConfiguracionRequestDto { Nombre = "X" }, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetNumeracionAsync_Exitoso_DevuelvePrefijosYConsecutivos()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(Empresa(id));

        var result = await _service.GetNumeracionAsync(id);

        result.PrefijoEmbarque.Should().Be("FR");
        result.PrefijoOrden.Should().Be("ORD");
        result.PrefijoCartaPorte.Should().Be("CP");
    }

    [Fact]
    public async Task UpdateNumeracionAsync_Exitoso_ActualizaPrefijos()
    {
        var id = Guid.NewGuid();
        _config.Setup(r => r.UpdateNumeracionAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(() =>
        {
            var e = Empresa(id);
            e.PrefijoEmbarque = "NIC";
            return e;
        });

        var result = await _service.UpdateNumeracionAsync(
            new NumeracionRequestDto { PrefijoEmbarque = "NIC", PrefijoOrden = "ORD", PrefijoCartaPorte = "CP" }, id);

        result.PrefijoEmbarque.Should().Be("NIC");
    }

    [Fact]
    public async Task UpdateLogoAsync_Vacio_LanzaBusinessError()
    {
        Func<Task> act = () => _service.UpdateLogoAsync(Guid.NewGuid(), Stream.Null, "image/png");

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeleteLogoAsync_SubeLogo_YBorraReferencia()
    {
        var id = Guid.NewGuid();
        var empresa = Empresa(id);
        empresa.LogoUrl = "https://x/supabase/logos-tenants/abc/logo.png?token=1";
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(empresa);
        _config.Setup(r => r.UpdateLogoUrlAsync(id, null)).ReturnsAsync(true);
        _storage.Setup(r => r.DeleteAsync("logos-tenants", "abc/logo.png")).ReturnsAsync(true);

        var result = await _service.DeleteLogoAsync(id);

        result.Should().BeTrue();
        _config.Verify(r => r.UpdateLogoUrlAsync(id, null), Times.Once);
    }
}
