using FluentAssertions;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests del panel de administración global del Super Admin (HU-009/010/011).
/// Cubre la impersonación con JWT trazable (CA-05) y el cambio de plan (CA-04).
/// </summary>
public class AdminDashboardServiceTests
{
    private readonly Mock<IEmpresaRepository> _empresas;
    private readonly Mock<ISuscripcionRepository> _susc;
    private readonly Mock<IPagoRepository> _pagos;
    private readonly Mock<IPlanRepository> _planRepo;
    private readonly Mock<IUsuarioRepository> _usuarios;
    private readonly Mock<IPermisoRepository> _permisos;
    private readonly Mock<IJwtService> _jwt;
    private readonly Mock<IAuditoriaService> _auditoria;
    private readonly AdminDashboardService _service;

    public AdminDashboardServiceTests()
    {
        _empresas = new Mock<IEmpresaRepository>();
        _susc = new Mock<ISuscripcionRepository>();
        _pagos = new Mock<IPagoRepository>();
        _planRepo = new Mock<IPlanRepository>();
        _usuarios = new Mock<IUsuarioRepository>();
        _permisos = new Mock<IPermisoRepository>();
        _jwt = new Mock<IJwtService>();
        _auditoria = new Mock<IAuditoriaService>();

        _service = new AdminDashboardService(
            _empresas.Object, _susc.Object, _pagos.Object, _planRepo.Object,
            _usuarios.Object, _permisos.Object, _jwt.Object, _auditoria.Object,
            Mock.Of<ILogger<AdminDashboardService>>());
    }

    private static Empresa EmpresaActiva(Guid id) => new()
    {
        Id = id, Nombre = "Trans SA", Estado = EstadoEmpresa.ACTIVE,
        Activo = true, FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task GetDashboardGlobalAsync_AgregaMetricas()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _empresas.Setup(r => r.GetAllAsync()).ReturnsAsync([EmpresaActiva(id1), EmpresaActiva(id2)]);
        _pagos.Setup(r => r.GetMrrAsync()).ReturnsAsync(2000m);
        _susc.Setup(r => r.GetProximasAVencerAsync(15)).ReturnsAsync([]);

        var result = await _service.GetDashboardGlobalAsync();

        result.TotalEmpresasActivas.Should().Be(2);
        result.NuevasEstesMes.Should().Be(2);
        result.Mrr.Should().Be(2000m);
        result.Arr.Should().Be(24000m);
        result.TotalEmbarquesHoy.Should().Be(0); // Fase 2, no hay módulo.
    }

    [Fact]
    public async Task ImpersonarAsync_EmpresaNoExiste_LanzaNotFound()
    {
        _empresas.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Empresa?)null);

        var act = async () => await _service.ImpersonarAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ImpersonarAsync_EmpresaSuspendida_LanzaBusinessError()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        empresa.Estado = EstadoEmpresa.SUSPENDED;
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        var act = async () => await _service.ImpersonarAsync(empresa.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task ImpersonarAsync_SinAdminActivo_LanzaBusinessError()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _usuarios.Setup(r => r.GetAllAsync(empresa.Id)).ReturnsAsync(
            [new Usuario { Id = Guid.NewGuid(), TipoUsuario = TipoUsuario.OPERADOR, Estado = EstadoUsuario.SUSPENDED }]);

        var act = async () => await _service.ImpersonarAsync(empresa.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task ImpersonarAsync_Exitoso_GeneraTokenTrazableYAudita()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        var superAdminId = Guid.NewGuid();
        var admin = new Usuario
        {
            Id = Guid.NewGuid(), PerfilId = Guid.NewGuid(), TipoUsuario = TipoUsuario.ADMIN,
            Estado = EstadoUsuario.ACTIVE, NombreCompleto = "Admin", Email = "a@b.com"
        };
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _usuarios.Setup(r => r.GetAllAsync(empresa.Id)).ReturnsAsync([admin]);
        _permisos.Setup(r => r.GetByPerfilAsync(admin.PerfilId, empresa.Id)).ReturnsAsync([]);
        _jwt.Setup(r => r.GenerateImpersonationToken(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid>(), It.IsAny<int>()))
            .Returns("imp-token");
        _usuarios.Setup(r => r.GetByIdAsync(superAdminId, IdsSistema.EmpresaRaizId))
            .ReturnsAsync(new Usuario { Id = superAdminId });

        var result = await _service.ImpersonarAsync(empresa.Id, superAdminId);

        result.AccessToken.Should().Be("imp-token");
        result.Usuario!.TipoUsuario.Should().Be(TipoUsuario.ADMIN);
        _auditoria.Verify(a => a.RegistrarAsync(It.IsAny<string>(), AccionAuditoria.IMPERSONACION,
            empresa.Id, superAdminId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task CambiarPlanAsync_EmpresaNoExiste_LanzaNotFound()
    {
        _empresas.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Empresa?)null);

        var act = async () => await _service.CambiarPlanAsync(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CambiarPlanAsync_PlanInactivo_LanzaNotFound()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Plan { Id = Guid.NewGuid(), Activo = false });

        var act = async () => await _service.CambiarPlanAsync(empresa.Id, Guid.NewGuid(), null, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CambiarPlanAsync_ConSuscripcionActiva_ActualizaPlanYAplicaNuevoEstado()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        var plan = new Plan { Id = Guid.NewGuid(), Nombre = "Enterprise", Codigo = "ENTERPRISE", Activo = true };
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id)).ReturnsAsync(
            new Suscripcion { Id = Guid.NewGuid(), EmpresaId = empresa.Id, TipoCiclo = TipoCiclo.MENSUAL, Estado = EstadoSuscripcion.PAST_DUE });

        await _service.CambiarPlanAsync(empresa.Id, plan.Id, "upgrade", Guid.NewGuid());

        _susc.Verify(r => r.UpdateAsync(It.Is<Suscripcion>(s => s.PlanId == plan.Id && s.Estado == EstadoSuscripcion.ACTIVE)), Times.Once);
        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e => e.PlanSuscripcion == "ENTERPRISE" && e.Estado == EstadoEmpresa.ACTIVE)), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(It.IsAny<string>(), AccionAuditoria.CAMBIAR_PLAN,
            empresa.Id, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoEmpresaAsync_EstadoInvalido_LanzaBusinessError()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);

        var act = async () => await _service.CambiarEstadoEmpresaAsync(empresa.Id, "INVALIDO", Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task CambiarEstadoEmpresaAsync_Valido_ReflejaEnSuscripcion()
    {
        var empresa = EmpresaActiva(Guid.NewGuid());
        _empresas.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
        _susc.Setup(r => r.GetActivaByEmpresaIdAsync(empresa.Id)).ReturnsAsync(
            new Suscripcion { Id = Guid.NewGuid(), EmpresaId = empresa.Id, Estado = EstadoSuscripcion.ACTIVE });

        await _service.CambiarEstadoEmpresaAsync(empresa.Id, EstadoEmpresa.SUSPENDED, Guid.NewGuid());

        _empresas.Verify(r => r.UpdateAsync(It.Is<Empresa>(e => e.Estado == EstadoEmpresa.SUSPENDED)), Times.Once);
        _susc.Verify(r => r.UpdateAsync(It.Is<Suscripcion>(s => s.Estado == EstadoSuscripcion.SUSPENDED)), Times.Once);
    }

    [Fact]
    public async Task GetDashboardFinancieroAsync_CalculaMrrArrIngresos()
    {
        _pagos.Setup(p => p.GetMrrAsync()).ReturnsAsync(1000m);
        _pagos.Setup(p => p.GetIngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(500m);

        var result = await _service.GetDashboardFinancieroAsync();

        result.Mrr.Should().Be(1000m);
        result.Arr.Should().Be(12000m);
        result.IngresosMes.Should().Be(500m);
        result.IngresosAño.Should().Be(6000m);
        _pagos.Verify(p => p.GetIngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(13));
    }

    [Fact]
    public async Task GetDashboardGlobalAsync_ConTenantsPorVencer_DevuelveLista()
    {
        var empresaId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var suscId = Guid.NewGuid();

        var empresa = EmpresaActiva(empresaId);
        _empresas.Setup(r => r.GetAllAsync()).ReturnsAsync([empresa]);
        _pagos.Setup(p => p.GetMrrAsync()).ReturnsAsync(500m);

        _susc.Setup(s => s.GetProximasAVencerAsync(15)).ReturnsAsync([
            new Suscripcion
            {
                Id = suscId,
                EmpresaId = empresaId,
                PlanId = planId,
                TipoCiclo = TipoCiclo.MENSUAL,
                FechaInicio = DateTime.UtcNow.AddDays(-25),
                FechaVencimiento = DateTime.UtcNow.AddDays(5),
                Estado = EstadoSuscripcion.ACTIVE,
                PrecioPactado = 99m,
                MonedaPactada = "USD",
                Activo = true,
                FechaCreacion = DateTime.UtcNow.AddDays(-30)
            }
        ]);

        _planRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(
            new Plan { Id = planId, Nombre = "Pro", Codigo = "PRO", Activo = true });
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(empresa);

        var result = await _service.GetDashboardGlobalAsync();

        result.TenantsPorVencer.Should().HaveCount(1);
        result.TenantsPorVencer[0].Id.Should().Be(suscId);
        result.TenantsPorVencer[0].EmpresaId.Should().Be(empresaId);
        result.TenantsPorVencer[0].PlanNombre.Should().Be("Pro");
        result.TenantsPorVencer[0].EmpresaNombre.Should().Be("Trans SA");
    }
}
