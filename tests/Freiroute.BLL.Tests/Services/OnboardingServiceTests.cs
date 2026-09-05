using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Onboarding;
using Freiroute.DTO.Usuario;
using Freiroute.Entity;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del wizard de onboarding multi-paso (HU-012, ADR-010).
/// Cubre el avance por pasos, la persistencia de modos de transporte (CA-04),
/// el máximo de invitaciones (CA-06) y la finalización (CA-08).
/// </summary>
public class OnboardingServiceTests
{
    private readonly Mock<IEmpresaRepository> _empresas;
    private readonly Mock<IConfiguracionRepository> _config;
    private readonly Mock<IUsuarioService> _usuarios;
    private readonly Mock<IStorageService> _storage;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly OnboardingService _service;

    public OnboardingServiceTests()
    {
        _empresas = new Mock<IEmpresaRepository>();
        _config = new Mock<IConfiguracionRepository>();
        _usuarios = new Mock<IUsuarioService>();
        _storage = new Mock<IStorageService>();
        _auditoria = new Mock<IAuditoriaService>();
        _service = new OnboardingService(
            _empresas.Object, _config.Object, _usuarios.Object, _storage.Object,
            _auditoria.Object, Mock.Of<ILogger<OnboardingService>>());
    }

    private Empresa EmpresaEn(Guid id, int paso) => new()
    {
        Id = id, Nombre = "Trans SA", OnboardingPasoActual = paso,
        OnboardingCompletado = false
    };

    [Fact]
    public async Task GetEstadoAsync_DevuelvePasoYPorcentaje()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 2));

        var result = await _service.GetEstadoAsync(id);

        result.PasoActual.Should().Be(2);
        result.PorcentajeCompletado.Should().Be(40); // 2/5 = 40%
        result.Completado.Should().BeFalse();
    }

    [Fact]
    public async Task GuardarPaso1Async_ActualizaDatosYAvanza()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 1));
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);

        var ok = await _service.GuardarPaso1Async(
            new OnboardingPaso1RequestDto { Nombre = "Trans Nicaragua SA", Industria = "Logística" }, id);

        ok.Should().BeTrue();
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e =>
            e.Nombre == "Trans Nicaragua SA" && e.OnboardingPasoActual == 2)), Times.Once);

        // Fix re-smoke test: el avance del wizard se persiste explícitamente en BD
        // (onboarding_paso_actual = 2) de forma independiente del UPDATE masivo.
        _empresas.Verify(r => r.ActualizarOnboardingAsync(id, 2, false), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso3Async_PersisteModosActivos_YAvanza()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 3));
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);
        _config.Setup(r => r.UpdateModosTransporteAsync(id, It.IsAny<string[]>()))
            .ReturnsAsync(true);

        await _service.GuardarPaso3Async(
            new OnboardingPaso3RequestDto
            {
                Moneda = "NIO", ModosTransporteActivos = ["FTL", "LTL", "AEREO"]
            }, id);

        // Fix re-smoke test: los modos se persisten en empresas.modos_transporte_activos
        // (TEXT[]) vía repositorio de config — ya no se serializan como string de la entidad.
        _config.Verify(r => r.UpdateModosTransporteAsync(
            id,
            It.Is<string[]>(m =>
                m.Length == 3 &&
                m.Contains("FTL") && m.Contains("LTL") && m.Contains("AEREO"))),
            Times.Once);

        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e =>
            e.MonedaPrincipal == "NIO" &&
            e.OnboardingPasoActual == 4)), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso4Async_UsuarioNoExiste_LanzaNotFound()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 4));
        _usuarios.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), id)).ReturnsAsync((UsuarioResponseDto?)null);

        var act = async () => await _service.GuardarPaso4Async(
            new OnboardingPaso4RequestDto { NombreCompleto = "Juan Pérez" }, id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GuardarPaso4Async_Exitoso_ActualizaAdminYAvanza()
    {
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 4));
        _usuarios.Setup(r => r.GetByIdAsync(usuarioId, id)).ReturnsAsync(
            new UsuarioResponseDto { Id = usuarioId, Email = "a@b.com", PerfilId = Guid.NewGuid(), TipoUsuario = "ADMIN" });
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);

        var ok = await _service.GuardarPaso4Async(
            new OnboardingPaso4RequestDto { NombreCompleto = "Juan Pérez" }, id, usuarioId);

        ok.Should().BeTrue();
        _usuarios.Verify(r => r.UpdateAsync(usuarioId, It.IsAny<UsuarioRequestDto>(), id), Times.Once);
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e => e.OnboardingPasoActual == 5)), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso5Async_MasDe5Invitaciones_LanzaBusinessError()
    {
        var invitaciones = Enumerable.Range(0, 6)
            .Select(i => new InvitacionRequestDto { Email = $"u{i}@b.com", PerfilId = Guid.NewGuid() })
            .ToList();

        var act = async () => await _service.GuardarPaso5Async(
            new OnboardingPaso5RequestDto { Invitaciones = invitaciones }, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task CompletarAsync_MarcaCompletadoYAudita()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 5));
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);

        var ok = await _service.CompletarAsync(id);

        ok.Should().BeTrue();
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e =>
            e.OnboardingCompletado && e.OnboardingPasoActual == 5)), Times.Once);

        // Fix re-smoke test: CompletarAsync garantiza el estado final en BD
        // (onboarding_paso_actual = 5 y onboarding_completado = true).
        _empresas.Verify(r => r.ActualizarOnboardingAsync(id, 5, true), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso2Async_ConLogoUrlSet_ActualizaColoresYLogoYAvanza()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 2));
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);

        var ok = await _service.GuardarPaso2Async(
            new OnboardingPaso2RequestDto
            {
                ColorPrimario = "#FF0000",
                ColorSecundario = "#00FF00",
                LogoUrl = "https://storage.example.com/logo.png"
            }, id);

        ok.Should().BeTrue();
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e =>
            e.ColorPrimario == "#FF0000" &&
            e.ColorSecundario == "#00FF00" &&
            e.LogoUrl == "https://storage.example.com/logo.png" &&
            e.OnboardingPasoActual == 3)), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso2Async_SinLogoUrl_ConservaLogoExistente()
    {
        var id = Guid.NewGuid();
        var empresa = EmpresaEn(id, 2);
        empresa.LogoUrl = "https://storage.example.com/logo-actual.png";
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(empresa);
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);

        var ok = await _service.GuardarPaso2Async(
            new OnboardingPaso2RequestDto
            {
                ColorPrimario = "#AABBCC",
                ColorSecundario = "#112233",
                LogoUrl = null
            }, id);

        ok.Should().BeTrue();
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e =>
            e.LogoUrl == "https://storage.example.com/logo-actual.png")), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso2Async_RegistraAuditoriaDelPaso()
    {
        var id = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(EmpresaEn(id, 2));
        _empresas.Setup(r => r.UpdateAsync(It.IsAny<Empresa>())).ReturnsAsync(true);
        _auditoria.Setup(a => a.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null,
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        await _service.GuardarPaso2Async(
            new OnboardingPaso2RequestDto
            {
                ColorPrimario = "#000000",
                ColorSecundario = "#FFFFFF"
            }, id);

        _auditoria.Verify(a => a.RegistrarAsync(
            "onboarding",
            It.IsAny<string>(),
            id, null,
            "onboarding", id,
            It.IsAny<object>(),
            null, null), Times.Once);
    }

    [Fact]
    public async Task GuardarPaso5Async_ListaVacia_SkipNoInvocaInvitar()
    {
        var empresaId = Guid.NewGuid();
        var invitadoPorId = Guid.NewGuid();

        var ok = await _service.GuardarPaso5Async(
            new OnboardingPaso5RequestDto { Invitaciones = [] },
            empresaId, invitadoPorId);

        ok.Should().BeTrue();
        _usuarios.Verify(
            u => u.InvitarAsync(It.IsAny<InvitacionRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task GuardarPaso5Async_CuatroInvitaciones_InvocaInvitarElNumeroCorrectoDeVeces()
    {
        var empresaId = Guid.NewGuid();
        var invitadoPorId = Guid.NewGuid();
        var invitaciones = Enumerable.Range(0, 4)
            .Select(i => new InvitacionRequestDto { Email = $"u{i}@b.com", PerfilId = Guid.NewGuid() })
            .ToList();
        _usuarios.Setup(u => u.InvitarAsync(It.IsAny<InvitacionRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var ok = await _service.GuardarPaso5Async(
            new OnboardingPaso5RequestDto { Invitaciones = invitaciones },
            empresaId, invitadoPorId);

        ok.Should().BeTrue();
        _usuarios.Verify(
            u => u.InvitarAsync(It.IsAny<InvitacionRequestDto>(), empresaId, invitadoPorId),
            Times.Exactly(4));
    }

    [Fact]
    public async Task GuardarLogoAsync_SubeYGuardaSignedUrl()
    {
        var id = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3, 4]);
        _storage.Setup(r => r.UploadAsync("logos-tenants", id.ToString(), "logo.png",
                stream, "image/png"))
            .ReturnsAsync($"{id}/logo.png");
        _storage.Setup(r => r.GetSignedUrlAsync("logos-tenants", $"{id}/logo.png", 86400))
            .ReturnsAsync("https://signed/url.png");
        _config.Setup(r => r.UpdateLogoUrlAsync(id, It.IsAny<string?>())).ReturnsAsync(true);

        var result = await _service.GuardarLogoAsync(id, stream, ".png");

        result.Should().Be("https://signed/url.png");
        _config.Verify(r => r.UpdateLogoUrlAsync(id, "https://signed/url.png"), Times.Once);
    }
}
