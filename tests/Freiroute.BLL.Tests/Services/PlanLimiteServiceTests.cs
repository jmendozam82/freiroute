using FluentAssertions;
using Freiroute.BLL.Services;
using Freiroute.DAL.Interfaces;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Freiroute.BLL.Tests.Services;

/// <summary>
/// Tests de límites del plan por tenant (HU-013 CA-08, ADR-004).
/// Cubre el bloqueo por límite de usuarios, la tolerancia de límites ilimitados
/// y el gating de módulos por plan.
/// </summary>
public class PlanLimiteServiceTests
{
    private readonly Mock<IEmpresaRepository> _empresas;
    private readonly Mock<IPlanRepository> _planes;
    private readonly Mock<IUsuarioRepository> _usuarios;
    private readonly PlanLimiteService _service;

    public PlanLimiteServiceTests()
    {
        _empresas = new Mock<IEmpresaRepository>();
        _planes = new Mock<IPlanRepository>();
        _usuarios = new Mock<IUsuarioRepository>();
        _service = new PlanLimiteService(
            _empresas.Object, _planes.Object, _usuarios.Object,
            Mock.Of<ILogger<PlanLimiteService>>());
    }

    private static Empresa EmpresaConPlan(Guid id, Guid planId, string codigo) => new()
    {
        Id = id, Nombre = "Trans SA", PlanId = planId, PlanSuscripcion = codigo, Estado = EstadoEmpresa.ACTIVE
    };

    private static Plan Plan(Guid id, string codigo, int limiteUsuarios, params string[] modulos) => new()
    {
        Id = id, Nombre = codigo, Codigo = codigo, Activo = true,
        LimiteUsuarios = limiteUsuarios, ModulosDisponibles = modulos
    };

    [Fact]
    public async Task VerificarLimiteUsuariosAsync_Alcanzado_LanzaBusinessError()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "STARTER"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "STARTER", 5));
        // 5 usuarios activos → se alcanza el límite.
        _usuarios.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync(
            Enumerable.Range(0, 5).Select(_ => new Usuario { Id = Guid.NewGuid(), Estado = EstadoUsuario.ACTIVE }));
        // Plan superior PROFESSIONAL existe.
        _planes.Setup(r => r.GetAllAsync(true)).ReturnsAsync(
            [Plan(Guid.NewGuid(), "PROFESSIONAL", 20)]);

        var act = async () => await _service.VerificarLimiteUsuariosAsync(empresaId);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task VerificarLimiteUsuariosAsync_BajoLimite_NoLanza()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "PROFESSIONAL"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "PROFESSIONAL", 20));
        _usuarios.Setup(r => r.GetAllAsync(empresaId)).ReturnsAsync(
            Enumerable.Range(0, 3).Select(_ => new Usuario { Id = Guid.NewGuid() }));

        var act = async () => await _service.VerificarLimiteUsuariosAsync(empresaId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task VerificarLimiteUsuariosAsync_LimiteIlimitado_NoLanza()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "ENTERPRISE"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "ENTERPRISE", -1));

        var act = async () => await _service.VerificarLimiteUsuariosAsync(empresaId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ModuloDisponibleAsync_ModuloEnPlan_DevuelveTrue()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "PROFESSIONAL"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "PROFESSIONAL", 20, "embarques", "usuarios"));

        var disponible = await _service.ModuloDisponibleAsync("embarques", empresaId);

        disponible.Should().BeTrue();
    }

    [Fact]
    public async Task ModuloDisponibleAsync_ModuloNoEnPlan_DevuelveFalse()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "STARTER"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "STARTER", 5, "embarques"));

        var disponible = await _service.ModuloDisponibleAsync("usuarios", empresaId);

        disponible.Should().BeFalse();
    }

    [Fact]
    public async Task ModuloDisponibleAsync_SinModulosDeclarados_DevuelveTrue()
    {
        var planId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        _empresas.Setup(r => r.GetByIdAsync(empresaId)).ReturnsAsync(EmpresaConPlan(empresaId, planId, "STARTER"));
        _planes.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(Plan(planId, "STARTER", 5));

        var disponible = await _service.ModuloDisponibleAsync("cualquiera", empresaId);

        disponible.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlanSuperiorAsync_UltimoPlan_DevuelveNull()
    {
        _planes.Setup(r => r.GetAllAsync(true)).ReturnsAsync(
            [Plan(Guid.NewGuid(), "ENTERPRISE", -1)]);

        var result = await _service.GetPlanSuperiorAsync("ENTERPRISE");

        result.Should().BeNull();
    }
}
